---
name: cratis-components-styling
description: Style and theme an application built on Cratis Components — the stylesheet imports an app must make, the --cratis-* design-token layer and the PrimeReact token chain behind it, choosing between the baseline theme, a styled preset, a custom palette, and fully unstyled, dark mode, the PrimeReact pass-through prop, and where Tailwind actually fits. Use when setting up a new Cratis frontend, changing colors or theming, fixing components that render unstyled, or reaching into a component's internal DOM. Do not use for component API or page composition questions.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-styling/SKILL.md -->

# Styling Cratis Components

Cratis Components builds on PrimeReact 11, which is **unstyled-first**: it ships
no CSS and renders no class names of its own. Everything a Cratis application
looks like comes from the layers below. Getting the setup wrong shows up as
components rendering completely unstyled, which is the single most common
first-run problem.

## Verified product sources

| Package | Version | Verified from |
| --- | --- | --- |
| `@cratis/components` | `3.0.0` | its package manifest, `Styled/`, and the CSS sources |
| `primereact` | `^11.0.0` | peer of `@cratis/components@3.0.0` |
| `@primereact/core`, `@primereact/headless`, `@primereact/hooks` | `^11.0.0` | peers of `@cratis/components@3.0.0` |
| `@primereact/styles`, `@primereact/types`, `@primeuix/themes` | optional peers | `@primereact/styles`/`types` `^11.0.0`, `@primeuix/themes` `^3.0.0` |
| `primeicons` | `^8.0.0` | peer of `@cratis/components@3.0.0` |

PrimeReact is a **peer dependency** — install it in the application yourself.
Two copies mean two provider contexts, which breaks overlays and pass-through
silently. Remove any `resolutions` or `overrides` pin that used to work around
this.

## Step 1 — What an app must do

Two things, or the components render unstyled:

1. **Mount `CratisComponentsProvider`** (from `@cratis/components/Common`)
   above every Cratis component. It wraps the PrimeReact provider and merges
   your configuration over the Cratis defaults, so anything you pass wins.
2. **Import the stylesheets explicitly**, in this order — components no longer
   import their own CSS:

   ```ts
   import '@cratis/components/tokens';   // the --cratis-* layer every component reads
   import '@cratis/components/styles';   // every component stylesheet, in one file
   import '@cratis/components/theme';    // optional: the license-free baseline look
   ```

Order matters: both `styles` and `theme` consume the tokens. The `styles` entry
also vendors the split-pane CSS the `DataPage` details pane needs, so importing
it is not optional even in a fully custom-styled app.

## Step 2 — Pick one of four setups

| Setup | What you do | When |
| --- | --- | --- |
| **Baseline theme** | import `tokens` + `styles` + `theme`, add the theme class to a root element | you want a working look with no license and little effort |
| **Styled mode** | import `tokens` + `styles`, pass the Cratis styled-mode configuration to the provider | you want a full PrimeReact-preset look |
| **Custom palette** | styled mode with your own preset | you have brand colors |
| **Fully unstyled** | import `tokens` + `styles`, supply your own pass-through or CSS | you have your own design system |

A preset **alone** is not one of these. Passing `theme: { preset }` to the
provider emits the PrimeReact token variables but styles nothing that Cratis
Components renders, because those are PrimeReact *primitives* — they render
data attributes rather than class names, so a preset has nothing to attach to.
That is exactly what the styled-mode helper fixes.

### Styled mode

`@cratis/components/styled` exports the pieces:

- `CratisPreset` — a PrimeReact preset derived from Lara with the Cratis blue
  primary ramp and a deliberate one-step surface shift so dark mode matches the
  previous Cratis look.
- `primeReactStyles` — the component-defaults map that glues the PrimeReact
  primitive styles onto the primitives Cratis Components renders. This is the
  part a bare preset is missing.
- `styledMode(options?)` — returns `{ theme, defaults }` ready to hand to the
  provider.

```tsx
import { CratisComponentsProvider } from '@cratis/components/Common';
import { styledMode } from '@cratis/components/styled';

<CratisComponentsProvider value={styledMode()}>
    <App />
</CratisComponentsProvider>
```

`styledMode` accepts `preset` (yours instead of `CratisPreset`),
`darkModeSelector`, and `cssLayer`. Its defaults are the dark-mode selector
`.cratis-dark` and a CSS layer named `primereact` ordered
`theme, base, primereact, components, utilities`. That order is deliberate: the
theme sits above Tailwind's `base` so preflight cannot strip table and input
padding, and below `components` and `utilities` so a utility class still wins.

### Dark mode

Toggle the `cratis-dark` class on the root element. The baseline theme scopes
its dark palette to that class, and styled mode uses it as the preset's dark
selector by default.

## Step 3 — Colors: use the token layer

Never hard-code a hex or `rgb()` value for UI chrome — it breaks the moment the
theme changes. Read a `--cratis-*` custom property instead.

The chain is: **preset (JavaScript) → `--p-*` (runtime) → `--cratis-*` (the
tokens stylesheet) → component CSS.** Each `--cratis-*` token resolves a
PrimeReact 11 token with a PrimeReact 10 name as fallback, so both eras work.
The tokens are intentionally fallback-free at the end of the chain: if nothing
resolves, the rule no-ops rather than painting a wrong color.

The full vocabulary:

| Group | Tokens |
| --- | --- |
| Surfaces | `--cratis-surface-0`, `--cratis-surface-100`, `--cratis-surface-ground`, `--cratis-surface-section`, `--cratis-surface-card`, `--cratis-surface-overlay`, `--cratis-surface-hover`, `--cratis-surface-border` |
| Text | `--cratis-text-color`, `--cratis-text-color-secondary` |
| Primary | `--cratis-primary-color`, `--cratis-primary-color-text`, `--cratis-primary-300`, `--cratis-primary-400`, `--cratis-primary-500`, `--cratis-primary-600` |
| Highlight | `--cratis-highlight-bg`, `--cratis-highlight-text-color` |
| Semantic | `--cratis-green-500`, `--cratis-orange-500`, `--cratis-red-500` |
| Geometry | `--cratis-border-radius` |
| Effects | `--cratis-focus-ring`, `--cratis-maskbg` |

Only hard-code a color that is intentionally theme-independent — a brand accent
dot, a traffic-light indicator.

A back-compatibility stylesheet
(`@cratis/components/primereact-v10-palette`) republishes the PrimeReact 10
names (`--surface-ground`, `--text-color`, `--primary-color`, and the numbered
ramps) for code that has not migrated. Import it to keep an old application
running; **write nothing new against those names.**

## Step 4 — Writing styles

- Put static styles in a **co-located `.css` file** and reference it from the
  application's stylesheet manifest. Never write `import './Foo.css'` inside a
  `.tsx` — a CSS file in the JavaScript module graph is what made the published
  Cratis Components package unloadable in Node, and the library's own build now
  fails if a component stylesheet is only reachable that way.
- One CSS file per component. A composition root's CSS carries layout and
  positioning for its children, not the children's own styling.
- Use inline `style` **only** for runtime-dynamic values such as computed
  pixel positions. A `style` object full of static token names bypasses theming
  and review — move it to a class.
- Name classes with a prefix matching the component.

## Step 5 — Pass-through: reaching a component's internals

PrimeReact's pass-through (`pt`) prop targets the internal parts of a rendered
component; `ptOptions` controls how your values merge with existing ones, and
`unstyled` opts a component out of the theme entirely. Cratis Components
forwards all three, typed against the underlying primitive, on roughly thirty
components — every command-form field, the buttons, dialogs, dropdowns, data
tables, the toaster, and more.

Three shapes to expect:

1. **A single `pt`** on most components.
2. **Named pass-throughs** where a component wraps two primitives —
   `DataPage` takes `tablePt` / `tablePtOptions` / `tableUnstyled` and
   `menubarPt` / `menubarPtOptions` / `menubarUnstyled`;
   `StepperCommandDialog` uses plain `pt` for the inner stepper and `dialogPt`
   for the outer dialog. Applying `pt` and expecting it to reach the other
   element is the usual cause of a pass-through that seems to do nothing.
3. **Composite pass-throughs**, such as the toaster's `{ region, toast }`.

There is **no global Cratis pass-through preset shipped** — the library's
defaults are deliberately empty so an application's configuration always wins.
Supply your own through the provider's value when you want an app-wide preset:

```tsx
<CratisComponentsProvider value={{ pt: myAppPreset }}>
```

A few components expose `className` only and have no pass-through of their own —
`SchemaEditor`, `ObjectContentEditor`, and `ObjectNavigationalBar`. Restyle
those through the global preset. `BusyIndicatorDialog` is also global-preset
only, because its request type is owned by the Arc React package.

### Pass-through as attribute removal

Setting a pass-through value to `undefined` **removes** that attribute. Cratis
Components uses this deliberately to strip invalid ARIA that PrimeReact 11 emits
— for example clearing `role` and `aria-controls` off stepper headers whose
target ids are never rendered. It is a legitimate tool when you need to delete
an attribute rather than add one.

### The compatibility contract

`@cratis/components/compatibility` exports a machine-checkable description of
which pass-through keys and slots this major version supports against which
PrimeReact major, plus an assertion helper. Use it in a specification when your
application depends on reaching a specific internal part, so a PrimeReact
upgrade that moves the part fails loudly instead of silently.

## Where Tailwind fits

Tailwind is **one supported path, not the Cratis default.** Cratis Components
compiles Tailwind utilities with **preflight deliberately excluded**, so
Tailwind's base resets never strip the component styling. If your application
enables preflight, keep the CSS layer order from styled mode so the theme still
sits above `base`.

Do not reach for Tailwind utilities as the primary styling mechanism for Cratis
components; reach for the token layer and pass-through first.

## Verify

- PrimeReact and its `@primereact/*` peers are installed in the application, at
  one version each.
- `CratisComponentsProvider` wraps the tree.
- `@cratis/components/tokens` and `@cratis/components/styles` are imported once
  at the entry point, tokens first.
- Exactly one of the four setups is chosen — a bare preset with no styled-mode
  defaults is not one of them.
- No hex or `rgb()` value is hard-coded for UI chrome; colors read `--cratis-*`.
- No new code is written against the PrimeReact 10 palette names.
- No `.tsx` imports a `.css` file.
- Inline `style` carries only runtime-dynamic values.
- Pass-through props target the intended element, especially on `DataPage` and
  `StepperCommandDialog`.
- Dark mode is toggled with the `cratis-dark` class on the root element.
