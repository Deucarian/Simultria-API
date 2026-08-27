# Simultria contract update automation

The package side is designed around one invariant: the authoritative backend
supplies a locally generated OpenAPI snapshot and its exact Git commit;
everything after that handoff is deterministic and runs without Unity, a
backend login, or deployment access.

## Automated backend gate

`Building-Virtuality-Backend` runs its `Simultria API Contract Compatibility`
Bitbucket step on every pull request and before every `development` deployment.
That job generates Scribe OpenAPI with live response calls disabled, checks out
this package's generator at an immutable commit, runs the generator tests, and
executes:

```text
python Tools~/update_contract.py \
  --spec <backend>/storage/app/scribe/openapi.yaml \
  --source-revision <package-manifest-backendRevision> \
  --check \
  --change-report-out <artifact.json> \
  --change-report-markdown-out <artifact.md>
```

Any source-to-generated drift or breaking/security-sensitive change is visible
as a CI failure with JSON and Markdown artifacts. The current backend commit is
recorded separately as CI provenance; it is not substituted into generated
package artifacts, because unrelated backend commits must not create false
contract drift. This is credential-safe: the authoritative backend repository
generates Scribe OpenAPI locally instead of retrieving an authenticated deployed
document, uses no Scribe authentication key, makes no response calls, and passes
the spec to this local-file-only tool. CI never pushes, merges, publishes, or
deploys a contract change.

## Manual author handoff

For an intentional package update, a backend author can also run Scribe and
hand over:

- `storage/app/scribe/openapi.yaml`; and
- the hexadecimal backend Git commit that generated it.

Package authors can select that file from **Tools > Deucarian > Simultria API >
Open Contract Updater**. Alternatively, copy it to the ignored package-author
inbox at `ContractSource~/openapi.yaml`. Unity notices a different inbox hash
once per session and opens the updater.

The Editor always previews first. Applying regenerates:

- the runtime `ApiEndpointCatalog` asset;
- catalog and coverage JSON;
- the deterministic provenance manifest;
- the package-owned `SimultriaApiV2Definition.asset` source version and
  fingerprint; and
- `Documentation~/Generated/API-Endpoints.md`.

The original OpenAPI file is not copied into package runtime content.

## Headless package update

The Editor calls the same command intended for future backend CI:

```text
python Tools~/update_contract.py \
  --spec <local-openapi.yaml> \
  --source-revision <backend-git-commit> \
  --change-report-out <change-report.json> \
  --change-report-markdown-out <change-report.md>
```

`SIMULTRIA_BACKEND_COMMIT` may supply the source revision instead of the command
argument. The revision is mandatory and must be a hexadecimal Git commit, not a
branch name.

Use `--check` to verify a package branch without rewriting generated artifacts:

```text
python Tools~/update_contract.py \
  --spec <local-openapi.yaml> \
  --source-revision <backend-git-commit> \
  --check
```

Use this command when the source snapshot is unavailable but a package PR still
needs internal generated-file validation:

```text
python Tools~/update_contract.py --validate-generated
```

That check proves the catalog JSON, Unity asset, coverage, manifest and Markdown
reference agree. Only an update/check with the source OpenAPI file proves that
all supported operations in that supplied snapshot were regenerated.

The package command returns non-zero for invalid OpenAPI, unsupported HTTP
methods, stale overlay keys, duplicate IDs, incomplete snapshot coverage, or
stale `--check` output. The CI-generated Markdown report is the review input for
the intentional package feature branch.

## Deliberate review boundary

Generation and PR preparation may be fully automated. Merge remains reviewed:

- added operations need ownership and sensitivity review;
- removed operations may break typed or generic consumers; and
- route, method, authentication, or logging changes are marked as breaking or
  security-sensitive.

After package merge, package release/promotion and pinned consumer updates are
separate distribution decisions. They can later receive their own bot PRs, but
must not be silently coupled to backend deployment.

## Security

- Tools accept local files only and reject URLs.
- No backend or user credential is stored or requested.
- The conventional full-spec inbox filenames are ignored by Git.
- Generated runtime routes remain relative and contain no deployment host.
- New unreviewed operations suppress API logging by default.
