# ADR-0002: Spielleiter-Modell

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Jeder registrierte Nutzer kann Spiele erstellen und an Spielen teilnehmen.
Es gibt keine globale Spielleiter-Rolle.

## Entscheidung
- Spielleiter ist, wer das Spiel erstellt hat (`Game.GameMasterId`). Er ist
  reiner Verwalter und kein `Participant`.
- Ein Nutzer darf beliebig viele Spiele leiten und in beliebig vielen
  Spielen mitspielen.
- Nur der Spielleiter darf starten, beenden, löschen, Einstellungen ändern
  und Attribute vergeben. Die Prüfung erfolgt in der Api per
  Resource-based Authorization (`GameMasterId == aktueller Nutzer`).
- Löschen ist nur in `Lobby` und `Finished` erlaubt und entfernt das Spiel
  hart (Cascade auf Participants und Attribute).
- Der Spielleiter kann das Spiel nicht verlassen, nur beenden oder
  (in Lobby und Finished) löschen.

## Verworfene Alternativen
- Globale Keycloak-Rolle `gm`: bildet "Leiter in A, Spieler in B" nicht ab.
- Spielleiter als Mitspieler: verkompliziert MaxPlayers und Attributlogik.
- Soft Delete: erfordert globale Filter, für den Umfang unnötig.

## Konsequenzen
+ Keycloak bleibt auf E-Mail und Passwort beschränkt.
+ Regeln (Join, Leave, Delete) sind per Domain-Unit-Test abgedeckt.
- Eine Übertragung der Spielleitung ist nicht möglich (nicht gefordert).
- Clients erfahren vom Löschen über das SignalR-Signal bzw. beim
  Nachladen (404).
