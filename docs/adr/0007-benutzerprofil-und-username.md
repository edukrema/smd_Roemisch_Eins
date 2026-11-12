# ADR-0007: Benutzerprofil und Username

Status: Accepted · 2026-10-02

## Kontext
Spieler werden über einen Username angezeigt, der änderbar sein soll.

## Entscheidung
- Tabelle `UserProfile` (`Id` = Konto-ID, `Username`, `UsernameNormalized`
  unique). Bei der Registrierung wird der Login-Name als Username übernommen.
- Username: 3–20 Zeichen, Buchstaben, Ziffern, `_`, `-`, case-insensitive
  eindeutig, jederzeit änderbar über `PUT /me/username` (vergeben: 409).
- Teilnehmer speichern nur die `UserId`, der Name wird beim Laden gelesen.

## Verworfen
- Username-Kopie pro Spiel: inkonsistent bei Umbenennung.
- SignalR-Signal bei Umbenennung: Aufwand ohne Anforderung.

## Konsequenzen
+ Ein Name pro Nutzer, überall konsistent.
- Umbenennungen sehen andere erst beim nächsten Nachladen.
