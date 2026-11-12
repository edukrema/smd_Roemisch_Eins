# ADR-0005: Nebenläufigkeit

Status: Accepted · 2026-10-02

## Kontext
Mehrere Nutzer können gleichzeitig dem letzten freien Platz beitreten,
während der Spielleiter startet oder Einstellungen ändert.

## Entscheidung
- Jede Änderung an einem Spiel läuft in einer Transaktion, die zuerst die
  Game-Zeile sperrt (`SELECT … FOR UPDATE`, gekapselt in `GameLocking`).
- Der Unique-Index `(GameId, UserId)` sichert Doppelbeitritte zusätzlich ab (409).
- Lesende Endpoints sperren nicht. Attribute: letzter Schreiber gewinnt.

## Verworfen
- `Serializable` mit Retry bzw. optimistisches Locking: Retry-Logik ohne
  Nutzen bei wenigen konkurrierenden Schreibern.

## Konsequenzen
+ Deterministisch, ein Muster überall.
- Raw SQL an einer Stelle; Aktionen auf demselben Spiel werden serialisiert.
