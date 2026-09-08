# Central Unity build routing

## Ownership and address

Simultria API owns the credential-free public directory address:

`https://buildingvirtualitysuite.com/api/v2/unity/builds/versions/{version}/{product}`

This Simultria-specific discovery host is not a normal runtime backend profile.
It cannot be changed by Editor selection, a build profile, a configured Local
URL, a project-owned Production URL, or a custom endpoint catalog. Generic API
transport and the generated host-free route catalog remain unchanged.

Use `SimultriaEndpointCatalog.UnityBuildVersion(version, product)` for a
fixed-address endpoint, or `SimultriaUnityBuildVersionLookupService(apiClient)`
for a typed lookup. Requests disable bearer-provider authentication, suppress
API logs, use a bounded 30-second timeout, and copy no backend-composition
headers. Supply a dedicated credential-free client (for example,
`ApiClientFactory.CreateDefault()`), as Viewer Connection does. A custom
`IApiClient` must not add session credentials or global authentication headers
to this public cross-host request.

## Resolution and fallback boundary

`SimultriaUnityBuildRoutingService(apiClient, targetComposition)` accepts a
composition only to validate the backend assigned by the returned record.
`ResolveAsync(version, product)` requires an exact version and product match,
maps the environment name, and rejects unknown, deprecated, or unconfigured
target environments. It never substitutes another active version.

The router never selects a fallback environment. Viewer Connection owns Editor
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

- Version 1.1.1 reconciles fixed central discovery with development's distinct
  1.1.0 shared lookup-context API, retaining Editor 1.3.0 and Session 1.0.7 minima.
  Normal project/model/activity services still compose a validated
  `SimultriaLookupContext` and retain their existing base-type compatibility.
- The old endpoint accessor and three-argument lookup/router constructors remain
  source-compatible obsolete overloads. Their directory selection cannot
  redirect discovery and is not validated.
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
  public request. New discovery code should use the client-only constructor,
  which needs no configured runtime environment. Typed null arguments distinguish
  the two one-argument overloads: `(IApiClient)null` or `(SimultriaLookupContext)null`;
  both are rejected rather than selecting another source.
- No generated contract snapshot has been regenerated for this host-selection
  policy change; the HTTP route is unchanged.
- Live rollout still requires Ticket 2 backend support. The inspected central
  deployment's blank 404 for viewer product names is not eligible for fallback
  and will remain a clear failure until that deployment supports the contract.

## Validation

Use `SimultriaCentralBuildDirectoryTests`,
`SimultriaUnityBuildMissingRecordTests`, `SimultriaLookupReconciliationTests`,
and the existing lookup/routing tests
in Unity EditMode. They inject an API spy and make no external requests.
The normal-service regression verifies explicit backend routing and Local's
unconfigured fail-closed behavior are unchanged. Also run the Package Registry
validator, `python -m unittest discover "Tools~/tests"`,
`python "Tools~/update_contract.py" --validate-generated`, and `git diff --check`.
