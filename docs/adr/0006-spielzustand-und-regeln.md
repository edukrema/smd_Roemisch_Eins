# ADR-0006: Spielzustand und Regeln

Status: Accepted · 2026-10-02

## Kontext
Ein Spiel durchläuft Lobby, laufendes Spiel und Ende. Die Regeln müssen
eindeutig und testbar sein.

## Entscheidung
Zustandsautomat `Lobby → Running → Finished` als Methoden der Domain-Klasse `Game`:

| Aktion | Erlaubt in | Regel |
|---|---|---|
| `Join` | Lobby | nicht voll, nicht schon drin, nicht der Leiter |
| `Leave` | Lobby, Running | nur Teilnehmer; Attribute werden gelöscht |
| `Start` | Lobby | mind. 1 Spieler |
| `End` | Running | |
| `UpdateSettings` | Lobby | MaxPlayers 1–50, nicht unter Spielerzahl |
| `ReplaceAttributes` | Lobby, Running | |
| Löschen | Lobby, Finished | |

Zustandsfehler → `DomainException` (409), ungültige Eingaben →
`ValidationException` (400).

## Verworfen
- State-Machine-Bibliothek: für drei Zustände unnötig.
- Automatisches Ende, wenn alle gehen: Regel ohne Anforderung.

## Konsequenzen
+ Alle Regeln an einer Stelle, ohne Datenbank testbar.
- Attribute gehen beim Verlassen verloren.
