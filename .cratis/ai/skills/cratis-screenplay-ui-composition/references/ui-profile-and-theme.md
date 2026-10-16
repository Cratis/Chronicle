<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/ui-profile-and-theme.md -->
# UI profile and theme

This reference holds the `ui profile` and `theme` excerpt and its rules for platforms, sizes, package order and theme compatibility.

## `ui profile` and `theme`

Excerpt: `AppShell` is the layout above.

```screenplay excerpt
ui profile Desktop
  target platform web
  target size regular
  layout AppShell
  theme Nordic
  packages
    Cratis.Components

theme Nordic
  compatible with Cratis.Components
```

- `target platform` takes a comma-separated list. `web`, `ios` and `android` are
  the documented examples; ⚠️ **the parser accepts any identifier** — there is no
  enforced list, so a typo is silent.
- `target size` is documented as `compact`, `regular` or `expanded`; ⚠️ again any
  identifier parses.
- `packages` are listed **in override-priority order** — a later package's `Button`
  shadows an earlier one's — and `core`, the built-in vocabulary, is always the
  final fallback.
- Component package metadata is owned by the package/runtime catalog; the model
  selects packages and icons, never copying metadata into `.play`.
- `compatible with` lists the packages a theme actually supports, each at most
  once. A profile selecting a theme not declared compatible with one of its own
  packages gets a compile-time **warning**.
