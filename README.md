# Deucarian Simultria API

`com.deucarian.simultria-api` is the optional Simultria backend connection
layer for Deucarian viewers. It keeps the reusable API, Session, and Viewer
Authentication packages vendor-neutral while giving Simultria consumers one
typed source for environments, routes, lookup DTOs, and token acquisition.

## Boundary

This package owns:

- canonical Development, Testing, Acceptance, and Production descriptors;
- a package-owned, credential-free API service definition with no deployment
  host or active environment;
- typed endpoints for login, validation, projects, project models, model
  versions, downloads, activities, and Unity build environment discovery;
- typed read-only project/model/version/activity lookup services; and
- an injected provider that implements Authentication acquisition and
  server validation through the Simultria login and validation routes.

It does not own token storage, bearer-header formatting, authentication UI,
viewer auto-load context, browser commands, report issues, media, or markers.

## Environments

Simultria defines four known environments in a stable order:

1. Development (`simultria.development`)
2. Testing (`simultria.testing`)
3. Acceptance (`simultria.acceptance`)
4. Production (`simultria.production`)

The descriptors contain only a typed ID, lifecycle stage, and safe display
name. They never imply a host. Create project-owned settings from:

`Assets > Create > Deucarian > Connections > Simultria Connection Settings`

The created `ApiConnectionSettings` contains four managed environment
sub-assets and references the package's read-only `ApiServiceDefinition`.
Each has the fixed `simultria.primary` client and an empty base URL. The custom
inspector keeps the everyday view to four URL/status cards. An empty slot is
**Not configured**. A valid absolute HTTP(S) URL changes it to **Configured**;
malformed or partial configuration is **Invalid** and fails closed.
Configuring one slot never causes another slot to fall back to that host.

You can also import **Simultria API Starter Assets** from Package Manager. It
contains the same four blank slots and is already wired to the package-managed
contract. IDs, request policies, and explicit project-owned contract overrides
remain available through the settings asset's **Advanced** details. An
explicit advanced action can fork the service definition after warning that
package contract updates no longer apply automatically.

Project settings must be referenced or bound explicitly. The package does not
ship connection settings or a fallback environment selection.

## Package service definition

The package definition contains contract metadata but no deployment URL. A
consumer must supply project-owned settings before composition:

```csharp
ApiConnectionSettings settings = projectConfiguration.ConnectionSettings;
ApiEnvironmentId environmentId = explicitEnvironmentId;
ApiComposition composition =
    SimultriaApiConnectionSettingsAdapter.CreateComposition(settings);
ApiEnvironmentStatus status =
    composition.GetEnvironmentStatus(environmentId);
```

The definition declares Development, Testing, Acceptance, and Production plus
the required `simultria.primary` named client. Unknown, blank, and unconfigured
IDs fail closed instead of guessing Development or Production. Settings never
contain credentials or tokens, and sanitized `ApiEnvironmentStatus` values do
not expose base URLs.

## Lookup services

Supply an `IApiClient` configured with the authoritative Session-backed auth
provider, then inject the resolved environment:

```csharp
var projects = new SimultriaProjectLookupService(
    apiClient,
    composition,
    environmentId);
ApiResult<SimultriaResourceResponse<SimultriaProjectDto>> result =
    await projects.GetProjectAsync(projectId, cancellationToken);
```

`SimultriaModelLookupService` resolves model and version metadata.
`SimultriaActivityLookupService` returns activity metadata. Report-specific
issue/media payloads intentionally remain in the Report integration.

`SimultriaUnityBuildVersionLookupService` calls the documented public Unity
build directory through an explicitly configured environment profile. It uses
`Application.version` only when a viewer integration deliberately supplies it;
the API package itself does not choose a build version, product, host, or
fallback environment. The backend names `development`, `test`/`testing`,
`accept`/`acceptance`, and `production` map to the four canonical Simultria
environment IDs. Missing, deprecated, and unknown values fail closed.

`SimultriaViewerModelResolver` accepts a project ID, model ID, and optional
version ID. It fetches project detail and returns the resolved project/model/
version IDs, names, and download URL. When no version is requested it selects
deterministically by order, version number, semantic version, update/create
time, then ID. This is the shared replacement for product-local URL resolvers
and DTO converters.

Report can retain its own issue/media DTO while reusing the route and transport:

```csharp
ApiResult<SimultriaCollectionResponse<ReportActivityDto>> result =
    await activities.GetActivitiesAsync<ReportActivityDto>(
        modelVersionId,
        cancellationToken);
```

Every lookup sends an API request with authentication explicitly required.

## Authentication

Create one concrete provider and register it in both generic provider slots:

```csharp
SimultriaAuthenticationProvider provider =
    SimultriaAuthenticationProviderFactory.Create(
        simultriaApiProfile,
        environmentId,
        apiClient);

IDisposable registration = AuthenticationTargetRegistry.Register(
    "viewer",
    "Viewer",
    authenticationSession,
    provider,
    provider);
```

The shared Authentication UI renders the provider's transient
`identity` and masked `password` fields. The package delegates the exchange to
Session API Integration, clears transient values after the operation, and
returns only sanitized lifecycle/validation results. API continues to own the
`Authorization: Bearer` header.

## Snapshot-scoped route catalog

The package-managed catalog contains all 351 operations parsed from the pinned
Scribe OpenAPI snapshot identified in
[`Documentation~/CONTRACT_PROVENANCE.md`](Documentation~/CONTRACT_PROVENANCE.md).
This is 351/351 coverage of that exact extracted file, not a claim that the
snapshot completely describes every live backend deployment. Extraction
warnings and the source discrepancy are recorded with the pinned SHA-256.

Existing package services keep these 13 hand-curated stable mappings:

- `POST /api/v2/login`
- `GET /api/v2/auth/validate`
- `GET /api/v2/projects`
- `GET /api/v2/projects/{id}`
- `GET /api/v2/projects/{project_id}/models`
- `GET /api/v2/projects/models/{id}`
- `GET /api/v2/projects/models/versions/{id}`
- `GET /api/v2/projects/models/{model_id}/versions/active`
- `GET /api/v2/projects/models/{model_id}/versions/frozen`
- `GET /api/v2/projects/models/versions/{version_id}/download`
- `GET /api/v2/projects/models/versions/{version_id}/activities`
- `GET /api/v2/projects/models/versions/{version_id}/activities/{id}`
- `GET /api/v2/unity/builds/versions/{id}/{product}`

The route catalog exposes resolved `ApiEndpoint` instances so callers do not
copy URL or authentication rules. The remaining 338 operations receive
deterministic `simultria.generated.<method>.<route>` IDs and are available
through the generic catalog API. Login and validation entries suppress API
request, response, and error logging because their payloads are sensitive.
The public Unity build lookup disables bearer authentication and suppresses
request/response logging so environment discovery cannot depend on a session.

No route contains a deployment host. Every absolute base URL remains in a
project-owned profile or imported starter asset and is blank by default.

## Updating the contract

The backend does not need a Unity-specific endpoint. A backend author generates
Scribe OpenAPI, then supplies the local `openapi.yaml` file and its exact Git
commit. Open the package-author workflow from:

`Tools > Deucarian > Simultria API > Open Contract Updater`

The updater previews semantic endpoint changes before it regenerates the
runtime catalog, coverage JSON, deterministic provenance manifest, and the
human-readable
[`Documentation~/Generated/API-Endpoints.md`](Documentation~/Generated/API-Endpoints.md).
It marks removals and route, method, authentication, or logging changes for
explicit review. The package must be referenced as a local or embedded package
to apply changes; installed Git packages remain read-only.

For command-line and future backend-CI integration, use:

```text
python Tools~/update_contract.py \
  --spec <local-openapi.yaml> \
  --source-revision <backend-git-commit>
```

The same command can emit JSON and Markdown change reports for an automated PR.
See
[`Documentation~/CONTRACT_AUTOMATION.md`](Documentation~/CONTRACT_AUTOMATION.md)
for the manual handoff, CI commands, security model, and deliberate human-review
boundary.
