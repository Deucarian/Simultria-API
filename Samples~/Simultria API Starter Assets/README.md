# Simultria API Starter Assets

This sample provides one project-owned `ApiConnectionProfile` with blank
Development, Testing, Acceptance, and Production URL slots. It references the
package-managed, read-only Simultria API v2 contract.

## Import

1. Open **Window > Package Manager**.
2. Select **Deucarian Simultria API**.
3. Import **Simultria API Starter Assets**.
4. Select the imported `SimultriaApiConnectionProfile` asset and enter only
   the base URLs
   this project is permitted to use. Leave every other environment blank.
5. Assign that profile to the viewer's Simultria integration.

The imported asset stores no credentials or access tokens. A blank URL is an
intentional disabled state, not a fallback to another environment.

## Create instead

Use **Assets > Create > Deucarian > Simultria > API Profile** to create the
same four-slot project asset anywhere under `Assets`.

Most projects should keep the package contract. If a project genuinely needs
different routes or request policies, use **Assets > Create > Deucarian >
Simultria > Advanced > API v2 Contract Override**, review the copy, then assign
it in the connection profile's contract field. The package asset is never
edited in place. Legacy `SimultriaApiProfile` assets remain supported under the
Advanced creation submenu for serialized compatibility.
