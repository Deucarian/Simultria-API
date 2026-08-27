# Simultria contract update automation

The package side is designed around one invariant: the backend supplies a local
OpenAPI snapshot and its exact Git commit; everything after that handoff is
deterministic and can run without Unity, a backend login, or network access.

## Manual handoff today

The backend author runs Scribe and hands over:

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

## Future backend pipeline handoff

The later backend job only needs to:

1. Generate Scribe OpenAPI in its contract-safe environment.
2. Check out a bot branch of `Deucarian/Simultria-API`.
3. Run `update_contract.py` with the OpenAPI artifact and backend commit.
4. Use the Markdown change report as the pull-request summary.
5. Commit the generated package files and open or update a review PR.

No further Unity generator or Editor redesign is required for that transition.
The package command returns non-zero for invalid OpenAPI, unsupported HTTP
methods, stale overlay keys, duplicate IDs, incomplete snapshot coverage, or
stale `--check` output.

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
