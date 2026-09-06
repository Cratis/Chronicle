# PrimeUI License

The Workbench frontend is built on [Cratis Components](https://github.com/Cratis/Components), which in turn
builds on PrimeReact. From version 11, PrimeReact is PrimeTek's commercially licensed **PrimeUI** rather
than the MIT-licensed library version 10 was. It verifies a license key when its provider mounts, and
without one it logs a warning and renders an *"Invalid PrimeUI License"* banner over the application — in
development and production alike, whatever the styling configuration.

Cratis is open source and qualifies for the free **Community** tier. The terms and the eligibility rules
are at [primeui.dev/licenses/community](https://primeui.dev/licenses/community).

## Contributing without a key

**You do not need a license key to contribute.** Everything builds, every spec runs, and the Workbench is
fully usable — the banner is the only difference. Pull requests from forks build without one, because
GitHub does not expose repository secrets to fork builds.

The banner is deliberately not suppressed. Hiding it would make a licensing problem invisible rather than
absent, and an unlicensed build is exactly the thing a contributor should be able to see at a glance.

## Supplying a key

The key is read from the environment as `PRIMEUI_LICENSE`. It is configuration, not source: it differs
between machines and builds, and it must never be committed.

Vite reads the **process environment** as well as `.env` files, so either of these works:

```bash
# A shell profile - one export serves every Cratis application on the machine
export PRIMEUI_LICENSE=your-key-here
```

```bash
# Or per-repository, in Source/Workbench/.env (git-ignored; see .env.example)
PRIMEUI_LICENSE=your-key-here
```

The variable keeps PrimeTek's own name rather than a Chronicle-specific one, so a single value covers this
repository, Studio, and any other Cratis application you have checked out. `Source/Workbench/.frontend`
exposes both the `CHRONICLE_` and `PRIMEUI_` prefixes to the bundle for exactly this reason.

Restart the dev server after setting it — Vite reads the environment at startup.

## How it reaches the application

| Piece | Role |
|---|---|
| [`vite.config.ts`](https://github.com/Cratis/Chronicle/blob/main/Source/Workbench/.frontend/vite.config.ts) | Exposes the `PRIMEUI_` prefix alongside `CHRONICLE_` |
| [`primeUiLicense.ts`](https://github.com/Cratis/Chronicle/blob/main/Source/Workbench/.frontend/primeUiLicense.ts) | The single accessor; resolves an empty value to `undefined` |
| [`index.tsx`](https://github.com/Cratis/Chronicle/blob/main/Source/Workbench/.frontend/index.tsx) | Passes it to `CratisComponentsProvider` |
| [`.storybook/preview.tsx`](https://github.com/Cratis/Chronicle/blob/main/Source/Workbench/.frontend/.storybook/preview.tsx) | The same, so Storybook renders unbannered too |
| [`vite-env.d.ts`](https://github.com/Cratis/Chronicle/blob/main/Source/Workbench/.frontend/vite-env.d.ts) | Types it, so it is discoverable rather than a magic string |

## Continuous integration

Both the pull-request build and the release build pass the key from a repository secret, also named
`PRIMEUI_LICENSE`. Because the environment variable and the secret share a name, no per-workflow
translation is involved:

```yaml
env:
    PRIMEUI_LICENSE: ${{ secrets.PRIMEUI_LICENSE }}
```

It is set in [`workbench-build.yml`](https://github.com/Cratis/Chronicle/blob/main/.github/workflows/workbench-build.yml) and
[`publish.yml`](https://github.com/Cratis/Chronicle/blob/main/.github/workflows/publish.yml). A fork's pull request builds with it unset, which is
a supported outcome rather than a failure.
