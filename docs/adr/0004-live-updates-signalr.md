# ADR-0004: Live-Updates mit SignalR

Status: Accepted · 2026-10-02

## Kontext
Spieler sollen Änderungen ohne manuelles Aktualisieren sehen.

## Entscheidung
- Ein Hub (`/hubs/game`), nur Server → Client. Er sendet nur Signale
  (`GameChanged`, `LobbiesChanged`), der Client lädt danach per REST nach.
- Schreibaktionen laufen ausschließlich über REST. Benachrichtigt wird nach
  dem Commit über `IGameNotifier`.
- Der Beitritt zur Gruppe `game-{id}` ist nur für Leiter und Teilnehmer erlaubt.
- Client: automatischer Reconnect, danach Gruppe neu betreten und neu laden.

## Verworfen
- Polling: Latenz und viele Requests.
- Zustand über den Hub pushen: doppelte Autorisierung, Inkonsistenz nach Abbruch.

## Konsequenzen
+ Eine Stelle für Daten und Berechtigungen; verpasste Signale sind unkritisch.
- Mehr Komplexität bei Token und Reconnect. Mehrere Api-Instanzen bräuchten
  ein Backplane, das nicht verfügbar ist (AC06, AC07); eine Instanz genügt.
