# ADR-0006: Spielzustand und Regeln

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Ein Spiel durchläuft Lobby, laufendes Spiel und Ende. Die Regeln müssen
eindeutig und testbar sein.

## Entscheidung
Zustände: `Lobby`, `Running`, `Finished`.
Übergänge: `Start` (Lobby -> Running), `End` (Running -> Finished).

| Aktion | Erlaubt in | Weitere Regel |
|---|---|---|
| `Join(userId)` | `Lobby` | Nicht voll, nicht schon drin, nicht der Leiter |
| `Leave(userId)` | `Lobby`, `Running` | Muss Teilnehmer sein, Leiter nie. Teilnehmer wird samt Attributen gelöscht |
| `Start()` | `Lobby` | Mindestens 1 Spieler |
| `End()` | `Running` | |
| `UpdateSettings(maxPlayers)` | `Lobby` | 1 bis 50, nicht unter aktuelle Spielerzahl |
| `ReplaceAttributes(...)` | `Lobby`, `Running` | In `Finished` read-only |
| `EnsureCanBeDeleted()` | `Lobby`, `Finished` | In `Running` nicht erlaubt |

- Die Regeln sind Methoden in der Domain-Klasse `Game`. Zustandsfehler
  werfen `DomainException` (Api: 409), ungültige Eingaben `ValidationException`
  (Api: 400).
- Nach dem Start gibt es keinen Beitritt. Wer eine offene Lobby verlässt,
  kann dort wie jeder andere Nutzer wieder beitreten.
- Die Lobby-Liste zeigt nur Spiele im Status `Lobby`, bei denen der Nutzer
  weder Leiter noch Teilnehmer ist.
- Verlassen alle Spieler ein laufendes Spiel, läuft es weiter, bis der
  Spielleiter es beendet.

## Verworfene Alternativen
- Externe State-Machine-Bibliothek: für drei Zustände unnötig.
- Automatisches Ende, wenn alle Spieler verlassen: zusätzliche Regel ohne
  Anforderung.
- Soft-Leave (Teilnehmer bleibt als "Left" markiert): zusätzlicher Zustand
  und Filter.

## Konsequenzen
+ Alle Regeln liegen an einer Stelle und sind ohne Datenbank testbar.
- Attribute eines Spielers gehen beim Verlassen unwiderruflich verloren.
