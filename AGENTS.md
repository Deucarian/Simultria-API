# Deucarian Simultria API Agent Notes

Package ID: `com.deucarian.simultria-api`

Follow the canonical Deucarian architecture and capability rules in Package
Registry.

## Ownership

This package owns Simultria-specific environment resolution, stable API v2
routes, response DTOs, read-only project/model/version/activity lookup
services, and the concrete Simultria acquisition/validation adapter injected
into Authentication.

It must not own HTTP transport, token/session persistence, authentication UI,
viewer lifecycle, browser transport, development viewer context, report
markers, issue media, or application commands.

The Package Registry capability is `simultria-api-integration`. Keep the
package metadata and registry ownership entry synchronized when changing it.

## Dependencies

- API owns HTTP transport, bearer-header formatting, and structured results.
- Session owns token state and lifecycle.
- Session API Integration owns credential-safe configurable token exchanges.
- Authentication owns the generic viewer-facing token workflow and UI.
- Newtonsoft JSON maps the documented snake_case API responses.

## Policies

- Endpoint profiles and environment definitions are credential-free.
- Never log, serialize, preview, or return credentials or access tokens from
  status values.
- Only documented Simultria routes and response semantics belong here.
- Do not add Report Viewer issues, media, markers, or commands.
- Unknown deployment environments require an explicit profile; do not invent
  production URLs.
- Do not add direct Unity `Debug` calls.

## Validation

Run the shared Package Registry validator, EditMode tests in a compatible Unity
consumer, and `git diff --check` before release. Do not create player builds for
package validation.
