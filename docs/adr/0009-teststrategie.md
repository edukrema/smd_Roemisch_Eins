# ADR-0009: Teststrategie

Status: Accepted · 2026-10-02

## Kontext
Stabilität und Testbarkeit werden bewertet, der Aufwand soll verhältnismäßig
bleiben.

## Entscheidung
- xUnit und NSubstitute.
- Domain: Unit-Tests für alle Regeln, ohne Datenbank.
- Api: Integrationstests mit `WebApplicationFactory`, Testcontainers
  (Postgres) und Fake-Auth: Erfolgs- und Fehlerfälle (401/403/404/409),
  parallele Joins, SignalR.
- App: ViewModel-Tests mit gemockten Services. Keine UI-Tests.
- Kein Coverage-Gate; Tests heißen nach Verhalten (`Join_WhenLobbyIsFull_Throws`).

## Verworfen
- SQLite/InMemory für Api-Tests: verhält sich anders als Postgres.
- Coverage-Gate: fördert Tests ohne Aussagekraft.

## Konsequenzen
+ Aussagekräftige Tests, Regeln ohne Infrastruktur prüfbar.
- Api-Tests brauchen Docker.
