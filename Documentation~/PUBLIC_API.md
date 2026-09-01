# Public API

Use lookup services first, the typed endpoint facade second, and the generic
generated catalog only for advanced calls. Editor workflow types are internal;
their menus and command-line entry points are the supported authoring surface.

## Quick start

New projects should use generic `ApiConnectionSettings` created from:

`Assets > Create > Deucarian > Connections > Simultria Connection Settings`

Validate it with
`SimultriaApiConnectionSettingsAdapter.TryCreateComposition`, choose one of the
explicit `SimultriaEnvironmentIds`, and inject the application's `IApiClient`
into a lookup service.

See the [Unity developer guide](index.md) for complete examples.

## Choose the API for your task

| Task | Recommended public API |
| --- | --- |
| Validate/compose project settings | `SimultriaApiConnectionSettingsAdapter` |
| List/load projects and project models | `SimultriaProjectLookupService` |
| Load models and versions | `SimultriaModelLookupService` |
| Load activity metadata | `SimultriaActivityLookupService` |
| Resolve viewer model download metadata | `SimultriaViewerModelResolver` |
| Discover/map a build environment | `SimultriaUnityBuildVersionLookupService`, `SimultriaBuildEnvironmentNameMapper` |
| Connect Authentication | `SimultriaAuthenticationProviderFactory` |
| Resolve a reviewed route | `SimultriaEndpointCatalog` |
| Resolve another snapshot route | `ApiComposition.ResolveEndpoint` plus an ID from the [endpoint reference](Generated/API-Endpoints.md) |

## Configuration

Namespace: `Deucarian.Simultria.API.Configuration`

### Recommended

- `SimultriaApiConnectionSettingsAdapter`
  - `CreateComposition(ApiConnectionSettings)` validates project settings and
    returns its `ApiComposition`, or throws with a configuration error.
  - `TryCreateComposition(ApiConnectionSettings, out ApiComposition, out string)`
    is the non-throwing application startup path.
  - `IsCompatibleSettings(...)`, `IsCompatibleDefinition(...)`, and
    `IsCompatibleCatalog(...)` support setup and validation tooling.
- `SimultriaEnvironmentIds`
  - `Local`, `Development`, `Testing`, `Acceptance`, and `Production` are stable
    `ApiEnvironmentId` values. Select one explicitly.
- `SimultriaEnvironmentDescriptors`
  - Exposes five individual descriptors, the four-stage `Standard` list, and the ordered five-option `All` list.
- `SimultriaBuildEnvironmentNameMapper.TryMap(...)`
  - Converts backend names such as `local`, `development`, `test`, `accept`, and
    `production` to canonical environment IDs. Unknown names fail closed.

### Stable integration IDs

- `SimultriaClientIds.Primary` identifies the standard Simultria client.
- `SimultriaCatalogIds.ApiV2` identifies the package-managed catalog.
- `SimultriaEndpointIds` exposes the 13 reviewed endpoint IDs through named
  fields and the ordered `Stable` list.

### Definition overrides

Normal projects own only `ApiConnectionSettings`; the package owns the
credential-free Simultria service definition and endpoint catalog. Use the
Advanced definition-override asset only when intentionally forking that
contract for a custom deployment.

## Lookup services

Namespace: `Deucarian.Simultria.API.Services`

Every lookup service constructor accepts:

```csharp
IApiClient apiClient,
ApiComposition composition,
ApiEnvironmentId environmentId
```

Every async operation accepts an optional `CancellationToken` and returns an
`ApiResult<T>`. Services inherit the sanitized `Composition`, `EnvironmentId`,
and `EnvironmentStatus` properties from `SimultriaLookupServiceBase`.

### `SimultriaProjectLookupService`

- `GetProjectsAsync(...)` returns
  `SimultriaCollectionResponse<SimultriaProjectDto>`.
- `GetProjectAsync(int projectId, ...)` returns
  `SimultriaResourceResponse<SimultriaProjectDto>`.
- `GetProjectModelsAsync(int projectId, ...)` returns
  `SimultriaCollectionResponse<SimultriaModelDto>`.

### `SimultriaModelLookupService`

- `GetModelAsync(int modelId, ...)`
- `GetModelVersionAsync(int versionId, ...)`
- `GetActiveModelVersionAsync(int modelId, ...)`
- `GetFrozenModelVersionAsync(int modelId, ...)`

Each operation returns the corresponding `SimultriaResourceResponse<T>`.

### `SimultriaActivityLookupService`

- `GetActivitiesAsync(int versionId, ...)` returns the standard activity DTO
  collection.
- `GetActivityAsync(int versionId, int activityId, ...)` returns one standard
  activity DTO.
- `GetActivitiesAsync<TActivity>(...)` and `GetActivityAsync<TActivity>(...)`
  deserialize into an integration-owned extended DTO while retaining the
  package-owned endpoint and request policy.

### `SimultriaUnityBuildVersionLookupService`

- `GetBuildVersionAsync(string buildVersion, string product, ...)` returns
  `SimultriaResourceResponse<SimultriaUnityBuildVersionDto>` from the public
  build-directory route.

The service still requires an explicitly configured directory environment; it
does not choose a host or fallback environment.

## Viewer model resolution

Namespace: `Deucarian.Simultria.API.Services`

`SimultriaViewerModelResolver.ResolveAsync(projectId, modelId,
optionalVersionId, cancellationToken)` is the normal high-level entry point. It
returns a `SimultriaViewerModelResolveResult` containing:

- `Succeeded`, stable `ErrorCode`, and sanitized `Message`;
- resolved project, model, and model-version IDs/names;
- `DownloadUrl`;
- `UsedRequestedVersion` and `UsedActiveVersion` selection information.

When no version is pinned, the resolver prefers the model's active version and
then uses a deterministic latest-version fallback.

`ResolveFromProjects(...)` and `SelectLatestVersion(...)` are public pure helpers
for cached data, custom integrations, and tests. `SimultriaViewerModelErrorCodes`
contains the stable error-code constants.

## Authentication

Namespace: `Deucarian.Simultria.API.Authentication`

Prefer `SimultriaAuthenticationProviderFactory`:

- `Create(ApiConnectionSettings, ApiEnvironmentId, IApiClient)` for normal
  project configuration;
- `Create(ApiComposition, ApiEnvironmentId, IApiClient)` when the composition
  is already validated;
- matching `TryCreate(...)` overloads when startup should return status and a
  message instead of throwing;

The resulting `SimultriaAuthenticationProvider` implements Authentication
acquisition and validation interfaces. It exposes input
descriptors, sanitized environment state, endpoint templates, `AcquireAsync`,
and `ValidateAsync`. Normal applications should register it with Authentication
and let that package drive the lifecycle instead of invoking
those methods directly.

## Typed endpoint facade

Namespace: `Deucarian.Simultria.API.Endpoints`

`SimultriaEndpointCatalog` resolves `ApiEndpoint` values from a validated
composition. These accessors are the reviewed stable route surface:

- `Login(...)`
- `ValidateAuthentication(...)`
- `Projects(...)`
- `Project(..., int projectId)`
- `ProjectModels(..., int projectId)`
- `Model(..., int modelId)`
- `ModelVersion(..., int versionId)`
- `ActiveModelVersion(..., int modelId)`
- `FrozenModelVersion(..., int modelId)`
- `ModelVersionDownload(..., int versionId)`
- `ModelVersionActivities(..., int versionId)`
- `ModelVersionActivity(..., int versionId, int activityId)`
- `UnityBuildVersion(..., string buildVersion, string product)`

Each accessor requires `ApiComposition` and `ApiEnvironmentId`. ID values must
be positive; text path segments must be non-empty. The returned endpoint can be
extended with query/path values and passed to `IApiClient.SendAsync<T>`.

## Generated snapshot endpoints

The catalog contains every operation from the pinned backend snapshot. The
named accessors above are the stable, reviewed subset. Advanced code can
resolve another operation with:

```csharp
ApiEndpoint endpoint = composition.ResolveEndpoint(
    environmentId,
    new ApiEndpointId("simultria.generated.<method>.<route>"))
    .Endpoint;
```

Copy the exact ID from the
[generated endpoint reference](Generated/API-Endpoints.md). These IDs are
deterministic but snapshot-scoped, and generated operations use conservative
authentication/logging defaults. Promote frequently used operations into this
package's stable facade and service layer.

## DTOs and response envelopes

Namespace: `Deucarian.Simultria.API.Models`

| Type | Purpose |
| --- | --- |
| `SimultriaResourceResponse<T>` | Standard single-resource `data` envelope |
| `SimultriaCollectionResponse<T>` | Standard resource-list `data` envelope |
| `SimultriaProjectDto` | Project metadata and nested/sub-project models |
| `SimultriaModelDto` | Model metadata, active/frozen versions, and version lists |
| `SimultriaModelVersionDto` | Version metadata, ordering/timestamps, and download URL |
| `SimultriaActivityDto` | Standard activity metadata |
| `SimultriaUserSummaryDto` | Small nested user projection used by activities |
| `SimultriaUnityBuildVersionDto` | Build version, product, and backend environment name |
| `SimultriaViewerModelResolveResult` | Sanitized high-level viewer resolution result |

The DTOs are writable Newtonsoft JSON contracts. Treat fields not represented
by a shared DTO as integration-specific and use a generic service overload or
an integration-owned response type.

## Advanced and compatibility surface

Use these deliberately rather than as the default application path:

- direct `SimultriaAuthenticationProvider` construction or lifecycle
  calls;
- `SimultriaLookupServiceBase` as an extension base;
- direct `SimultriaEndpointCatalog` and stable identifier use;
- `SimultriaViewerModelResolver` pure selection helpers;
- generic activity DTO overloads;
- project-owned service-definition overrides exposed by the creation menu's
  Advanced section;
- generated endpoint IDs.

## Package boundaries and third-party APIs

- `com.deucarian.simultria-api` owns Simultria-specific environments, IDs,
  generated asset, DTOs, services, endpoint facade, and auth adapter.
- `com.deucarian.api` owns `ApiConnectionSettings`, `ApiServiceDefinition`,
  `ApiEndpointCatalog`, `ApiComposition`, `ApiEndpoint`, `IApiClient`,
  `ApiResult<T>`, and transport.
- Product bridges such as `SimultriaApiActivityMetadataSource` live in their
  integration packages, not here.

Do not put third-party operations in the Simultria catalog. Build a
project-owned generic catalog for a small integration, or a separate reusable
provider API package for a shared integration.

## More documentation

- [Unity developer guide](index.md)
- [Generated endpoint reference](Generated/API-Endpoints.md)
- [Contract automation and handoff](CONTRACT_AUTOMATION.md)
- [Installed contract provenance](CONTRACT_PROVENANCE.md)
