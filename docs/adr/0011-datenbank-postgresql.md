# ADR-0011: Datenbank (PostgreSQL)

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Die Api speichert Profile, Spiele, Teilnehmer und Attribute. Gleichzeitige
Zugriffe müssen korrekt behandelt werden (ADR-0005).

## Entscheidung
- PostgreSQL mit EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- Die Datenbank läuft per `docker-compose` neben Keycloak. Keycloak nutzt im
  Dev-Modus seine eingebettete Datenbank.
- Migrations liegen im Repo. `Database.Migrate()` läuft beim Start nur in
  Development, in Produktion ist die Migration ein separater Schritt.
- Der Connection-String steht nicht im Repo (User Secrets bzw.
  Umgebungsvariable). Im `docker-compose.yml` stehen nur als Dev
  gekennzeichnete Zugangsdaten.
- Api-Tests laufen gegen Postgres per Testcontainers.

## Verworfene Alternativen
- SQLite: null Setup, aber abweichendes Nebenläufigkeitsverhalten und
  anderer SQL-Dialekt als in Produktion.
- EF InMemory: bildet Constraints und Transaktionen nicht ab.

## Konsequenzen
+ Echte Parallelität, `FOR UPDATE` und Unique-Indizes verhalten sich wie in
  Produktion.
- Zusätzlicher Container, Docker ist für Betrieb und Tests nötig.
