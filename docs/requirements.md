# Anforderungen und Kontext – Spielleiter-App

Aufbau nach arc42, Kapitel 1–3 (Einführung und Ziele, Randbedingungen, Kontextabgrenzung).
Architekturentscheidungen (Kapitel 9) siehe [docs/adr](adr/README.md).

## Zweck / Ziele

Bei Pen-&-Paper-, Brett- oder Gruppenspielen verwaltet meist eine Person, der Spielleiter, die
Werte aller Mitspieler (z. B. Lebenspunkte, Klasse, Inventar). Das passiert oft auf Zetteln und
ist für die Spieler nicht einsehbar. Die Spielleiter-App bündelt diese Daten zentral: Der
Spielleiter erstellt ein Spiel, die Spieler verbinden sich mit ihren eigenen Geräten und bringen
ihre Attribute mit. Der Spielleiter kann alle Werte außer dem Username anpassen. Jede Änderung
ist sofort bei allen Beteiligten sichtbar, sodass alle denselben Stand haben.

## Qualitätsziele

| Priorität | Qualitätsziel | Bedeutung für dieses Projekt |
|---|---|---|
| 1 | Sicherheit | Nur angemeldete Nutzer; nur der Spielleiter steuert sein Spiel; fremde Spiele bleiben unsichtbar; Passwörter und Daten sind geschützt |
| 2 | Konsistenz | Gleichzeitige Aktionen (z. B. Beitritt zum letzten Platz) führen nie zu einem ungültigen Spielzustand |
| 3 | Aktualität | Änderungen sind ohne manuelles Neuladen sofort bei allen Beteiligten sichtbar |
| 4 | Stabilität | Fehler und Verbindungsabbrüche bringen weder App noch Api zum Absturz |
| 5 | Testbarkeit | Regeln, Endpoints und ViewModels sind automatisiert und ohne UI testbar |

Szenarien zu diesen Zielen: [architecture.md, Kapitel 10](architecture.md#10-qualitätsanforderungen).

## Anforderungen

* REQ01: Ein Spielleiter kann ein Spiel (Lobby) erstellen
* REQ02: Spieler sehen offene Lobbys und können ihnen beitreten
* REQ04: Der Spielleiter kann alle Attribute eines Spielers überschreiben, nur den Username nicht
* REQ05: Der Spielleiter kann Spielern im Spielverlauf Attribute zuweisen, ändern und entfernen
* REQ06: Usernames sind eindeutig (ohne Beachtung der Groß-/Kleinschreibung)
* REQ07: Ein Spiel durchläuft die Zustände Lobby → Running → Finished; nur der Spielleiter startet, beendet oder löscht es
* REQ08: Der Spielleiter legt die maximale Spielerzahl fest (1–50)
* REQ09: Spieler können ein Spiel wieder verlassen
* REQ10: Der Server informiert alle Beteiligten live über Änderungen (Server-Push)
* REQ11: Spieldaten sind persistent (= die Api ist ohne Datenverlust neu startbar)
* REQ12: Login-Mechanismus mit Username und Passwort
* REQ13: Benutzer geben ihre Zugangsdaten in der App ein
* REQ14: Passwörter werden nur als gesalzener Hash gespeichert
* REQ15: Rollen pro Spiel (Spielleiter, Spieler): steuernde Aktionen nur für den Spielleiter
* REQ16: Nur Spielleiter und Spieler eines Spiels dürfen dessen Daten und Live-Signale sehen
* REQ17: Der Signierschlüssel ist Konfiguration und ohne Neukompilieren änderbar
* REQ18: Geheimnisse (Connection-String, Signierschlüssel) liegen nicht im Repository *(teilweise: Signierschlüssel noch in `appsettings.json`)*
* REQ19: Der Server validiert alle Eingaben und meldet Fehler einheitlich (ProblemDetails)
* REQ20: Gleichzeitige Aktionen dürfen den Spielzustand nicht inkonsistent machen (z. B. überfüllte Lobby)
* REQ21: Die App stürzt bei Fehlern nicht ab und verbindet sich nach Verbindungsabbruch neu
* REQ22: Die REST-Schnittstelle ist über OpenAPI beschrieben
* REQ23: Verschlüsselte Übertragung über HTTPS mit HSTS und HTTPS-Redirect (data in transit)
* REQ25: Die Verbindung zwischen Api und Datenbank ist per TLS verschlüsselt
* REQ24: Automatisierte Tests für Domain, Api und ViewModels

### Nicht-Anforderungen

* NREQ01: Passwort-Rotation bzw. „Passwort vergessen“
* NREQ02: E-Mail-Verifikation bei der Registrierung
* NREQ03: Angemeldet bleiben über einen App-Neustart hinaus (Token nur im Speicher)
* NREQ04: Globale Rollen wie Admin oder Moderator
* NREQ05: Eigentliche Spiellogik (Würfeln, Züge, Punkteberechnung)
* NREQ06: Chat zwischen Spielern
* NREQ07: Offline-Modus
* NREQ08: Web-Frontend (daher kein CORS)
* NREQ09: Horizontale Skalierung der Api (mehrere Instanzen)
* NREQ10: Mehrsprachigkeit
* NREQ11: Eigene Attribute, die Spieler beim Beitritt mitbringen (Attribute vergibt ausschließlich der Spielleiter)

## Stakeholder

* SH01: Spielleiter – erstellt und steuert Spiele, verwaltet die Werte der Spieler
* SH02: Spieler – tritt Spielen bei und sieht seine Werte und die der Mitspieler live
* SH03: Entwicklungsteam – setzt das Produkt um und betreibt es
* SH04: Auftraggeber (Lehrveranstaltung) – gibt die Anforderungen vor und nimmt das Ergebnis ab

## Architektur-Randbedingungen

* AC01: .NET 10 als Technologie vorgegeben (WebApi und MAUI-App)
* AC02: git ist verpflichtend
* AC03: keine kommerzielle Datenbank verfügbar
* AC04: kein Key Vault verfügbar
* AC05: kein externer Identity Provider bzw. Standard-OAuth-Dienst verfügbar
* AC06: kein Message Broker verfügbar (z. B. RabbitMQ, Kafka)
* AC07: kein verteiltes Caching verfügbar (z. B. Redis)
* AC08: keine kommerzielle Logging-Lösung verfügbar (z. B. Splunk, Elastic)
* AC09: Entwicklung und Betrieb lokal mit Docker; mobil wird primär auf dem Android-Emulator getestet

## Kontext und Abgrenzung

Das Projekt entsteht als Case Study im Fach Secure Mobile Development. Es konzentriert sich auf
die Verwaltung von Spielern und ihren Werten durch einen Spielleiter. Das eigentliche Spiel
findet weiterhin außerhalb der App statt. Neben der Fachlichkeit
soll das Projekt eine saubere, sichere Architektur mit Fokus auf Authentifizierung, Autorisierung
und Datensicherheit zeigen.

### Fachlicher Kontext

Spielleiter und Spieler brauchen einen gemeinsamen, verlässlichen Stand der Spielerwerte.
Statt Zettelwirtschaft hält die App alle Werte zentral. Der Spielleiter behält die Kontrolle
darüber, die Spieler sehen jede Änderung sofort.

### Technischer Kontext

| Partner | Kanal | Inhalt |
|---|---|---|
| MAUI-App → Api | HTTPS/REST, JSON, `Authorization: Bearer <JWT>` | Login, Profil, Spiele, Attribute |
| Api → MAUI-App | SignalR (WebSocket über TLS), Token als Query-Parameter | nur Signale (`GameChanged`, `LobbiesChanged`), Daten holt die App per REST |
| Api → PostgreSQL | EF Core (Npgsql), TLS | Accounts, Profile, Spiele, Teilnehmer, Attribute |
