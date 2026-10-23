# ADR-0010: Fehlerbehandlung und API-Konventionen

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Die Api muss Fehler einheitlich melden, ohne interne Details zu verraten.
Die Lösung soll schlank bleiben.

## Entscheidung
- Alle Fehler als ProblemDetails (RFC 9457) über einen zentralen
  `IExceptionHandler`.
- Mapping: `DomainException` 409, `ValidationException` 400, unbekanntes
  Spiel 404, fehlendes Recht 403, fehlende Authentifizierung 401,
  unerwartete Fehler 500 ohne Details (nur `traceId`).
- Validierung in Domain und Endpoint, keine FluentValidation.
- Nicht-Teilnehmer eines Spiels erhalten 403.
- Standard-`ILogger`: Zustandsänderungen (Join, Start, End, Delete) auf
  Information-Level, Unerwartetes auf Error.
- Health Check unter `/health` inklusive Datenbank.
- Entities verlassen die Api nie, Verträge liegen in `Game.Contracts`.

## Verworfene Alternativen
- FluentValidation: zusätzliche Abhängigkeit, Regeln liegen ohnehin in der
  Domain.
- 404 für fremde Spiele: Existenz verbergen, aber offene Lobbies sind
  ohnehin sichtbar.
- Serilog, Rate Limiting: nicht nötig, als Ausblick möglich.

## Konsequenzen
+ Einheitliche Fehlerantworten, ein Ort für das Mapping.
+ Keine Informationslecks bei 500.
- Keine Rate Limits, bei öffentlichem Betrieb nachzurüsten.
