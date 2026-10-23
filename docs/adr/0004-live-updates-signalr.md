# ADR-0004: Live-Updates mit SignalR

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Spieler sollen Änderungen (Beitritt, Start, Attribute, Ende, Löschen) ohne
manuelles Aktualisieren sehen.

## Entscheidung
- ASP.NET Core SignalR mit einem Hub (`/hubs/game`), nur Server-zu-Client.
- Der Hub sendet nur Signale (`GameChanged`, `LobbiesChanged`). Der Client
  lädt den Zustand danach per REST nach (`GET /games/{id}` bzw.
  `GET /games?status=lobby`).
- Alle Schreibaktionen laufen weiterhin über REST-Endpoints.
- Der Beitritt zur Gruppe `game-{id}` wird serverseitig geprüft (Leiter oder
  Teilnehmer). Die Lobby-Liste nutzt die Gruppe `lobby-list`.
- Authentifizierung über das Keycloak-JWT. Bei WebSockets wird das Token als
  `access_token`-Query-Parameter gelesen (nur für Pfade unter `/hubs`).
- Benachrichtigung erfolgt in der Api nach erfolgreichem Commit über
  `IGameNotifier`, nie in der Domain.
- Client: `IGameHubClient` kapselt die `HubConnection`,
  `WithAutomaticReconnect`, nach Reconnect Gruppe neu betreten und Zustand
  neu laden. Verbindung nur, solange Lobby oder Spiel angezeigt wird.

## Verworfene Alternativen
- Polling: höhere Latenz und mehr Requests.
- Zustand direkt über den Hub pushen: doppelte Autorisierung,
  Inkonsistenzen nach Verbindungsabbruch.
- Schreibaktionen über Hub-Methoden: dupliziert die REST-Logik.

## Konsequenzen
+ Fast sofortige Updates, ein Pfad für Daten und Berechtigungen.
+ Verpasste Events sind unkritisch, weil immer nachgeladen wird.
- Zusätzliche Komplexität bei Token, Reconnect und Lebenszyklus.
- Mehr Testaufwand (Hub-Integrationstests).
- Mehrere Api-Instanzen bräuchten ein Backplane (z. B. Redis), für die
  Abgabe nicht nötig (eine Instanz).
