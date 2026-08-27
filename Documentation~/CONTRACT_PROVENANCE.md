# Simultria API Contract Provenance

## Current status

The package-owned `SimultriaApiV2EndpointCatalog.asset` is a snapshot-scoped,
read-only integration catalog. Its routes and HTTP methods were generated from
this local Scribe OpenAPI extraction:

- source: `storage/app/scribe/openapi.yaml` from the backend extraction
  worktree;
- backend source: `Building-Virtuality-Backend` `origin/development` commit
  `53f2ee778c5ec3d22763c86537850061642317cb`;
- canonical source SHA-256 (mapping keys sorted; volatile `example` and
  `examples` values excluded):
  `2283E2AD99C8D42F4ED400F763F08C53DB5730459CF32E0087849344F710E8E8`;
- parsed surface: 232 paths and 351 HTTP operations; and
- runtime catalog coverage: all 351 operations in that exact snapshot;
- hand-curated compatibility mappings: 13 stable IDs described by
  `simultria-api-v2.supported-subset.overlay.json`; and
- deterministic generated mappings: 338 method/path-derived IDs.

The same source identity is recorded mechanically in
`Generated/SimultriaApiV2Contract.manifest.json`. The generated
`Generated/API-Endpoints.md` file provides a reviewable route table for every
catalog operation without deployment URLs or request/response examples.

This is **not** evidence of full Simultria API coverage. Scribe returned a
non-zero status with route-level extraction warnings, while a separate docs
text scan observed roughly 383 method/path entries. The snapshot's only server
is a placeholder (`http://{tenant}.example.invalid`). In the isolated
extraction worktree, the only tracked source change set Scribe
`response_calls.methods` from `['GET']` to `[]`; that guarantees contract
generation did not invoke a live application route. The snapshot is not
committed here because it is a large backend-generated artifact; the backend
commit and canonical SHA-256 pin the exact review trail. Excluding response
examples prevents Scribe-generated sample values from creating false drift;
paths, methods, authentication, schemas, parameters, and other contract fields
remain fingerprinted.

Environment base URLs are deliberately excluded from the contract. They are
project-owned values entered in `ApiConnectionSettings` assets that reference
the generated `ApiServiceDefinition`.

## Intended source of truth

Package authors should keep an approved, versioned local OpenAPI snapshot for
each release review and pair it with a small Deucarian overlay. The snapshot
owns paths and HTTP methods. The overlay owns stable
endpoint IDs and Deucarian metadata that OpenAPI may not express, such as the
named client, authentication requirement, logging suppression, response hint,
and request-policy overrides.

Generated catalog and coverage JSON are checked in under
`Documentation~/Generated`. The generator also writes the Unity catalog asset,
which is the authoritative runtime artifact. Runtime code must never download
an endpoint catalog: authentication bootstrap, offline use, and
installed-package versioning all require a pinned local contract.

## Local generation workflow

`Tools~/generate_contract.py` accepts JSON directly and YAML when PyYAML is
installed. Both inputs must be local files; URL inputs are rejected.

Package authors normally use the Editor Contract Updater or the one-command
wrapper. Both invoke the same deterministic generator:

```text
python Tools~/update_contract.py \
  --spec path/to/approved-openapi.yaml \
  --source-revision <backend-git-commit>
```

The wrapper also regenerates the manifest and Markdown endpoint reference.
`--change-report-out` and `--change-report-markdown-out` emit ephemeral semantic
review summaries suitable for a future backend-triggered package PR.

The checked-in `simultria-api-v2.supported-subset.overlay.json` is the current
overlay. A minimal overlay entry has this shape:

```json
{
  "catalogId": "simultria.api-v2",
  "displayName": "Simultria API v2",
  "defaultClientId": "simultria.primary",
  "operations": {
    "loginOperationIdFromTheApprovedSpec": {
      "endpointId": "simultria.auth.login",
      "authentication": "Disabled",
      "suppressLogging": true
    }
  }
}
```

Generate review files:

```text
python Tools~/generate_contract.py \
  --spec path/to/approved-openapi.json \
  --overlay Documentation~/simultria-api-v2.supported-subset.overlay.json \
  --catalog-out Documentation~/Generated/SimultriaApiV2EndpointCatalog.generated.json \
  --coverage-out Documentation~/Generated/SimultriaApiV2EndpointCatalog.coverage.json \
  --manifest-out Documentation~/Generated/SimultriaApiV2Contract.manifest.json \
  --documentation-out Documentation~/Generated/API-Endpoints.md \
  --unity-asset-out Runtime/Resources/Deucarian/Simultria/API/SimultriaApiV2EndpointCatalog.asset \
  --source-revision <backend-git-commit> \
  --require-complete
```

Unmapped operations receive deterministic
`simultria.generated.<method>.<route>` IDs. The overlay preserves reviewed IDs
and settings where package APIs already depend on them. `--require-complete`
checks that every operation is emitted and that every overlay entry matches.
That result proves coverage only for the exact canonical contract fingerprint,
never for a live service. Add `--check` in CI to compare existing generated files without
rewriting them.

Every derived/unreviewed operation suppresses API request, response, and error
logging by default. Only reviewed overlay entries may opt back into normal
logging. This prevents newly discovered credential, token, or sensitive payload
routes from becoming loggable merely because they appeared in a new snapshot.

Before updating the Unity asset, review:

1. the snapshot source, approval, version, and canonical SHA-256;
2. every unmapped operation and unused overlay key in the coverage output;
3. authentication and logging suppression for credential-bearing routes;
4. relative route templates only (never deployment URLs); and
5. typed accessor/service tests for each operation the package claims to
   support.

The checked-in Unity catalog remains the authoritative runtime artifact. A
release must not describe the package as covering the complete backend unless
the Scribe warnings are resolved, the source discrepancy is explained, and a
separately reviewed scope decision explicitly adopts every operation.
