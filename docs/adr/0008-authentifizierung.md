# ADR-0008: Authentifizierung

Status: Accepted · 2026-10-02, überarbeitet 2026-11-12 (ersetzt Keycloak)

## Kontext
Nutzer müssen sich registrieren und anmelden. Ein externer Identity Provider
ist nicht verfügbar (AC05).

## Entscheidung
- Die Api verwaltet Konten selbst (`UserAccount`). Login und Registrierung
  über `POST /auth/login`: Ein unbekannter Username legt ein Konto an.
- Passwörter: PBKDF2-SHA256 mit Zufalls-Salt, Vergleich in konstanter Zeit.
- Die Api stellt eigene JWTs aus (HS256, 8 h, nur User-ID), validiert über
  `AddJwtBearer`. Für SignalR wird das Token aus `access_token` gelesen.
- Fallback-Policy: Alles außer `/auth/login` und `/health` verlangt Login.
- Die App hält das Token nur im Speicher.

## Verworfen
- Keycloak (ursprüngliche Entscheidung): externer Dienst, nicht verfügbar (AC05).
- ASP.NET Core Identity: viel Funktionsumfang, der nicht gebraucht wird.

## Konsequenzen
+ Keine zusätzliche Infrastruktur, einfach testbar (Fake-Auth in Tests).
- Sicherheit liegt in eigenem Code. Offene Punkte (Signierschlüssel im Repo,
  kein Rate-Limiting) siehe Risiken R02–R06 in
  [architecture.md](../architecture.md#11-risiken-und-technische-schulden).
