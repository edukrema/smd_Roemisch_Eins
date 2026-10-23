# ADR-0009: Teststrategie

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Stabilität und Testbarkeit werden bewertet. Der Aufwand soll verhältnismäßig
bleiben.

## Entscheidung
- xUnit, NSubstitute als Mocking-Bibliothek.
- Domain: Unit-Tests für alle Regeln aus ADR-0002 bis ADR-0007, ohne
  Datenbank.
- Api: Integrationstests mit `WebApplicationFactory`, Testcontainers
  (Postgres) und Fake-Authentifizierung. Abgedeckt sind pro Endpoint der
  Erfolgsfall und die wichtigsten Fehlerfälle (401, 403, 404, 409),
  Nebenläufigkeit (parallele Joins), Username-Konflikt und eine
  SignalR-Benachrichtigung.
- Optional ein Smoke-Test gegen echtes Keycloak.
- App: Unit-Tests für ViewModels mit gemockten Services (Login-Weiterleitung,
  Username-Fehler, Reaktion auf `GameChanged`, 404 bei gelöschtem Spiel).
- Ein Postgres-Container pro Testlauf, Daten pro Test bereinigt.
- Tests heißen nach Verhalten, z. B. `Join_WhenLobbyIsFull_Throws`.
- Kein festes Coverage-Ziel und kein Coverage-Gate. Es werden nur sinnvolle
  Tests geschrieben, die Verhalten und Risiken absichern.
- Keine UI-Tests, Views enthalten keine Logik.

## Verworfene Alternativen
- Coverage-Gate (z. B. 80 %): fördert Tests ohne Aussagekraft.
- SQLite/InMemory für Api-Tests: weicht von Postgres ab.
- Moq: kein fachlicher Grund, NSubstitute ist lesbarer.

## Konsequenzen
+ Schnelle, aussagekräftige Tests, Regeln ohne Infrastruktur prüfbar.
- Api-Tests benötigen Docker.
- Keine Zahl, die den Testumfang objektiv belegt.
