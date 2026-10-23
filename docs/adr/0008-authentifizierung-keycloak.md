# ADR-0008: Authentifizierung mit Keycloak

- Status: Accepted
- Datum: 2026-10-02

## Kontext
Nutzer registrieren sich und melden sich an. Keycloak verwaltet nur E-Mail
und Passwort, der Username gehört der Anwendung (ADR-0007). Die App läuft
auf Android, iOS und Windows.

## Entscheidung
- Keycloak im Dev-Modus per `docker-compose`, Realm `game` mit
  Self-Registration, Login per E-Mail, ohne E-Mail-Verifikation.
  Realm-Export im Repo (`keycloak/realm-game.json`).
- Public Client `game-app`, Authorization Code + PKCE, Custom-URI-Scheme als
  Redirect, kein Client-Secret in der App.
- Api: `AddJwtBearer`, Nutzer-ID aus dem `sub`-Claim über `ICurrentUser`,
  keine Keycloak-Rollen. Audience-Mapper am Client, `ValidAudience` in der
  Api.
- MAUI: `OidcClient` mit System-Browser, Tokens im `SecureStorage`, Refresh
  über `IAuthService`.
- Fester `KC_HOSTNAME`, damit der Issuer auf allen Plattformen identisch ist.
  Die Api lädt die Metadaten über die interne Adresse (`MetadataAddress`) und
  validiert gegen den festen Issuer (`ValidIssuer`).
- Plattformen: Android (`IntentFilter`), iOS (`Info.plist`), Windows
  (Protokollaktivierung im Manifest) sind konfiguriert. Hostnamen und URLs
  kommen aus zentraler Konfiguration (`.env`, `AppSettings`).
- Dev-Zugangsdaten stehen im `docker-compose.yml` und sind als Dev
  gekennzeichnet. Klartext-HTTP ist nur in Debug-Builds erlaubt.
- Tests: Fake-Auth für die Masse, optional ein Smoke-Test gegen echtes
  Keycloak. Der OIDC-Flow wird pro Plattform manuell per Checkliste geprüft.

## Verworfene Alternativen
- ASP.NET Core Identity: eigene Benutzerverwaltung und Token-Ausgabe, kein
  Standardprotokoll.
- Eigenbau: Sicherheitsrisiko.
- Keycloak-Rollen für den Spielleiter: siehe ADR-0002.

## Konsequenzen
+ Registrierung, Login und Passwortverwaltung ohne eigenen Code.
+ Standard-OIDC, Provider später austauschbar (`ICurrentUser`,
  `IAuthService`).
- Zusätzlicher Container und Konfigurationsaufwand.
- OIDC-Redirect je Plattform eigen konfiguriert, automatisiert getestet ist
  höchstens eine Plattform.
- Dev-Modus ist nicht produktionstauglich (kein TLS, eingebettete DB).
