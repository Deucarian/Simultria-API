# Changelog

All notable changes to this package are documented here.

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
