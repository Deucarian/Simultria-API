# Changelog

All notable changes to this package are documented here.

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
