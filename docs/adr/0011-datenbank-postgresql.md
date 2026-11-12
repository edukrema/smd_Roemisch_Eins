# ADR-0011: Datenbank (PostgreSQL)

Status: Accepted · 2026-10-02

## Kontext
Konten, Profile, Spiele, Teilnehmer und Attribute müssen gespeichert werden,
auch bei gleichzeitigen Zugriffen (ADR-0005). Keine kommerzielle DB (AC03).

## Entscheidung
- PostgreSQL mit EF Core (Npgsql), lokal per `docker-compose`.
- Migrations im Repo; `Database.Migrate()` beim Start nur in Development.
- Api-Tests laufen gegen echtes Postgres (Testcontainers).

## Verworfen
- SQLite / EF InMemory: anderes Sperr- und Constraint-Verhalten.

## Konsequenzen
+ `FOR UPDATE` und Unique-Indizes verhalten sich wie im Betrieb.
- Docker ist für Betrieb und Tests nötig.
