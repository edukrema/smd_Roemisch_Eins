# ADR-0010: Fehlerbehandlung und API-Konventionen

Status: Accepted · 2026-10-02

## Kontext
Die Api soll Fehler einheitlich melden, ohne interne Details zu verraten.

## Entscheidung
- Alle Fehler als ProblemDetails (RFC 9457) über einen zentralen
  `IExceptionHandler`: `DomainException` 409, `ValidationException` 400,
  unbekanntes Spiel 404, fehlendes Recht 403, unerwartet 500 (nur `traceId`).
- Validierung in Domain und Endpoint, keine FluentValidation.
- Entities verlassen die Api nie, Verträge liegen in `Game.Contracts`.
- Standard-`ILogger` und Health-Check unter `/health` (inkl. Datenbank).

## Verworfen
- FluentValidation: zusätzliche Abhängigkeit, Regeln liegen in der Domain.

## Konsequenzen
+ Einheitliche Fehlerantworten, ein Ort für das Mapping, keine Lecks bei 500.
