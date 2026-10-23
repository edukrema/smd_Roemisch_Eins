# ADR-0007: Benutzerprofil und Username

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Keycloak verwaltet nur E-Mail und Passwort. Der Username gehört der
Anwendung und kann geändert werden.

## Entscheidung
- Tabelle `UserProfile` mit `Id` = Keycloak-`sub`, `Username` (nullable),
  `UsernameNormalized` (unique) und `CreatedAt`.
- Bei der Registrierung über `POST /auth/login` wird das Profil zusammen mit
  dem Konto angelegt und der Login-Name direkt als Username übernommen.
  Fehlt ein Profil trotzdem (z. B. Testdaten), wird es lazy über `GET /me`
  angelegt; parallele Erstaufrufe werden über den Primärschlüssel abgesichert.
- Username: 3 bis 20 Zeichen (nach Trim), Buchstaben, Ziffern, `_` und `-`,
  case-insensitive eindeutig, jederzeit änderbar (auch während eines Spiels).
  Setzen und Ändern über `PUT /me/username`. Vergeben: 409, Format: 400.
- Die Eindeutigkeit sichert der Unique-Index auf `UsernameNormalized`.
- Teilnehmer speichern nur die `UserId`, der Name wird beim Laden aus dem
  Profil gelesen.
- Der Beitritt zu einem Spiel setzt einen gesetzten Username voraus.
- Die Username-Seite der App dient nur noch zum Umbenennen; nach dem Login
  geht es direkt ins Menü, weil der Username bei der Registrierung gesetzt wird.
- Umbenennungen werden nicht per SignalR gemeldet, andere Clients sehen den
  neuen Namen beim nächsten Nachladen.

## Verworfene Alternativen
- Username in Keycloak: widerspricht der Vorgabe, dass Keycloak nur E-Mail
  und Passwort kennt.
- Username-Kopie pro Spiel: Inkonsistenz bei Umbenennung.
- Keycloak-Webhook zur Profilanlage: zusätzliche Infrastruktur.
- SignalR-Signal bei Umbenennung: Aufwand ohne Anforderung.

## Konsequenzen
+ Ein Name pro Nutzer, überall konsistent.
+ Keine Synchronisation mit Keycloak nötig.
- Umbenennung ist bei anderen nicht sofort live sichtbar.
- Eindeutigkeit hängt am Unique-Index, Konflikte müssen als 409 gemappt
  werden.
