# ADR-0005: Nebenläufigkeit

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Mehrere Nutzer können gleichzeitig dem letzten freien Platz beitreten,
während der Spielleiter startet oder Einstellungen ändert.

## Entscheidung
- Jede zustandsändernde Aktion auf einem Spiel (`Join`, `Leave`, `Start`,
  `End`, `Delete`, Einstellungen, Attribute) läuft in einer Transaktion,
  die zuerst die Game-Zeile sperrt (`SELECT ... FOR UPDATE`).
- Danach wird die Domain-Methode aufgerufen und gespeichert. Die
  SignalR-Benachrichtigung erfolgt erst nach dem Commit.
- Das Muster ist in einem Helfer gekapselt (`LoadGameForUpdateAsync`).
- Der Unique-Index `(GameId, UserId)` sichert Doppelbeitritte zusätzlich ab
  (`DbUpdateException` wird zu 409).
- Attribute: letzter Schreiber gewinnt, kein optimistisches Locking, da nur
  der Spielleiter schreibt.
- Lesende Endpoints sperren nicht.

## Verworfene Alternativen
- Isolation Level `Serializable` mit Retry: reiner EF-Code, aber Konflikte
  müssen als Exception behandelt und wiederholt werden.
- Optimistisches Locking (RowVersion/ETag): erfordert Retry-Logik im Client,
  für wenige konkurrierende Schreiber unnötig.

## Konsequenzen
+ Einfach zu erklären, deterministisches Verhalten, ein Muster überall.
- Raw SQL für die Sperre im Api-Projekt (an einer Stelle gekapselt).
- Aktionen auf demselben Spiel werden serialisiert (bei dieser Größe
  unproblematisch).
