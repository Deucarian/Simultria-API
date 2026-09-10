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
  - Exposes five individual descriptors, the four-stage `Standard` list, and
    the ordered five-option `All` list.
  - `Local` is a first-class `ApiEnvironmentStage.Local` descriptor, never
    `ApiEnvironmentStage.Custom`. It remains selectable when its project-owned
    URL is blank; its status is then unconfigured and does not fall back.
- `SimultriaBuildEnvironmentNameMapper.TryMap(...)`
  - Converts backend names such as `local`, `development`, `test`, `accept`, and
    `production` to canonical environment IDs. Unknown names fail closed.
- `SimultriaUnityBuildLookupEnvironment`
  - `Production = 0` (default) and `Development = 1` select only the public build
    directory. The returned record independently assigns the runtime environment.
- `SimultriaUnityBuildDirectory.TryGetBaseUrl(selection, out baseUrl)`
  - Maps Production to `https://buildingvirtualitysuite.com` and Development to
    `https://backend.dev-buildingvirtuality.com`. Unsupported values return false
    and an empty URL. No Local/custom host or automatic cross-directory retry exists.

### Stable integration IDs

- `SimultriaClientIds.Primary` identifies the standard Simultria client.
- `SimultriaCatalogIds.ApiV2` identifies the package-managed catalog.
- `SimultriaEndpointIds` exposes the 13 reviewed endpoint IDs through named
  fields and the ordered `Stable` list.

### Definition overrides

Normal projects own only `ApiConnectionSettings`; the package owns the
credential-free Simultria service definition and endpoint catalog. Use the
Advanced definition-override asset only when intentionally forking that
contract for a genuinely custom deployment. A normal Local connection is a
built-in package environment and does not require a definition override.

## Lookup services

Namespace: `Deucarian.Simultria.API.Services`

Normal project/model/activity lookup service constructors accept:

```csharp
IApiClient apiClient,
ApiComposition composition,
ApiEnvironmentId environmentId
```

Every async operation accepts an optional `CancellationToken` and returns an
`ApiResult<T>`. Services inherit the sanitized `Composition`, `EnvironmentId`,
and `EnvironmentStatus` properties from `SimultriaLookupServiceBase`.

The normal services also accept one shared `SimultriaLookupContext` constructed
from those three arguments. The context validates its explicit environment
and forwards requests through the injected client. It preserves normal-service
base-type compatibility and does not default an unconfigured environment.

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

- Default Production constructor: `SimultriaUnityBuildVersionLookupService(IApiClient)`.
- Explicit lookup constructor: `(IApiClient, SimultriaUnityBuildLookupEnvironment)`.
  Invalid selections throw `ArgumentOutOfRangeException` before transport.

- `GetBuildVersionAsync(string buildVersion, string product, ...)` returns
  `SimultriaResourceResponse<SimultriaUnityBuildVersionDto>` from the public
  build-directory route.

The service uses the selected API-owned directory without authentication or a
runtime profile. The old three-argument constructor is obsolete, always uses
Production and ignores its runtime-directory selection. Its old context properties remain
obsolete compatibility values only; the service no longer inherits the
environment-bound `SimultriaLookupServiceBase`.

The development 1.1.0 context-taking constructor remains as an obsolete
transport-only compatibility adapter in 1.1.1. It forwards the fixed central
endpoint through an already-supplied `SimultriaLookupContext`, without using
that context's environment, catalog or profile headers for discovery. Prefer
the client-based overloads: central discovery must not require a configured
runtime context. Both forms require a credential-free injected client; no
arbitrary client-global authentication headers can be made safe by the adapter.
When testing null arguments, explicitly cast to `IApiClient` or
`SimultriaLookupContext` to distinguish the two overloads.

### `SimultriaUnityBuildRoutingService`

Namespace: `Deucarian.Simultria.UnityBuildRouting`

- Default Production constructor: `(IApiClient, ApiComposition targetComposition)`.
- Explicit lookup constructor: `(IApiClient, SimultriaUnityBuildLookupEnvironment, ApiComposition targetComposition)`.
- `ResolveAsync(version, product, cancellationToken)` validates exact identity
  and the assigned runtime environment against the target composition.
  Invalid lookup selection returns `build_lookup_environment_invalid` without a
  request. Development lookup can resolve Production runtime and vice versa.
- `EvaluateResponse(version, product, dto)` evaluates a successful DTO.
- `EvaluateLookupResult(version, product, apiResult)` also classifies explicit
  missing-record HTTP failures for transports outside the injected client.
- `SimultriaUnityBuildRoutingResult.IsVersionMissing` and stable error code
  `build_version_not_found` identify only HTTP 404 JSON with the exact top-level
  `code: build_version_not_found`. Legacy message-only errors are rejected.
  The result does not itself choose a fallback environment.

The obsolete three-argument router constructor ignores its runtime-directory
selection and always uses Production. Pure `EvaluateResponse` and
`EvaluateLookupResult` evaluate supplied data only; caller-owned transports must
validate their lookup selection before obtaining that data.
See [central build routing](../UNITY_BUILD_ROUTING.md) for response semantics,
failure exclusions, and the Viewer Connection ownership boundary.

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

Each normal backend accessor requires `ApiComposition` and `ApiEnvironmentId`.
`UnityBuildVersion(buildVersion, product)` instead defaults to Production.
`UnityBuildVersion(buildVersion, product, lookupEnvironment)` explicitly selects
one supported API-owned directory and rejects invalid values before transport;
its obsolete four-argument overload ignores composition/selection and uses Production.
ID values must
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
