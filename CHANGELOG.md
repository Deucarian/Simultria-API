# Changelog

## [1.0.3] - 2026-09-01

### Fixed

- Restored Local as a separate built-in selectable environment instead of
  requiring consumers to alias local development to Development.

### Added

- Added stable `simultria.local` ID and descriptor, a blank Local slot in
  generated and sample settings, and explicit backend-name mapping.
- Local remains credential-free and has no implied URL; projects may configure
  it or leave it intentionally unconfigured.

## [1.0.2] - 2026-08-31

- Registered the package workflow and a bounded, sanitized local-state card with Deucarian Control Center.
- Removed normal `Tools/Deucarian` menu exposure while preserving the standalone open API.
- Updated the shared Editor dependency to 1.2.0.
- Aligned API to 2.0.1 and Authentication to 1.0.1.

All notable changes to this package are documented here.

## [1.0.1] - 2026-08-27

### Fixed

- Updated the public API guide and Unity developer guide to use the canonical
  connection-settings and generic Authentication APIs introduced in 1.0.0.

## [1.0.0] - 2026-08-26

### Breaking

- Removed `SimultriaApiProfile`, its defaults loader, package resource,
  environment assets, factory, inspector, fallback paths, and compatibility
  overloads.
- Replaced generic connection profiles with project-owned
  `ApiConnectionSettings` referencing the package-owned
  `SimultriaApiV2Definition`.
- Missing project settings are now a hard configuration error; no package
  connection fallback or implicit environment selection remains.

### Added

- Updated the exact Session API Integration dependency to 1.2.0 for API 2.0
  compatibility.

- Added a complete Simultria connection-settings creation action and explicit
  advanced service-definition fork workflow.
- Added static tests rejecting the removed script GUID, field name, and assets.

## [0.5.1] - 2026-08-26

### Added

- Added a Unity-first developer guide with setup, lookup-service, direct
  endpoint, generated endpoint, authentication, package-boundary, and
  third-party integration guidance.
- Added a task-oriented public C# API map that distinguishes recommended,
  advanced, and serialized-compatibility surfaces.
- Added Unity menu commands for opening the installed developer guide and
  generated endpoint reference, with package-path and traversal tests.

## [0.5.0] - 2026-08-26

### Added

- Added a package-author Contract Updater that previews a local backend Scribe
  snapshot and regenerates the runtime catalog, coverage, provenance, and local
  endpoint reference from one Editor workflow.
- Added deterministic contract manifest and Markdown endpoint-reference output.
- Added semantic JSON and Markdown change reports for added, removed, changed,
  breaking, authentication, and logging-policy changes.
- Added a headless `update_contract.py` entry point for future backend-triggered
  package pull requests and a generated-artifact validation mode for package CI.
- Added an ignored `ContractSource~` inbox and a once-per-session Editor notice
  when its OpenAPI hash differs from the installed contract.
- Added Python generator tests, Editor contract-status tests, and GitHub package
  and generated-contract validation.

### Changed

- Snapshot count tests now use the deterministic generated manifest, so valid
  endpoint additions do not require hand-editing test counts.
- The package catalog inspector now shows its source commit, hash, coverage, and
  opens the Contract Updater.

### Security

- Contract updates still accept local files only, require an exact backend Git
  commit, store no credentials or deployment URLs, and keep generated routes
  logging-suppressed until reviewed.
- Breaking route/method/authentication/logging changes are explicitly marked for
  human review; automation prepares updates and PRs but does not silently merge.

## [0.4.1] - 2026-08-25

### Fixed

- Unpinned viewer model resolution now uses the model's configured active
  version before the deterministic latest-version fallback.
- Positive model-version IDs remain exact pins and are never replaced by the
  active or latest version.

## [0.4.0] - 2026-08-24

### Added

- Added a typed, credential-free Unity build-version lookup service and DTO.
- Added strict mapping from documented backend environment names to canonical
  Simultria environment IDs.

### Changed

- Promoted the public Unity build-version route to a reviewed stable endpoint
  with authentication disabled and logging suppressed.

### Security

- Missing, deprecated, and unknown build environments fail closed; no runtime
  lookup silently defaults to Production.

## [0.3.0] - 2026-08-19

### Added

- Added an importable **Simultria API Starter Assets** sample with blank
  Development, Testing, Acceptance, and Production URL slots.
- Added a package-author-only deterministic OpenAPI generator, pinned contract
  provenance, generated review data, and snapshot coverage reporting.
- Added a safe project-owned endpoint-catalog override workflow behind the
  profile inspector's Advanced section.

### Changed

- Expanded the package-managed runtime catalog to all 351 operations in the
  pinned Scribe snapshot while preserving the 12 existing stable endpoint IDs.
- Simplified the normal profile inspector to four URL/status cards and an
  explicit `Simultria API v2 · package managed · read-only` contract summary.
- Login and validation now inherit method, authentication requirement, and
  resolved timeout from the catalog instead of duplicating those values.
- Updated the Deucarian API dependency to 1.4.2 and adopted its generic
  project connection aggregate for new starter assets and guided creation.
- Expanded the blank package fallback from one physical environment asset to
  all four standard slots while preserving the Development asset identity.

### Security

- Kept all deployment URLs blank in package and sample assets. The generator
  emits only relative route templates and never fetches a runtime catalog.
- Suppressed API logging by default for every generated/unreviewed operation;
  only the reviewed typed subset may explicitly opt back into normal logging.

## [0.2.0] - 2026-08-19

### Added

- Added canonical Development, Testing, Acceptance, and Production descriptors
  without implied deployment URLs.
- Added a project-owned API profile creator with four editable, initially
  unconfigured environment sub-assets.
- Added a profile inspector using the shared Editor status language for
  Configured, Not configured, and Invalid state without exposing connection
  details through status objects.

### Changed

- Simultria profile composition now preserves known-but-unconfigured
  environments and fails closed for invalid partial configuration.
- Kept the existing package Development profile asset identity for migration
  safety while clearing its host, so no deployment URL ships in the package.

## [0.1.1] - 2026-08-19

### Fixed

- Qualified Unity object cleanup in the shared EditMode test composition so it
  compiles without ambiguity against System.Object.

## [0.1.0] - 2026-08-19

### Added

- Stable Simultria environment IDs and credential-free environment profiles.
- Typed API v2 endpoint catalog for authentication and viewer lookup routes.
- Project, model, model-version, and activity DTOs and lookup services.
- Simultria token acquisition and validation provider for Viewer
  Authentication 0.4.0.
- EditMode contract tests and Deucarian package-governance metadata.
