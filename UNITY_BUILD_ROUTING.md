# Central Unity build routing

## Ownership and explicit lookup selection

Simultria API owns two credential-free public directory addresses:

- Production (default): `https://buildingvirtualitysuite.com`
- Development: `https://backend.dev-buildingvirtuality.com`

Both use `/api/v2/unity/builds/versions/{version}/{product}`. The shared Development
host is not a product-specific DEV host. `SimultriaUnityBuildLookupEnvironment`
has stable serialized values `Production = 0` and `Development = 1`.
`SimultriaUnityBuildDirectory.TryGetBaseUrl(selection, out baseUrl)` returns false
and an empty URL for any unsupported value. There is no Local, custom URL,
automatic cross-environment retry, or inferred selection from old runtime IDs.

This lookup selection is independent of normal runtime profiles. A build profile,
runtime Editor override, configured Local URL, project-owned Production URL or
custom endpoint catalog cannot redirect discovery. The exact returned record
assigns the runtime environment: a Development lookup can legitimately resolve
to a configured Production runtime, and vice versa. Generic API transport and
the generated host-free route catalog remain unchanged.

Use `SimultriaEndpointCatalog.UnityBuildVersion(version, product)` for a
Production endpoint, or `SimultriaUnityBuildVersionLookupService(apiClient)` for
a Production typed lookup. Select Development explicitly with
`UnityBuildVersion(version, product, lookupEnvironment)` or
`SimultriaUnityBuildVersionLookupService(apiClient, lookupEnvironment)`.
Invalid enum values throw `ArgumentOutOfRangeException` before transport.
Requests disable bearer-provider authentication, suppress
API logs, use a bounded 30-second timeout, and copy no backend-composition
headers. Supply a dedicated credential-free client (for example,
`ApiClientFactory.CreateDefault()`), as Viewer Connection does. A custom
`IApiClient` must not add session credentials or global authentication headers
to this public cross-host request.

## Resolution and fallback boundary

`SimultriaUnityBuildRoutingService(apiClient, targetComposition)` accepts a
composition only to validate the backend assigned by the returned record and
defaults lookup to Production. Its explicit overload is
`SimultriaUnityBuildRoutingService(apiClient, lookupEnvironment, targetComposition)`.
Invalid lookup selections yield `build_lookup_environment_invalid` from
`ResolveAsync` without sending a request or authorizing fallback.
`ResolveAsync(version, product)` requires an exact version and product match,
maps the environment name, and rejects unknown, deprecated, or unconfigured
target environments. It never substitutes another active version.

The router never selects another lookup directory or a fallback runtime environment.
Viewer Connection owns the explicit lookup dropdown separately from Editor
manual override and the compiled build-profile fallback. Only
`SimultriaUnityBuildRoutingResult.IsVersionMissing` permits that integration to
consider its build-profile fallback. `ErrorCode` is `build_version_not_found`
in this case; `Succeeded` remains false and `EnvironmentId` remains empty.

`EvaluateLookupResult(version, product, ApiResult<SimultriaResourceResponse<
SimultriaUnityBuildVersionDto>>)` applies the same policy to an already-obtained
transport result. Synchronous Editor transports must preserve HTTP status and
the raw response body in `ApiError`; they must not turn every HTTP 404 into a
missing-record result. `EvaluateResponse(...)` remains available for successful
DTO responses only.

## Explicit missing-record response contract

Ticket 2's strict viewer backend route must return HTTP 404 with a JSON object
whose top-level `code` is exactly `build_version_not_found` only when the
requested supported product/version record is missing. For example:

```json
{
  "code": "build_version_not_found",
  "version": "1.0",
  "product": "activity_viewer",
  "message": "No matching Unity build version record exists."
}
```

Identity fields are optional for migration compatibility, but when present
must be strings matching the requested product/version exactly. Unsupported
products and deprecated records must use distinct codes/outcomes. A missing
route or unrecognized product must never use `build_version_not_found`.

Legacy message-only responses, including "No active Unity Build version...",
do not prove that the exact requested supported record is missing and are
rejected even when product/version identity fields match. Only the typed
`build_version_not_found` response above permits missing-record classification.
Bare 404s, blank responses, HTML, arbitrary messages, cancellation, timeout, network
exceptions, authentication failures, and successful responses with mismatched
identity are not missing-record evidence. Error projection never includes the
raw response body or exception message.

## Migration and compatibility

- Version 1.2.0 revises the former fixed-Production-only policy to support an
  explicit Development lookup. Production remains the zero/default selection.
  Consumer integrations must add a separate serialized field, not migrate or
  reinterpret an old ignored `ApiEnvironmentId` directory field. Package Editor
  1.7.0 and all existing dependency minima are retained.
- Version 1.1.1 reconciles fixed central discovery with development's distinct
  1.1.0 shared lookup-context API, retaining Editor 1.3.0 and Session 1.0.7 minima.
  Normal project/model/activity services still compose a validated
  `SimultriaLookupContext` and retain their existing base-type compatibility.
- The old endpoint accessor and three-argument lookup/router constructors remain
  source-compatible obsolete overloads. Their old runtime-directory selection
  cannot redirect Production discovery and is not validated. The new typed
  lookup-environment overloads are separate; the router places the enum second
  to preserve old `(client, composition, default)` call-site compatibility.
- The lookup service is no longer an environment-bound
  `SimultriaLookupServiceBase`. Callers typed as that base must migrate to the
  concrete lookup service. This deliberately avoids weakening configuration
  validation for normal project/model/activity services.
- Its obsolete `Composition`, `EnvironmentId`, and `EnvironmentStatus` properties
  represent the old caller context only, never the directory. They are empty
  or null when the preferred constructor is used.
- The context-taking build lookup constructor introduced in development remains
  as an obsolete transport-only compatibility adapter. It neither builds nor
  revalidates a runtime context; its supplied context sends the fixed endpoint
  directly, without copying the context's backend route, auth requirement or
  profile headers. The context's client must still be credential-free for this
  public request. New discovery code should use the client-based constructors,
  which needs no configured runtime environment. Typed null arguments distinguish
  the two one-argument overloads: `(IApiClient)null` or `(SimultriaLookupContext)null`;
  both are rejected rather than selecting another source.
- No generated contract snapshot has been regenerated for this host-selection
  policy change; the HTTP route is unchanged.
- Each selected directory must deploy the strict route and contain the requested
  product/version record. An old deployment's blank 404 is not eligible for
  runtime fallback and never causes a retry against another directory. Lookup
  selection does not create records, deploy backends, or activate database schema.

## Validation

Use `SimultriaUnityBuildLookupEnvironmentTests`, `SimultriaCentralBuildDirectoryTests`,
`SimultriaUnityBuildMissingRecordTests`, `SimultriaLookupReconciliationTests`,
and the existing lookup/routing tests
in Unity EditMode. They inject an API spy and make no external requests.
The complete missing-record fixture runs against both explicit lookup choices.
The normal-service regression verifies explicit backend routing and Local's
unconfigured fail-closed behavior are unchanged. Also run the Package Registry
validator, `python -m unittest discover "Tools~/tests"`,
`python "Tools~/update_contract.py" --validate-generated`, and `git diff --check`.
