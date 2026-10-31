---
name: cratis-components-styling
description: Style and theme an application built on Cratis Components 4 — the three stylesheet entries an app must import, the --cratis-* design-token seam, choosing between the baseline theme, a product theme and fully owned styling, dark mode, the typed pt parts and data-cratis-part selectors, the optional renderer adapters, and where Tailwind actually fits. Use when setting up a new Cratis frontend, changing colors or theming, fixing components that render unstyled, or styling one part of a component. Do not use for component API or page composition questions.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-styling/SKILL.md -->

# Styling Cratis Components

Cratis Components 4 owns its markup and its styling contract. There is no
third-party theme engine, preset or provider underneath it: what a Cratis
application looks like is decided by three Cratis-owned stylesheet entries and
the `--cratis-*` custom properties they read. Getting the setup wrong shows up as
components rendering completely unstyled, which is the single most common
first-run problem — start by checking the CSS entry.

## Verified product sources

| Package | Verified from |
| --- | --- |
| `@cratis/components` | its package manifest (`exports`, no vendor UI dependency or peer), `Source/tokens.css`, `Source/MIGRATION.md`, `Documentation/Styling/*`, `Documentation/Common/cratis-components-provider.md`, `Documentation/renderers/*` |

Components declares **no** PrimeReact, PrimeIcons or PrimeUI dependency or peer.
React Aria supplies focus, overlay, collection and date behavior *internally* —
you never import it, never style its class names, and it never appears in an
application's manifest. Remove any leftover Prime packages that only Components
used to need (`Source/MIGRATION.md` lists them); keep one only if the application
itself still imports it directly, and then that island's provider, theme and
license are the application's.

## Step 1 — What an app must do

Two things, or the components render unstyled:

1. **Import the stylesheets, in this order**, in `index.css`; the entry point
   imports that file with `import './index.css'`:

   ```css
   @import '@cratis/components/tokens';
   @import '@cratis/components/styles';
   @import '@cratis/components/theme'; /* optional baseline */
   ```

   Direct CSS subpath imports from TypeScript need ambient declarations on
   TypeScript 6, otherwise TS2882. To avoid the roughly 32 KiB gzip aggregate,
   replace `styles` with `styles/base` and the needed `<subpath>/styles` entries
   (for example `Dialogs/styles`, `DataTables/styles`). Area order does not matter.

   `tokens` first — `styles` and `theme` both read it. `styles` also carries the
   split-pane CSS the `DataPage` details pane needs; use the aggregate or the
   corresponding area sheets even in a fully custom-styled app. `styles` contains **no** Tailwind Preflight, no
   global reset and no copy of the token values.

2. **Mount `CratisComponentsProvider`** (from the package root,
   `@cratis/components`) above every Cratis component. It carries `locale` and
   the Components-owned `messages`, and mounts the toast region with `toaster`;
   it carries **no** theme. `toaster` accepts `boolean | ToasterProps`: without
   it or a standalone `<Toaster />`, no toast appears; mount one, never both.
   Message groups are `paginator`, `datePicker`, `dropdown`, `dialog`, `stepper`,
   `notifications`, `dataTable`, `filter`, `columnFilter`, `toolbar`. `locales`
   is deprecated. `value.icons` replaces `close`, `remove`, `expand`,
   `sortAscending`, `sortDescending`, `previous`, `next`, `clear`; a component
   prop wins over the provider, then the built-in glyph (`null` draws none).
   `useCratisIcon()` resolves that vocabulary. Provider props also include
   `overlayEnvironment`, `rendererSetup` (non-secret booleans), experimental
   `libraryMode` (`strict` / `degrade`) and `rendererFallback` (`core` / `throw`).
   The Components 3 renderer keys — `license`, `theme`,
   `defaults`, global `pt`, `ptOptions`, `ripple`, `unstyled`, z-index — are a
   type error on purpose, so a migrated app cannot compile a provider whose
   visual configuration silently does nothing.

   ```tsx
   import { CratisComponentsProvider } from '@cratis/components';

   <CratisComponentsProvider value={{ locale: 'en-US' }} toaster>
       <App />
   </CratisComponentsProvider>
   ```

## Step 2 — Pick one of three setups

| Setup | Imports | You own |
| --- | --- | --- |
| **Baseline theme** | `tokens` + `styles` + `theme` | choosing `cratis-dark` / `cratis-light` / system preference; overriding only the brand values you mean to |
| **Product theme** | `tokens` + `styles` + *your* CSS, imported after them | mapping your complete palette onto `--cratis-*`; typography, spacing, motion, contrast; component treatment through parts |
| **Fully owned** | `tokens` + `styles` + your CSS | the same, plus every `--cratis-*` value — omit `theme` entirely |

A theme is **CSS**. No JavaScript preset, provider option or wrapper class is
required for the normal whole-application setup. The product's CSS is written
*outside* a cascade layer, so it wins over all three Components layers
(`cratis-theme`, `cratis-components`, `cratis-utilities`) without specificity
tricks for structural rules. Tokens and theme are unlayered: their overrides
still need the correct specificity. If the product uses layers, name the
`cratis-*` layers in its **first** layer-order statement, before any stylesheet
(see Tailwind below).

### Baseline theme and dark mode

`theme` adds document foreground/background, system dark-mode values, the
explicit scheme classes, forced-colors tuning and `.cratis-theme` subtree
defaults. It stays visually familiar to Components 2/3 — Lara-adjacent blue
actions, neutral surfaces, 6px radii — implemented entirely with Cratis tokens.

```ts
document.documentElement.classList.toggle('cratis-dark', darkMode);
```

- Without a class, the baseline follows `prefers-color-scheme`.
- `cratis-light` keeps light values when the OS prefers dark; an explicit light
  wins over an ambient root `cratis-dark`.
- An independently themed island puts `cratis-theme` on the subtree and adds
  `cratis-dark` or `cratis-light` there (on the same element, or on an ancestor).
- A plain `:root` override after `theme` loses to dark selectors such as
  `:root.cratis-dark`, `.cratis-theme.cratis-dark`, and the system-dark
  `:root:not(.cratis-light)`. Match the scheme selectors' specificity and place
  overrides after the import, including subtree selectors when using islands.
  Or omit `theme` and use the product-theme setup below.
- Requires Components 4.22.1: it maps Arc's `--color-error` / `--color-error-bg` in both dark
  schemes. A product theme omitting `theme` must define those variables itself;
  do not carry forward the old dark-error contrast workaround.

### Product theme

```css
:root {
    --cratis-primary-color: var(--brand-accent-700);
    --cratis-primary-color-text: var(--brand-text-inverse);
    --cratis-action-background: var(--brand-action);
    --cratis-action-background-hover: var(--brand-action-hover);
    --cratis-action-background-active: var(--brand-action-active);
    --cratis-action-text: var(--brand-on-action);
    --cratis-surface-ground: var(--brand-canvas);
    --cratis-surface-card: var(--brand-surface);
    --cratis-surface-overlay: var(--brand-surface);
    --cratis-surface-border: var(--brand-border);
    --cratis-control-background: var(--brand-control);
    --cratis-control-border: var(--brand-control-border);
    --cratis-text-color: var(--brand-text-primary);
    --cratis-text-color-secondary: var(--brand-text-secondary);
    --cratis-focus-ring: var(--brand-focus-ring);
}

[data-theme='dark'] {
    --brand-canvas: #171717;   /* switch schemes by redefining the *product* values under the product's own selector */
}
```

Nothing sits between the product tokens and the rendered component — no
renderer preset, no internal selector, no commercial theme package.

## Step 3 — Colors: use the token seam

Never hard-code a hex or `rgb()` value for UI chrome — it breaks the moment the
theme changes. Read a `--cratis-*` custom property instead. The full vocabulary
(from `tokens.css`):

| Group | Tokens |
| --- | --- |
| Accent | `--cratis-primary-color`, `--cratis-primary-color-text`, `--cratis-primary-300` … `-600`, `--cratis-primary-600-text` |
| Primary action | `--cratis-action-background` / `-hover` / `-active`, `--cratis-action-text` |
| Status pairs | `--cratis-info-background` / `-text`, `--cratis-success-*`, `--cratis-warning-*`, `--cratis-danger-*`; single indicators `--cratis-green-500`, `--cratis-orange-500`, `--cratis-red-500` |
| Surfaces | `--cratis-surface-ground` (page), `-section`, `-card`, `-overlay` (dialogs, popovers, toasts), `-hover`, `-border`, `-0`, `-100` |
| Controls | `--cratis-control-background`, `--cratis-control-border`, `--cratis-control-height` / `-small` / `-large` |
| Text & highlight | `--cratis-text-color`, `--cratis-text-color-secondary`, `--cratis-highlight-bg`, `--cratis-highlight-text-color` |
| Effects | `--cratis-focus-ring`, `--cratis-maskbg`, `--cratis-border-radius`, `--cratis-disabled-opacity`, `--cratis-shadow-subtle` / `-overlay` / `-dialog` / `-toast` |
| Layering | `--cratis-z-index-dialog` (1100), `-overlay` (1200), `-filter` (1250), `-tooltip` (1300), `-toast` (1400) |

Only hard-code a color that is intentionally theme-independent — a brand accent
dot, a traffic-light indicator.

## Step 4 — Writing styles

- Put static styles in a **co-located `.css` file**, one per component. A
  composition root's CSS carries layout and positioning for its children, not the
  children's own styling.
- Use inline `style` **only** for runtime-dynamic values such as computed pixel
  positions. A `style` object full of static token names bypasses theming and
  review — move it to a class.
- Name classes with a prefix matching the component.
- Import product CSS *after* the Components entries so it wins the cascade.
- When a customer supplies brand material, keep their CSS and tokens verbatim in product-owned files, map them onto the `--cratis-*` tokens, and use the baseline theme for every decision their material does not cover.

## Step 5 — Parts: reaching inside a component

Every meaningful element a component renders carries **`data-cratis-part`**, and
most components accept a **`pt`** prop typed by their own `*Parts` type. They
are documented separately because they are not always the same names: for most
components the typed `pt` key is the camelCase spelling of the kebab-case DOM
value (`headerRow` ↔ `header-row`), but the Toolbar family prefixes its DOM
values (`ToolbarButtonParts.root` ↔ `data-cratis-part='button'`,
`ToolbarFolderParts.root` ↔ `'toolbar-folder'`, `ToolbarFanOutParts.trigger` ↔
`'fanout-trigger'`). Read the DOM value off the rendered element (or the parts
manifest in `@cratis/components/types`) before writing a selector. Both spellings
are stable across internal foundation changes; React Aria class names and
undocumented DOM structure are not.

- **One instance:** `pt` — plain HTML attributes per part.

  ```tsx
  <Dropdown aria-label='Role' options={roles}
      pt={{ trigger: { className: 'product-select-trigger' }, popover: { className: 'product-select-popover' }, option: { className: 'product-select-option' } }} />
  ```

- **Product-wide:** a CSS rule on the part and its state attributes.

  ```css
  [data-cratis-part='header-cell'] { text-transform: uppercase; }
  [data-cratis-part='row'][data-selected='true'] { background: var(--product-selected-row); }
  .product-dialog[data-cratis-part='root'] { border-radius: 1rem; }
  ```

The typed part families: `ButtonParts`/`IconButtonParts` (`root`, `icon`,
`label`, `spinner`); `DialogParts` (`backdrop`, `positioner`, `root`, `header`,
`title`, `close`, `content`, `footer`, `confirm`, `cancel`); `DropdownParts`
(`root`, `trigger`, `value`, `clear`, `indicator`, `popover`, `listbox`, `option`,
`filter`, `multiple`); `DataTableParts` (`root`, `search`, `searchInput`,
`tableContainer`, `table`, `head`, `headerRow`, `headerCell`, `body`, `row`,
`cell`, `emptyRow`, `emptyCell`, `loadingRow`, `loadingCell`, `failureRow`,
`failureCell`); `TablePaginatorParts` (`root`, `range`, `info`,
`first`, `previous`, `next`, `last`); `StepperParts` (`root`, `list`, `step`,
`header`, `number`, `title`, `separator`, `panels`, `panel`);
`ToasterPassThrough` (`region`, `toast`, `icon`, `content`, `title`,
`description`, `action`, `close`); the `Toolbar*Parts` family; and per-field
parts for every CommandForm field. The Components `Styling/pass-through`
reference is the exhaustive table.

**Named parts where a component wraps two surfaces** — the usual cause of a
`pt` that "does nothing" is applying it to the wrong one:

- `DataPage`: `tablePt: DataTableParts`, `paginatorPt: TablePaginatorParts`, `menubarPt: ButtonParts`.
- `StepperCommandDialog`: `pt` is the inner **stepper**, `dialogPt` the outer **dialog**.
- `Column`: `filterPt: ColumnFilterMenuParts` for the filter popup.

**State attributes** are component-specific: `data-active`, `data-selected`,
`data-invalid`, `data-disabled`, `data-readonly`, `data-loading`, `data-position`,
`data-orientation`, `data-size`, `data-severity`. Do not assume one exists on
every component — read the component's parts reference.

`ptOptions` and `unstyled` are accepted for Components 3 source compatibility and
do **nothing**: part attributes always merge, and styling is always CSS-owned.
There is no global provider `pt`.

## Renderer adapters — when a vendor look is wanted

The default renderer is built in; omit `library` on the provider. Reach for an adapter only when the product wants a vendor look on nine primitive controls; mapping tokens is almost always the smaller change. The adapters cover nine primitive slots; the progress bar slot is `common.progress`, not `common.progressBar`. Read [references/renderer-adapters.md](references/renderer-adapters.md) when choosing or installing an adapter.

## Where Tailwind fits

Tailwind is **one supported way to write the CSS above, not the Cratis default.**
It is a fine tool for authoring the token mapping and part rules; it is not a
substitute for them. Components' own internal utilities are prefixed (`cratis:*`)
and are not public styling hooks. `styles` ships no Preflight and no reset of its
own. A host with layers must put this order in its **first** `@layer` statement,
parsed before any stylesheet (for example an inline `<style>` in the host):

```css
@layer properties, theme, base, cratis-theme, cratis-components, cratis-utilities, components, utilities;
```

Components layers belong above the product reset and below product utilities.
Declaring them after the imports cannot reorder layers already established;
otherwise component rules beat product utilities and `pt` classes lose.

## Verify

- No PrimeReact / PrimeIcons / PrimeUI package remains in the manifest unless the
  application itself imports it directly — and then it has its own provider.
- `CratisComponentsProvider` (root import) wraps the tree with locale/messages;
  exactly one toaster is mounted when notifications are used.
- `@cratis/components/tokens` and `@cratis/components/styles` are imported once
  at the entry point, tokens first; `theme` is imported or deliberately omitted;
  product CSS comes after.
- No hex or `rgb()` value is hard-coded for UI chrome; colors read `--cratis-*`.
- No CSS targets a React Aria class name or undocumented DOM; parts are reached
  through `pt` or `[data-cratis-part=…]`.
- Inline `style` carries only runtime-dynamic values.
- Named parts land on the intended surface (`tablePt` vs `menubarPt`; `pt` vs
  `dialogPt`).
- Dark mode is toggled with `cratis-dark` / `cratis-light` on the root element
  (or a `cratis-theme` subtree), not through a provider option.
