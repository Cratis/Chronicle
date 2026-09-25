---
applyTo: "**/*.tsx"
paths:
  - "**/*.tsx"
profile: application
---
<!-- cratis-ai-managed: rules/components.md -->

# Building React Components

The frontend is composed from **Cratis Components** (`@cratis/components` **4.x**). Components owns its markup, styling contract and public types; React Aria supplies focus, overlay, collection and date behavior *internally* — you never import or style it. Components declares **no** PrimeReact, PrimeIcons or PrimeUI dependency or peer, and it ships no vendor CSS. PrimeReact (11 and 10) and MUI exist only as optional *presentation adapters* (`@cratis/components.primereact`, `.primereact10`, `.mui`) that adapt nine primitive slots — button, icon-button, text input, text area, checkbox, radio, switch, progress bar, surface — and nothing else. Dialogs, dropdowns, tables, date pickers, toasts and steppers are always Components-owned.

## Import from subpaths — the root is setup-only

The package root exports **only** setup (`CratisComponentsProvider`, configuration and message types). Every component ships from its own subpath; the Components 3 root namespaces are gone, and `@cratis/eslint-plugin-components`' `no-root-barrel-import` keeps them gone.

| Need | Use | Subpath |
|---|---|---|
| App root | `CratisComponentsProvider` | `@cratis/components` |
| Page chrome, buttons, basic controls | `Page`, `Button`, `IconButton`, `TextInput`, `TextArea`, `NumberInput`, `Checkbox`, `Radio`, `Switch`, `Tooltip`, `ErrorBoundary`, `FormElement`, `ComboBox` (≥ 4.6.0 — single-selection, text-searching entity picker) | `@cratis/components/Common` |
| Icon | `Icon` (type) / `IconDisplay` | `@cratis/components/Common` |
| Query list page | `DataPage`, `MenuItem`, `Column` | `@cratis/components/DataPage` |
| Standalone query table | `DataTableForQuery` / `DataTableForObservableQuery`, `Column` | `@cratis/components/DataTables` |
| Dropdown | `Dropdown` | `@cratis/components/Dropdown` |
| Command dialog | `CommandDialog`, `StepperCommandDialog`, `StepperPanel` | `@cratis/components/CommandDialog` |
| Standalone stepper | `CommandStepper` | `@cratis/components/CommandStepper` |
| Data / confirmation / busy dialog | `Dialog`, `ConfirmationDialog`, `BusyIndicatorDialog` | `@cratis/components/Dialogs` |
| Command form fields | `InputTextField`, `NumberField`, `DropdownField`, `CheckboxField`, `CalendarField`, …, `AutoCommandForm` | `@cratis/components/CommandForm` |
| Notifications | `Toaster`, `toast`, `toastCommandResult` | `@cratis/components/Notifications` |
| Status & display | `Tag`, `Badge`, `Chip`, `Skeleton`, `Avatar`, `ProgressBar`, `ProgressSpinner`, `Message` | `@cratis/components/Display` |
| Canvas tool palette | `Toolbar`, `ToolbarButton`, … | `@cratis/components/Toolbar` |
| Shared types | `JsonSchema`, `Json`, … | `@cratis/components/types` |

Use `Dropdown` from `@cratis/components/Dropdown`, never a vendor select: it renders through the Components overlay environment, so it stacks correctly above dialogs, and it follows the WAI-ARIA button/listbox (or combobox, when `filter` is on) pattern. Its `onChange` receives the **value**, not an event — as do every CommandForm field's.

### Notifications — feedback for commands run outside a dialog

`CommandDialog` handles success/error feedback itself. For a command executed
**programmatically** (`command.execute()` outside a dialog), mount the toast region once —
`toaster` on `CratisComponentsProvider`, or one `<Toaster />` near the root — and surface the result
with `toastCommandResult` (`@cratis/components/Notifications`). It maps the granular
`ICommandResult` flags to the right toast (success, not-authorized, validation with per-field
messages, exceptions — never stack traces):

```tsx
const result = await command.execute();
if (toastCommandResult(result, { successTitle: 'Author registered' })) refresh();
```

For ad-hoc notifications, call the imperative `toast.success/info/warn/error(...)` — each takes an
**options object**, not a bare string: `toast.info({ title: 'Saved', description: '…' })`.

### Column filtering & display components

`<Column>` supports `sortable` and `filter` (a per-column filter popup with match modes), and
`DataPage` / the data tables show a search box when `globalFilterFields` is set. **Sorting and
filtering apply to the currently loaded page**; complete-result filtering belongs in the query's
arguments and the server-side query, before paging (`clientFiltering` is a deprecated no-op). Use the
`Display` components (`Tag`, `Badge`, `Skeleton`, …) for status indicators and loading states.

### `DataPage` — query list pages

`DataPage` (from `@cratis/components/DataPage`) owns the data table's subscription, paging,
selection, action menubar, and details split — **do not pre-fetch rows and pass an `items` array**.
Required props: `title`, `query` (`Constructor<TQuery>`; snapshot and observable queries are
auto-detected), `emptyMessage`, and `children`. Other props: `queryArguments`, `dataKey` (pass
whenever the read model has an identity), `selection` / `onSelectionChange`, `globalFilterFields` /
`defaultFilters`, `detailsComponent` (`React.FC<IDetailsComponentProps<T>>` = `{ item, onRefresh? }`),
`onRefresh`, and the Cratis part props `tablePt: DataTableParts` / `paginatorPt: TablePaginatorParts`
/ `menubarPt: ButtonParts` (plus `tableClassName` / `menubarClassName`).

Columns and toolbar actions are compositional children:

```tsx
import { DataPage, MenuItem, Column } from '@cratis/components/DataPage';

<DataPage title="Accounts" query={AllAccounts} emptyMessage="No accounts yet.">
    <DataPage.Columns>
        <Column field="name" header="Name" />
    </DataPage.Columns>
    <DataPage.MenuItems>
        <MenuItem label="Add" command={() => showAdd()} disableOnUnselected={false} />
    </DataPage.MenuItems>
</DataPage>
```

`MenuItem` is a declarative Components element read by `DataPage.MenuItems` (use `command`, not
`onClick`); `disableOnUnselected` greys it out until a row is selected; its `icon` is a **React
component type** (e.g. a `react-icons` icon), not an `Icon`. See the **cratis-arc-react-page** skill
for the full page workflow.

## Composition over Monoliths

A well-built component tree is like a well-organized kitchen — every tool has a place, and you can find what you need without opening every drawer. Large components that do everything are hard to understand, hard to test, and hard to change without breaking something unrelated.

- Split components into small, focused pieces and compose them together. Each component should have a single, clear responsibility.
- Parent components own state and event handlers; children receive props. This makes data flow predictable and debuggable.
- If you find yourself writing a block comment like `// Author list section` inside a component, that section should be its own component. The comment is a code smell — the component name should provide that context instead.

## Folder Structure

- Single-file component → place directly in the parent feature folder.
- Multi-file component (sub-components, hooks, CSS) → create a folder named after the component:

```
PrototypeWindow/
  PrototypeWindow.tsx      ← composition root
  PrototypeWindow.css      ← styles for the composition
  TitleBar.tsx             ← sub-component
  CanvasArea.tsx           ← sub-component
  ResizeHandle.tsx         ← sub-component
  index.ts                 ← re-exports public API
```

Add an `index.ts` that re-exports the public surface so import paths stay stable.

## Styling

Consistent styling comes from discipline: static styles in CSS files, dynamic values inline, and colors always from the `--cratis-*` design tokens. This ensures theming works automatically and no component breaks the visual language.

### App setup — the imports Cratis Components needs

An app does three things, once, at its root — or the components render unstyled:

1. **Import the stylesheets, in this order** — components do not import their own CSS:

   ```ts
   import '@cratis/components/tokens';   // the semantic --cratis-* variables every component reads
   import '@cratis/components/styles';   // structural rules for every component, in low-priority Cratis layers
   import '@cratis/components/theme';    // optional: the baseline light/dark look (auto, or `cratis-dark`/`cratis-light`)
   ```

   A product with its own design system omits `theme`, defines the `--cratis-*` variables itself, and imports those values *after* the two Components entries so they win the cascade.

2. **Mount `CratisComponentsProvider`** (from the package root) around the app. Its `value` carries `locale` (BCP 47 — drives dates, numbers, keyboard behavior and announcements) and `messages` (the Components-owned labels for `paginator`, `datePicker`, `dropdown`, `dialog`, `stepper`, `notifications`, `dataTable`, `columnFilter`, `toolbar`); `toaster` mounts the toast region. Renderer keys from Components 3 — `license`, `theme`, `pt`, `ripple`, `unstyled`, z-index — are a **type error**, on purpose. Keep the `value` object stable when it carries a large message catalog.

   ```tsx
   import { CratisComponentsProvider } from '@cratis/components';

   export const App = () => (
       <CratisComponentsProvider value={{ locale: 'en-US' }} toaster>
           <Application />
       </CratisComponentsProvider>
   );
   ```

3. **Install nothing else.** No PrimeReact, no PrimeIcons, no theme preset, no vendor provider. Only if the product wants a vendor's *look* on the nine primitive slots does it install one adapter package and pass its manifest to the provider's `library` prop — and the vendor's own provider, theme and license then stay the application's, outside Components.

### Writing styles

- Use **CSS classes in co-located `.css` files** for static styles; each component has its own CSS file — never add sub-component styles to the parent's CSS.
- The composition root's CSS only contains layout/grid rules for positioning children — it should not style the children themselves.
- Use inline `style` props **only** for runtime-dynamic values (pixel positions, computed sizes).
- Use the **`--cratis-*` tokens** for every color, background, border, shadow and radius, so theming and dark mode work automatically:
  - surfaces — `var(--cratis-surface-0)`, `var(--cratis-surface-100)`, `var(--cratis-surface-card)`, `var(--cratis-surface-ground)`, `var(--cratis-surface-section)`, `var(--cratis-surface-overlay)`, `var(--cratis-surface-hover)`, `var(--cratis-surface-border)`
  - text & accent — `var(--cratis-text-color)`, `var(--cratis-text-color-secondary)`, `var(--cratis-primary-color)`, `var(--cratis-primary-color-text)`, `var(--cratis-highlight-bg)`, `var(--cratis-highlight-text-color)`, `var(--cratis-focus-ring)`
  - status — `var(--cratis-success-background)`/`-text`, `var(--cratis-warning-*)`, `var(--cratis-danger-*)`, `var(--cratis-info-*)`
  - shape — `var(--cratis-border-radius)`, `var(--cratis-control-height)`, `var(--cratis-shadow-subtle)`/`-overlay`/`-dialog`
  - Never hard-code hex or `rgb()` for UI chrome. Only hard-code colors that are intentionally theme-independent (brand accent dots, traffic-light indicators).
- **Reach inside a component through its parts, not its DOM.** Every meaningful element carries `data-cratis-part="<name>"` and state attributes (`data-selected`, `data-invalid`, `data-disabled`, `data-active`, `data-position`). Style them with those selectors, or pass per-part attributes through the component's `pt` prop typed by its `*Parts` type (`DialogParts`, `StepperParts`, `DataTableParts`, `TablePaginatorParts`, `ButtonParts`, the `Toolbar*Parts` family). Never target React Aria class names or the internal element structure — they are not a contract.
- Name CSS classes with a BEM-like prefix matching the component name.

## Props

Props are a component's public API. They should be clear, minimal, and well-documented.

- Each sub-component declares its own `*Props` interface with JSDoc on every prop.
- Pass only needed props — avoid threading large prop bags through component trees.
- Event handlers follow `on*` naming: `onPointerDown`, `onSelect`.
- Model visual variation the way `Button` does — one typed `variant` (`solid | outline | ghost | link`), one `tone` (`neutral | accent | positive | caution | critical`), one `shape` — not a pile of boolean flags.

## Dialogs

See [dialogs.md](./dialogs.md) for the full dialog guide.

**Summary:** Use `CommandDialog` from `@cratis/components/CommandDialog` for command-executing dialogs and `Dialog` from `@cratis/components/Dialogs` for data-collection dialogs; confirmations and busy indicators are raised through hooks, never built. Never import a vendor dialog. Do not render manual `<Button>` components for dialog actions — the dialog components handle footers.

## Icons

Components accepts an **`Icon`** (`@cratis/components/Common`) wherever a control takes an icon: either a React node — preferred — or a complete, consumer-owned icon-font class string. Components installs **no icon font**: a string is rendered on an `<i>` unchanged, so the product must load that stylesheet itself. `react-icons` is a Components dependency and the natural source of React icon nodes (`<FaHouse aria-hidden="true" />`). Not every `icon` prop has that shape — `DataPage.MenuItem.icon` is a React component *type* because the page instantiates it; follow the exported prop type.

For the application's own SVG icons:

- **Distinguish icons from status/interactive components.** A pure SVG icon is a simple presentational element. A component that wraps an icon with interactive behavior (e.g. a dropdown, tooltip, or complex state) is a _component_, not an icon — name it accordingly (e.g. `SliceStatus`, not `SliceStatusIcon`).
- **Store each SVG as a separate `.svg` file** inside the icon's folder. Do not embed SVG markup directly in `.tsx` files.
  - Import SVG files with the `?raw` suffix to get the raw SVG string: `import iconSvg from './Icon.svg?raw';`
  - Render inline using `dangerouslySetInnerHTML={{ __html: iconSvg }}` so that CSS `currentColor` is honored.
- **Use subfolders for grouping related icons or complex components.**
  - A folder named `SliceStatus/` groups the four status SVG files together with the interactive `SliceStatus` component that uses them.
  - Simple, standalone icons may live directly in the `icons/` root if they have no related siblings.
- **Every icon folder must have an `index.ts`** that re-exports the public API, keeping import paths for consumers stable.
- **Barrel-export all icons through `icons/index.ts`** so consumers import from the `icons` path alias, not from deep paths.

**Example structure:**
```
icons/
  SliceStatus/
    NotStarted.svg         ← raw SVG file
    InProgress.svg         ← raw SVG file
    ReadyForReview.svg     ← raw SVG file
    Done.svg               ← raw SVG file
    SliceStatus.tsx        ← interactive component using the SVG files
    SliceStatus.css
    index.ts
  CogWheelIcon/
    CogWheel.svg           ← raw SVG file
    CogWheelIcon.tsx       ← thin wrapper component
    CogWheelIcon.css
    index.ts
  WireframeIcon.tsx        ← simple component with no SVG (stays at root)
  WireframeIcon.css
  index.ts                 ← re-exports everything
```

## Storybook

- Storybook runs at **http://localhost:6006** — never restart it.
- Use the `click` tool to interact with Storybook for visual verification.

## Verification

After every task, run both:
1. `yarn lint`
2. `npx tsc -b`

## README.md for Complex Components

Complex components accumulate knowledge that lives nowhere else — why a particular state structure was chosen, how sub-components divide responsibilities, what conventions the CSS follows. Without a README, the next developer (or AI) has to reverse-engineer all of this from the code.

Every component folder with sub-components, hooks, or non-trivial architecture **must** have a `README.md`.

**Before starting work:** Check for an existing README and read it first. It may contain context that changes your approach.

**A README must cover:**
- Component hierarchy — tree of components and what each owns
- Architecture decisions — what was chosen and why
- State management — where state lives, what each piece controls
- CSS conventions — patterns used across children
- How to extend — steps for common modifications

**Keep READMEs current** — update in the same commit when changing architecture, layout, or state structure.
