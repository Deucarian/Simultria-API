# Deucarian Simultria API

`com.deucarian.simultria-api` is the optional Simultria backend connection
layer for Deucarian viewers. It keeps the reusable API, Session, and Viewer
Authentication packages vendor-neutral while giving Simultria consumers one
typed source for environments, routes, lookup DTOs, and token acquisition.

## Boundary

This package owns:

- the documented Simultria API v2 development environment and stable
  environment IDs;
- credential-free custom environment profiles;
- typed endpoints for login, validation, projects, project models, model
  versions, downloads, and activities;
- typed read-only project/model/version/activity lookup services; and
- an injected provider that implements Viewer Authentication acquisition and
  server validation through the Simultria login and validation routes.

It does not own token storage, bearer-header formatting, authentication UI,
viewer auto-load context, browser commands, report issues, media, or markers.

## Environments

The only built-in deployment currently backed by the supplied API
documentation is:

```csharp
ApiEnvironmentId environmentId = SimultriaEnvironmentIds.Development;
SimultriaApiProfile simultriaApiProfile =
    SimultriaApiProfileDefaults.Load();
ApiComposition composition = simultriaApiProfile.CreateComposition();
ApiEnvironmentStatus status =
    composition.GetEnvironmentStatus(environmentId);
```

The package-provided development profile maps that ID to the
`realization-simultria` tenant on
`https://realization-simultria.backend.dev-buildingvirtuality.com`. Selection
stores only the serializable `ApiEnvironmentId`; the generic API profile owns
the URL. A future deployment adds an explicit `ApiEnvironmentProfile` to the
`SimultriaApiProfile`. Unknown IDs fail closed instead of guessing a production
URL. Profiles and status objects never contain credentials or tokens, and the
sanitized `ApiEnvironmentStatus` intentionally does not expose base URLs.
Stable `Acceptance` and `Production` IDs are available for selection data, but
the default profile intentionally does not resolve them until verified generic
API environment assets are supplied.

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

## Viewer Authentication

Create one concrete provider and register it in both generic provider slots:

```csharp
SimultriaViewerAuthenticationProvider provider =
    SimultriaViewerAuthenticationProviderFactory.Create(
        simultriaApiProfile,
        environmentId,
        apiClient);

IDisposable registration = ViewerAuthenticationTargetRegistry.Register(
    "viewer",
    "Viewer",
    authenticationSession,
    provider,
    provider);
```

The shared Viewer Authentication UI renders the provider's transient
`identity` and masked `password` fields. The package delegates the exchange to
Session API Integration, clears transient values after the operation, and
returns only sanitized lifecycle/validation results. API continues to own the
`Authorization: Bearer` header.

## Documented routes in the catalog

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

The route catalog exposes resolved `ApiEndpoint` instances so callers do not
copy URL or authentication rules. Login and validation entries suppress API
request, response, and error logging because their payloads are sensitive.
