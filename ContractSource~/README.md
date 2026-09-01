# Contract source inbox

This folder is an optional, package-author-only inbox for a backend-generated
OpenAPI file. The three conventional `openapi.*` filenames are ignored by Git
so the full backend specification is not committed accidentally.

Manual handoff:

1. Generate `storage/app/scribe/openapi.yaml` in the backend checkout.
2. Copy it here as `openapi.yaml`.
3. Open **Deucarian Control Center > Developer > Simultria API Contract > Open Contract Updater**.
4. Enter the backend Git commit, preview, and apply the generated update.

The Editor detects when the inbox hash differs from the installed manifest and
opens the updater once per Unity session. An external file can also be selected
without copying it here.

Backend CI should call `Tools~/update_contract.py` directly and does not need
to use this inbox.
