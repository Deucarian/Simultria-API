# Deucarian Simultria API for Unity

Use this package when Unity code needs to talk to the Simultria backend. Start
with a lookup service, use a typed endpoint accessor only when a service does
not fit, and use a generated endpoint ID only for an uncommon operation that
has not yet been promoted to the stable public API.

In Unity, this page is available from:

`Tools > Deucarian > Simultria API > Open Documentation`

## Choose what you need

| Task | Start with |
| --- | --- |
| Configure Development, Testing, Acceptance, and Production | `ApiConnectionSettings` and `SimultriaApiConnectionSettingsAdapter` |
| List or load projects | `SimultriaProjectLookupService` |
| Load models or model versions | `SimultriaModelLookupService` |
| Load activity metadata | `SimultriaActivityLookupService` |
| Resolve a viewer model to its download URL | `SimultriaViewerModelResolver` |
| Discover a build's backend environment | `SimultriaUnityBuildVersionLookupService` |
| Connect Authentication | `SimultriaAuthenticationProviderFactory` |
| Call a reviewed route without a service | `SimultriaEndpointCatalog` |
| Find any route in the current backend snapshot | [Generated endpoint reference](Generated/API-Endpoints.md) |
| See the supported C# surface | [Public API](PUBLIC_API.md) |

## How the packages fit together

The activity flow discussed in Activity Viewer is:

```text
Activity Viewer composition
    -> com.deucarian.activity-visualization.simultria
       SimultriaApiActivityMetadataSource
    -> com.deucarian.simultria-api (this package)
       SimultriaActivityLookupService
       SimultriaEndpointCatalog
       generated Simultria API v2 catalog asset
    -> com.deucarian.api
       ApiComposition + IApiClient + transport
```

`SimultriaApiActivityMetadataSource` is the product bridge. It is intentionally
not part of this package. This package owns Simultria routes, DTOs, services,
environment IDs, and authentication adapters. The generic API package owns the
provider-neutral client, composition, endpoint, result, and catalog types.

Most product code should enter this chain at a product bridge or lookup
service. It should not copy route strings or read the generated ScriptableObject
directly.

## Set up a project

1. Install `com.deucarian.simultria-api`. Its generic API and authentication
   dependencies are installed with it.
2. In Unity, choose
   `Assets > Create > Deucarian > Connections > Simultria Connection Settings`.
3. Enter the base URL for each environment the project uses. The settings store
   no credentials or tokens.
4. Reference those project-owned `ApiConnectionSettings` from the application's
   composition root.
5. Select an environment explicitly with `SimultriaEnvironmentIds`. There is
   no implicit Production fallback.
6. Inject the application's Session-backed `IApiClient` into the service that
   needs it.

The package owns the credential-free service definition and endpoint catalog;
the project-owned settings supply deployment URLs.

## Make a normal request

The application owns the API client and connection settings. The package turns
those into a validated composition and typed service:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;

public static class ProjectLoader
{
    public static async Task<
        ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>> LoadAsync(
            IApiClient apiClient,
            ApiConnectionSettings connectionSettings,
            int projectId,
            CancellationToken cancellationToken)
    {
        if (!SimultriaApiConnectionSettingsAdapter.TryCreateComposition(
                connectionSettings,
                out ApiComposition composition,
                out string message))
        {
            throw new InvalidOperationException(message);
        }

        var projects = new SimultriaProjectLookupService(
            apiClient,
            composition,
            SimultriaEnvironmentIds.Development);

        return await projects.GetProjectAsync(
            projectId,
            cancellationToken);
    }
}
```

In production code, pass the environment chosen by the application instead of
hard-coding Development. `ApiResult<T>.IsSuccess` reports transport/API success;
the response envelope's `Data` property contains the Simultria payload.

## Load activities

Use the package DTO when its fields are enough:

```csharp
var activities = new SimultriaActivityLookupService(
    apiClient,
    composition,
    environmentId);

ApiResult<SimultriaCollectionResponse<SimultriaActivityDto>> result =
    await activities.GetActivitiesAsync(
        modelVersionId,
        cancellationToken);
```

An integration that needs additional response fields can keep its own DTO while
reusing the same route, authentication, and transport policy:

```csharp
ApiResult<SimultriaCollectionResponse<ReportActivityDto>> result =
    await activities.GetActivitiesAsync<ReportActivityDto>(
        modelVersionId,
        cancellationToken);
```

This is why Report can own issue/media fields without duplicating a Simultria
endpoint.

## Use a stable endpoint directly

When no lookup service fits, resolve one of the reviewed stable endpoints and
send it through the injected API client:

```csharp
using Deucarian.API.Models;
using Deucarian.Simultria.API.Endpoints;

ApiEndpoint endpoint = SimultriaEndpointCatalog.Project(
        composition,
        environmentId,
        projectId)
    .WithQueryParameter("included_fields", "projects.models");

ApiResult<SimultriaResourceResponse<SimultriaProjectDto>> result =
    await apiClient.SendAsync<
        SimultriaResourceResponse<SimultriaProjectDto>>(
        endpoint,
        cancellationToken);
```

The accessor supplies the method, route template, client, authentication rule,
timeout, and logging policy. Callers only supply route/query values and the
response type.

## Use an endpoint that is only in the generated snapshot

The generated catalog contains every operation in the pinned backend snapshot.
Find its deterministic ID in the
[generated endpoint reference](Generated/API-Endpoints.md), then resolve it
through the composition:

```csharp
var endpointId = new ApiEndpointId(
    "simultria.generated.get.api.v2.projects.models.issues.by-id");

ApiEndpoint endpoint = composition
    .ResolveEndpoint(environmentId, endpointId)
    .Endpoint
    .WithPathParameter("id", issueId);

ApiResult<MyIssueResponse> result =
    await apiClient.SendAsync<MyIssueResponse>(
        endpoint,
        cancellationToken);
```

Generated IDs are snapshot-scoped and deliberately conservative: they preserve
the authentication rule shown in the endpoint reference and suppress logging
until reviewed. If an operation becomes a normal dependency, add a stable ID,
typed accessor, DTO/service, tests, and documentation to this package instead
of spreading the generated string across product code.

## Authentication

Create the Simultria provider from the same project settings, selected
environment, and API client, then register it as both the acquisition and
validation provider:

```csharp
SimultriaAuthenticationProvider provider =
    SimultriaAuthenticationProviderFactory.Create(
        connectionSettings,
        environmentId,
        apiClient);

IDisposable registration = AuthenticationTargetRegistry.Register(
    "viewer",
    "Viewer",
    authenticationSession,
    provider,
    provider);
```

Let Authentication drive `AcquireAsync` and `ValidateAsync`. The API
package owns bearer-header formatting; neither the settings nor this package
stores tokens.

## Add a third-party API

Third-party endpoints are easy to add at the generic `com.deucarian.api` layer,
but they should not be added to the Simultria catalog:

- For a small project-only integration, create a project-owned
  `ApiConnectionSettings` plus a project-owned `ApiServiceDefinition` and
  `ApiEndpointCatalog`.
- For a reusable provider integration, create a package such as
  `com.deucarian.<provider>-api` that owns its IDs, catalog, DTOs, typed
  services, authentication adapter, and documentation.
- Reuse `ApiComposition` and `IApiClient` for both approaches.

`SimultriaApiConnectionSettingsAdapter` intentionally rejects non-Simultria
definitions. The current OpenAPI generator is also Simultria-specific; it can be
extracted into generic tooling later without mixing providers today.

## When the backend contract changes

Today, a backend author generates the Scribe OpenAPI file and hands it off with
the exact backend Git commit. From that point, Unity package authors do not edit
route assets or endpoint documentation by hand. Use:

`Tools > Deucarian > Simultria API > Open Contract Updater`

The updater previews drift and regenerates the catalog asset, coverage,
provenance, manifest, and endpoint reference together. The same deterministic
command is ready for a future backend-CI job that opens an automated package PR.
Human review remains intentional for removals and route, method,
authentication, or logging-policy changes.

See [Contract automation](CONTRACT_AUTOMATION.md) for the handoff and CI design,
and [contract provenance](CONTRACT_PROVENANCE.md) for the installed snapshot.
