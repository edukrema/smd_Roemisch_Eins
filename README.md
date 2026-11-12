# Spielleiter-App

Ein Spielleiter erstellt Lobbies, Spieler treten bei, der Spielleiter startet und beendet das Spiel
und vergibt dynamische Attribute (Key-Value) an die Spieler. Alle Spieler sehen Änderungen live.

- Backend: ASP.NET Core 10 (Minimal API), EF Core, PostgreSQL, SignalR
- Auth: Die Api stellt eigene JWTs aus (Login mit Username/Passwort, Registrierung automatisch)
- Client: .NET MAUI 10, MVVM mit CommunityToolkit.Mvvm
- Architekturentscheidungen: [docs/adr](docs/adr/README.md)

```
src/
  Game.Domain       Entitäten und Spielregeln, keine Abhängigkeiten
  Game.Contracts    DTOs und Hub-Namen für Api und App
  Game.Api          Endpoints, Auth (Accounts + JWT), Data/ (DbContext, Migrations), Hubs/
  Game.App          MAUI-App (Views, ViewModels, Services)
tests/
  Game.Domain.Tests Regeln ohne Datenbank
  Game.Api.Tests    Endpoints gegen PostgreSQL (Testcontainers), Fake-Auth
  Game.App.Tests    ViewModels mit gemockten Services
docker-compose.yml  PostgreSQL mit TLS (nur Entwicklung)
```

## Voraussetzungen

- .NET 10 SDK
- Docker (für docker-compose und die Api-Tests)
- Für die App: MAUI-Workload (`dotnet workload install maui`), z. B. mit Visual Studio 2026.

## 1. PostgreSQL starten

```bash
docker compose up -d
```

| Dienst | Adresse | Zugang (nur Dev) |
|---|---|---|
| PostgreSQL | `localhost:5432`, DB `game`, TLS an | `game` / `game-dev-password` |

Nach dem Umstieg auf TLS (alpine → Debian-Image) einmal das alte
Volume löschen: `docker compose down -v`.

## 2. Api starten

Der Connection-String liegt nicht im Repo, sondern in den User Secrets:

```bash
dotnet user-secrets set ConnectionStrings:Game \
  "Host=localhost;Database=game;Username=game;Password=game-dev-password" \
  --project src/Game.Api


dotnet dev-certs https --trust     # einmalig: HTTPS-Dev-Zertifikat
dotnet run --project src/Game.Api
```

Die Api läuft auf https://localhost:5001 (http://localhost:5000 leitet auf HTTPS um). Zur
Datenbank verbindet sie sich immer per TLS (`SSL Mode=Require` wird im Code erzwungen).
`GET /health` prüft auch die Datenbank.

**Auth:** `POST /auth/login` mit `{ "username", "password" }` liefert ein JWT (8 h gültig).
Ein unbekannter Username registriert automatisch ein neues Konto und setzt den Username
direkt als Anzeigenamen des Profils; ein bekannter Username mit falschem Passwort wird
abgelehnt. Der Signierschlüssel steht in `appsettings.json`
(`Auth:SigningKey`, Dev-Wert).

**Migrations:** In der Umgebung `Development` wendet die Api beim Start alle Migrations
automatisch an. In Produktion ist das ein eigener Schritt:

```bash
dotnet tool restore
dotnet ef database update --project src/Game.Api            # anwenden
dotnet ef migrations add <Name> --project src/Game.Api --output-dir Data/Migrations   # neue Migration
```

## 3. App starten

Die Api-URL steht zentral in `src/Game.App/AppSettings.cs` (Standard: `https://localhost:5001`).

- **Windows:** `dotnet build src/Game.App -f net10.0-windows10.0.19041.0`, dann die exe unter
  `src/Game.App/bin/Debug/net10.0-windows10.0.19041.0/win10-x64/Game.App.exe` starten —
  gern mehrfach: Die App läuft unpackaged, jede Instanz hat ihre eigene Session (Token nur
  im Speicher). So lassen sich Spielleiter und Spieler parallel auf einem Rechner testen.
  Alternativ in Visual Studio `Game.App` als Startprojekt wählen und F5.
- **Android-Emulator:** Der Emulator erreicht den Rechner nicht über `localhost`, deshalb
  einmal pro Emulator-Start den Api-Port umleiten:
  ```bash
  adb reverse tcp:5001 tcp:5001
  ```
- **iOS-Simulator:** `localhost` funktioniert direkt (nur auf macOS).

Ablauf: Username und Passwort eingeben, *Sign in / Register* registriert beim ersten Mal
automatisch und übernimmt den Username direkt als Anzeigenamen, anschließend erscheint
das Menü. Umbenennen geht dort über *Change username*.

Die App spricht nur HTTPS/WSS; Android verbietet Klartext auch im Debug-Build
(`network_security_config.xml`). Emulator und Simulator vertrauen dem Dev-Zertifikat nicht,
deshalb akzeptieren Debug-Builds es für `localhost` (`Services/DevCertificate.cs`).
Release-Builds prüfen Zertifikate normal.

## Tests

```bash
dotnet test tests/Game.Domain.Tests
dotnet test tests/Game.Api.Tests      # braucht Docker (startet einen PostgreSQL-Container)
dotnet test tests/Game.App.Tests      # braucht den MAUI-Workload
```

## API

**Swagger UI** (nur Development): https://localhost:5001/swagger.

Alle Endpoints außer `/health` und `/auth/login` brauchen ein gültiges Access-Token
(`Authorization: Bearer <token>`). Fehler kommen als ProblemDetails: 400 ungültige Eingabe,
401 nicht angemeldet, 403 kein Recht, 404 nicht gefunden, 409 Regelverstoß, 500 unerwartet.

| Methode | Pfad | Zweck |
|---|---|---|
| POST | `/auth/login` | Login, registriert unbekannte Usernames automatisch; liefert JWT |
| GET | `/me` | Eigenes Profil, wird beim ersten Aufruf angelegt |
| PUT | `/me/username` | Username setzen oder ändern |
| GET | `/games` | Offene Lobbies, in denen man weder Leiter noch Spieler ist |
| POST | `/games` | Lobby erstellen (startet mit 4 Max-Spielern, änderbar über `/settings`) |
| GET | `/games/{id}` | Spielstand mit Spielern und Attributen (Leiter und Spieler) |
| DELETE | `/games/{id}` | Löschen (Spielleiter, in Lobby oder Finished) |
| POST | `/games/{id}/join` | Beitreten (nur Lobby, nicht voll, Username nötig) |
| POST | `/games/{id}/leave` | Austreten (Spieler, in Lobby oder Running) |
| POST | `/games/{id}/start` | Starten (Spielleiter, mindestens ein Spieler) |
| POST | `/games/{id}/end` | Beenden (Spielleiter) |
| PUT | `/games/{id}/settings` | `maxPlayers` ändern (Spielleiter, nur Lobby) |
| PUT | `/games/{id}/participants/{userId}/attributes` | Attributliste eines Spielers ersetzen (Spielleiter) |

**Live-Updates:** SignalR-Hub unter `/hubs/game`. Er sendet nur Signale (`GameChanged` mit der
GameId an die Gruppe des Spiels, `LobbiesChanged` an die Lobby-Liste). Die App lädt danach per
REST neu.
