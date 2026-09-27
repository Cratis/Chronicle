---
applyTo: "**/*"
---

## Chronicle documentation authoring

Chronicle's documentation lives under `Documentation/**` and is part of the published Cratis documentation site. The shared `cratis/documentation` profile provides general authoring guidance; Chronicle-specific conventions live here and in the native repository verification.

### Chronicle client tabs

Chronicle's shared public documentation uses unified language tabs (`<ChronicleClientTabs>`) for examples that apply across clients. A shared page that contains `<ChronicleClientTabs>` must be `.mdx`. Write each placeholder as a self-closing component on its own line:

```mdx
<ChronicleClientTabs snippet="events/appending/example" />
```

The snippet id is extensionless. Matching snippets may exist for only the participating clients; use an explicit unsupported snippet when a participating client's tab must remain visible. Follow `Documentation/contributing/clients/index.mdx` for ownership and validation.

### Documentation verification

Run the repository-native documentation gates before committing:

```bash
bash Documentation/verify-markdown.sh
```

This runs:
- `markdownlint-cli2` on all `.md` and `.mdx` files under `Documentation/`
- `Documentation/verify-authoring.mjs` — validates Starlight aside variants, imports/components requiring `.mdx`, `<ChronicleClientTabs>` syntax, landing-page collisions, and zero direct client-language fences in shared pages (`Documentation/**` except `client-snippets/` and `clients/`)
- `Documentation/verify-authoring.mjs --self-test` — exercises the shared-page fence detector against Markdown/GFM and MDX parsing

The gate MUST pass with **zero errors**. `.github/workflows/markdown-verification.yml` runs the same self-test and authoring check on pull requests.

For detailed Chronicle client snippet workflows (adding shared examples, ownership, validation, and CI), see `Documentation/contributing/clients/index.mdx`.
