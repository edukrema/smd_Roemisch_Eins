# ADR-0002: Spielleiter-Modell

Status: Accepted · 2026-10-02

## Kontext
Jeder Nutzer kann Spiele erstellen und an Spielen teilnehmen. Es gibt keine
globale Spielleiter-Rolle.

## Entscheidung
- Spielleiter ist, wer das Spiel erstellt (`Game.GameMasterId`). Er ist
  Verwalter, kein Teilnehmer, und kann das Spiel nicht verlassen.
- Nur er darf starten, beenden, löschen, Einstellungen ändern und Attribute
  vergeben. Prüfung in der Api pro Ressource (`GameMasterId == aktueller Nutzer`).
- Löschen ist hart (Cascade) und nur in `Lobby` und `Finished` erlaubt.

## Verworfen
- Globale Rolle: bildet „Leiter in A, Spieler in B“ nicht ab.
- Spielleiter als Mitspieler: verkompliziert MaxPlayers und Attribute.

## Konsequenzen
+ Keine Rollenverwaltung nötig, Regeln per Unit-Test abgedeckt.
- Spielleitung ist nicht übertragbar.
