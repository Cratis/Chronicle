---
name: cratis-screenplay-extension-packages
description: Use when authoring, verifying, publishing or consuming a Cratis extension package that realizes a Screenplay system, including its contract, lock, restore, settings and components. Not for declaring a system's abilities in a model or invoking them; use cratis-screenplay-automations-and-translations.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-extension-packages/SKILL.md -->

# Building and consuming extension packages

An **extension package** is the code and contract that realizes a Screenplay `system`. The model names what the business can ask (abilities, triggers, components); the package carries how. Do not invent package behavior: the contract below is the Screenplay documentation, and the tooling is the Cratis CLI. If a fact is not on a linked page, say so instead of guessing.

## Pages to read first

- Screenplay [extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md): the contract file, manifest, settings, components, the `extension` reference, the lock, the cache and restore, provider compatibility.
- Screenplay [systems and abilities](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md) and [environments](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/environments.md#system-settings-and-named-secrets).
- Screenplay [screens: extension components](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/screens.md#extension-components) and the [command line `restore`](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/tool.md#restore-extensions).
- Stage [extension backends](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/index.md) (protocol, SDKs for C#, Kotlin, Go and TypeScript, host services), [packages](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/packages.md) and [execution tiers and activation](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/execution-tiers.md).
- Scene [extension component contract](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md): how a frontend component registers and what conformance checks.
- Cratis CLI [manage extension packages](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension.md), [`extension new`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-new.md), [`extension verify`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-verify.md), [`restore`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-restore.md), [`update`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-update.md) and [`vendor`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-vendor.md).

## Author a package

1. **Scaffold, do not hand-roll.** `cratis extension new -n <Name> [--kind extension|package] [--backend csharp|kotlin|go|typescript|none] [--frontend react|mount|screenplay|none] [--component react|mount] [--publisher <name>]`. The result already has a `contract.play`, builds and passes `cratis extension verify` without edits. In an AI agent or with redirected output it never prompts; it uses flags and defaults. `--dry-run` lists files without writing. The templates are `Cratis.Templates.Extensions` (`cratis-extension`, `cratis-extension-package`) from Cratis/Templates.
2. **Choose the kind.** `extension` is an OCI artifact (contract, optional backend, optional frontend). `package` is an npm package with frontend components only and no backend. An extension with neither backend nor frontend that only declares abilities is valid.
3. **Write the contract** (`contract.play`): a manifest (`display name`, `description`, `icon` required, else `PLAY0999`; `image`, `requires stage|screenplay|platform|runtime`, `activation`), then `setting`, `ability` (same grammar as [systems](../cratis-screenplay-automations-and-translations/references/systems-and-abilities.md)), `trigger`, `component` and `backend runtime <name> <version> assembly "<file>"` / `frontend "<path>"`. Contents are read from their declarations; never restate them in the manifest. Types resolve in the consuming model's scope.
4. **Settings.** `setting <name> [public | secret]`. `public` may reach a browser; `secret` never does; both is `PLAY1103`; neither stays with the backend.
5. **Components.** `kind react | mount | screenplay` (else `PLAY1100`), `platform`, `placeholder "<path>"` (required for `react` and `mount`, `PLAY1102`), typed `property` lines, `raises <signal>` with typed values, and `invokes <Ability>` for abilities the component may call through the host (must be declared, `PLAY1104`). A placeholder is static HTML: no script, inline event handler, `javascript:` URL or external reference, styles inline and images as `data:` URIs (`PLAY1113`).
6. **Backend.** Stage hosts backends outside its own process over a two-way gRPC protocol; the backend asks Stage for settings, secrets and telemetry through host services. Tier is chosen by Stage (`None`, `SharedHost`, `Native`, `Container`) from the contract's runtime. A backend is told its address, system and package digest by `STAGE_EXTENSION_*` environment variables. Read the Stage SDK page for the language chosen.
7. **Frontend.** A frontend component registers as its declared kind and must pass the Scene conformance kit; use the Scene contract page, not memory. A signal it raises runs the screen's `on <signal>` interaction (a Scene `SignalInteractionTrigger`, bound by Stage); see [handling a raised signal](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/components.md#handling-a-raised-signal).
8. **Same name twice** (abilities, triggers, components, settings, ignoring case) is `PLAY1105`; anything else unrecognized in the file is `PLAY0998`.

A `.extensionignore` in the package folder names what the build does not ship (sources, specifications, scripts) so verify can tell it from what ships.

## Verify before publishing

`cratis extension verify [TARGET] [--range <r>] [--baseline <folder>] [--strict] [--format table|json]`. TARGET is a package folder (default current directory), a `contract.play`, or a package reference. Checks always appear with a status: `passed`, `failed`, `skipped` (could not run; never a pass; `--strict` fails it) or `notapplicable`. The checks are `contract`, `manifest`, `contents`, `placeholders`, `frontend` and `backend`. The `frontend` and `backend` checks report `EXTV060` and `EXTV070` where they cannot run yet; read what the CLI page says does not run yet and report that honestly, never as green.

## Consume a package from a model

```play
system Payments
  extension "cratis/stripe" version 2
  setting secretKey from secret StripeSecretKey
```

- A system that names an extension declares no abilities of its own (`PLAY1106`); it gets the contract's abilities, and the contract's triggers become `<System>.<Trigger>` for reactions.
- A reference is a catalog short name (`owner/name` resolves to `ghcr.io/owner/extension-name`), a full OCI reference, or `npm:<package>` for frontend-only packages. `version 2` is the latest 2.x, `2.3.1` exactly, `"^2.3"` and `"~2.3"` as in semver.
- **The lock.** `screenplay.lock` beside the model root pins each reference (range, resolved reference, version, package `digest`, `contract` hash, `publisher`). Commit it. The compiler never resolves a range; it reads the contract by locked digest from the vendored `extensions` folder, then the user cache (`SCREENPLAY_EXTENSION_CACHE`). Unlocked is `PLAY1107`, range mismatch `PLAY1110`, not restored `PLAY1108`, contract differs from the hash `PLAY1109`, malformed lock `PLAY1111`. The language server and MCP downgrade some to warnings and restore on opening a model root.
- **Restore, update, vendor.** `cratis extension restore` fills the cache from the lock and pins what is missing; `cratis extension update` moves references to the newest release in range and shows how each contract changes; `cratis extension vendor` copies every locked package to a folder or private registry for offline use. `--locked` is for CI and never pins. Shared options: `--root`, `--source <folder|oci://host/prefix>`, `-e <environment>`, `-o <format>`. `screenplay restore` in the Screenplay tool restores from a directory without network.
- **Trust.** Naming a package is not trusting it. The CLI requires a valid signature for exactly the package digest and a publisher in the committed `.cratis/extensions.json` allow-list. An interactive terminal may ask once; CI and AI agents cannot ask and fail `Unallowed`. Never edit the allow-list or accept a publisher to make a command pass without the user's say; a changed key is reviewed, never auto-trusted.
- **Exit codes** of the extension commands: `0` ok, `1` not found, `2` registry unreachable, `4` credentials refused or missing, `5` not locked, unallowed, unsigned, mismatched or incompatible.
- **Registry credentials** live in the selected environment (`extensions.registry.<host>.token` or `.username` and `.password`, as secrets), never in the model or lock. See [environments and secrets](../cratis-environments-and-secrets/SKILL.md).
- **Swapping providers.** A replacement must offer the same abilities, triggers, components, properties and signals with the same typed shape; mismatches are reported at each use (`PLAY1112`, unknown ability).

## Configure and run

Settings, named secrets and activation are set in the model and supplied by the environment; see [environments and secrets](../cratis-environments-and-secrets/SKILL.md). `cratis run <model> -e <environment>` admits the model's locked extensions and hands the Stage their settings and secrets; read the CLI `run` page.

## Boundaries

- Declaring a system and invoking its abilities from the model is [automations and translations](../cratis-screenplay-automations-and-translations/SKILL.md).
- Placing a package component on a screen is [UI composition](../cratis-screenplay-ui-composition/references/extension-components.md).
- Do not publish, push to a registry or change an allow-list unless the user asked for that effect.
