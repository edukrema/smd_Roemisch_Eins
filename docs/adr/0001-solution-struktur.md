# ADR-0001: Solution-Struktur

Status: Accepted · 2026-10-02

## Kontext
.NET-10-WebApi und MAUI-App (AC01). Ziel: saubere, testbare Architektur bei
möglichst wenig Struktur.

## Entscheidung
| Projekt | Inhalt | Referenziert |
|---|---|---|
| `Game.Domain` | Spielregeln, keine NuGet-Abhängigkeiten | – |
| `Game.Contracts` | DTOs für Api und App | – |
| `Game.Api` | Endpoints, Auth, `Data/` (DbContext, Fluent-Mapping, Migrations) | Domain, Contracts |
| `Game.App` | MAUI, MVVM (CommunityToolkit.Mvvm) | Contracts |

- Regeln sind Domain-Methoden, Verstöße werfen `DomainException`.
- Keine Repositories, Endpoints nutzen den `DbContext` direkt.
- ViewModels kennen keine UI-Typen; Navigation, Api und Auth laufen über
  Interfaces.

## Verworfen
- Clean Architecture mit Repositories: Boilerplate, der DbContext ist schon
  Repository und Unit of Work.
- Ein einziges Backend-Projekt: Trennung nur per Konvention, nicht per Compiler.

## Konsequenzen
+ Domain-Tests ohne Datenbank und Webserver; der Compiler hält Technik aus der Domain.
- EF Core braucht Fluent-Mapping für private Setter.
