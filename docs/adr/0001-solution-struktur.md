# ADR-0001: Solution-Struktur

- Status: Accepted
- Datum: 2026-10-02

## Kontext
.NET-10-WebApi und MAUI-App. Bewertet werden Stabilität, Testbarkeit und
saubere Architektur. Die Lösung soll so einfach wie möglich bleiben.

## Entscheidung
- Das Backend besteht aus zwei Projekten: `Game.Domain` (Spielregeln, keine
  NuGet-Abhängigkeiten) und `Game.Api` (Endpoints, Auth, Datenzugriff).
- `Game.Contracts` enthält die DTOs und wird von Api und App genutzt.
- `Game.App` ist die MAUI-App (MVVM mit CommunityToolkit.Mvvm). ViewModels
  liegen im App-Projekt und kennen keine UI-Typen (kein `Shell`, `Page`,
  `Application.Current`). Navigation, Api und Auth laufen über Interfaces
  (`INavigationService`, `IGameApiClient`, `IAuthService`).
- DbContext, EF-Mapping (Fluent API) und Migrations liegen in
  `Game.Api/Data`. Domain-Klassen enthalten keine EF-Attribute.
- Regeln stehen in Domain-Methoden (`Join`, `Start`, `End`, ...). Verstöße
  werfen eine `DomainException`, die die Api auf 409 abbildet.
- Es gibt keine Repositories. Endpoints bzw. kleine Services nutzen den
  `DbContext` direkt.

```
src/
  Game.Domain      Regeln, keine Abhängigkeiten
  Game.Contracts   DTOs für Api und App
  Game.Api         Endpoints, Auth, Data/ (DbContext, Migrations)
  Game.App         MAUI, MVVM
tests/
  Game.Domain.Tests
  Game.Api.Tests
  Game.App.Tests
docs/adr/
keycloak/realm-game.json
docker-compose.yml
```

Referenzen: `Api -> Domain, Contracts` / `App -> Contracts` / `Domain -> nichts`.

## Verworfene Alternativen
- Volle Clean Architecture mit Application und Infrastructure samt
  Repositories: zu viel Boilerplate, der DbContext ist bereits Repository
  und Unit of Work.
- Eigenes Projekt `Game.Data`: mehr Struktur, als die Größe rechtfertigt.
- Alles in einem Backend-Projekt: Die Trennung gälte nur per Konvention,
  nicht vom Compiler erzwungen.
- ViewModels in `Game.App.Core`: nur nötig, falls das Testprojekt nicht
  sauber gegen das MAUI-Projekt baut (Fallback).

## Konsequenzen
+ Domain-Tests laufen ohne Datenbank, Webserver und Container.
+ Der Compiler verhindert, dass Technik in die Domain gelangt.
- EF Core braucht Fluent-API-Mapping für private Setter und Backing Fields.
- Das Testprojekt der App muss ein Target-Framework von `Game.App` mitnutzen.
- Bei Wachstum kann `Data/` in ein eigenes Projekt verschoben werden.
