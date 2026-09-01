# Simultria API Starter Assets

This sample provides one project-owned `ApiConnectionSettings` asset with blank
Local, Development, Testing, Acceptance, and Production URL slots. It references the
package-managed, read-only Simultria API v2 contract.

## Import

1. Open **Window > Package Manager**.
2. Select **Deucarian Simultria API**.
3. Import **Simultria API Starter Assets**.
4. Select the imported `SimultriaConnectionSettings` asset and enter only
   the base URLs
   this project is permitted to use. Leave every other environment blank.
5. Bind those settings through Deucarian Control Center or assign them to the integration.

The imported asset stores no credentials or access tokens. A blank URL is an
intentional disabled state, not a fallback to another environment.

## Create instead

Use **Assets > Create > Deucarian > Connections > Simultria Connection
Settings** to create the
same five-slot project asset anywhere under `Assets`.

Most projects should keep the package contract. If a project genuinely needs
different routes or request policies, use **Assets > Create > Deucarian >
Connections > Advanced > Simultria API Definition Override**, review the copy,
then bind it explicitly. The package definition is never edited in place.
