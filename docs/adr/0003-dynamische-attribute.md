# ADR-0003: Dynamische Attribute

Status: Accepted · 2026-10-02

## Kontext
Der Spielleiter vergibt und ändert Attribute der Spieler.

## Entscheidung
- Key-Value-Paare (String, Value optional) in eigener Tabelle
  `ParticipantAttribute` mit Unique-Index `(ParticipantId, Key)`.
- Limits: max. 20 pro Spieler, Key 1–50 Zeichen (case-insensitive eindeutig),
  Value max. 200 Zeichen.
- Der Spielleiter ersetzt die Liste eines Spielers als Ganzes
  (`PUT /games/{id}/participants/{userId}/attributes`), möglich in `Lobby`
  und `Running`.
- Alle Teilnehmer sehen alle Attribute über `GET /games/{id}`.

## Verworfen
- JSON-Spalte: Eindeutigkeit schwer zu sichern.
- Typisierte Werte: Aufwand ohne Anforderung.

## Konsequenzen
+ Einfaches Modell und UI, ein GET liefert den ganzen Spielstand.
- Keine Typprüfung oder Berechnung auf Werten.
