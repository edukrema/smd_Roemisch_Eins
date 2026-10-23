# ADR-0003: Dynamische Attribute

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Spieler haben dynamische Attribute, die der Spielleiter vergibt und ändert.
Die Lösung soll so einfach wie möglich sein.

## Entscheidung
- Attribute sind Key-Value-Paare. Der Key ist ein Pflicht-String, der Value
  ein optionaler String (nullable).
- Ein leerer oder nur aus Leerzeichen bestehender Value wird zu `null`
  normalisiert.
- Eigene Tabelle `ParticipantAttribute` (`ParticipantId`, `Key`, `Value`)
  mit Unique-Index `(ParticipantId, Key)`. `Key` ist `NOT NULL`, `Value`
  nullable.
- Limits: max. 20 Attribute pro Spieler, Key 1 bis 50 Zeichen (nach Trim),
  Value max. 200 Zeichen. Keys sind pro Spieler case-insensitive eindeutig.
- Alle Teilnehmer sehen alle Attribute aller Spieler (read-only). Nur der
  Spielleiter darf schreiben.
- Der Spielleiter ersetzt die Attributliste eines Spielers als Ganzes
  (`PUT /games/{id}/participants/{userId}/attributes`).
- Änderungen sind in `Lobby` und `Running` möglich, in `Finished` nicht.
- Lesen liefert `GET /games/{id}` mit (Leiter und Teilnehmer).

## Verworfene Alternativen
- JSON-Spalte: Eindeutigkeit und Einzeländerungen schwerer zu sichern.
- Typisierte Werte (Zahl, Bool): mehr Validierung und UI-Aufwand ohne
  Anforderung.
- Sichtbarkeit pro Attribut oder Spieler: nicht gefordert.
- Optimistisches Locking: nur ein Schreiber (Spielleiter), siehe ADR-0005.

## Konsequenzen
+ Einfaches Modell, einfaches UI (zwei Textfelder pro Zeile).
+ Ein einziger GET liefert den kompletten Spielstand.
- Keine Berechnungen oder Typprüfungen auf Attributwerten.
- Typen können später über eine zusätzliche Spalte ergänzt werden.
