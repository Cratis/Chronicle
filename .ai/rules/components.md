---
applyTo: "**/*.tsx"
paths:
  - "**/*.tsx"
profile: application
---

# Building React Components

## Cratis Components — pick the wrapper, import from subpaths

Reach PrimeReact almost exclusively through Cratis Components wrappers. Import from **subpaths**, not the root barrel (the barrel pulls optional-peer-heavy exports):

| Need | Use | Subpath |
|---|---|---|
| Page chrome | `Page` | `@cratis/components/Common` |
| Query list page | `DataPage` | `@cratis/components/DataPage` |
| Standalone query table | `DataTableForQuery` / `DataTableForObservableQuery` | `@cratis/components/DataTables` |
| Form section | `FormElement` | `@cratis/components/Common` |
| Icon | `Icon` / `IconDisplay` | `@cratis/components/Common` |
| Dropdown | `Dropdown` | `@cratis/components/Dropdown` |
| Command dialog | `CommandDialog` / `StepperCommandDialog` | `@cratis/components/CommandDialog` |
| Data/confirmation dialog | `Dialog` / `ConfirmationDialog` / `BusyIndicatorDialog` | `@cratis/components/Dialogs` |
| Command form fields | `InputTextField`, … | `@cratis/components/CommandForm` |
| Canvas tool palette | `Toolbar` | `@cratis/components/Toolbar` |

Use `Dropdown` from `@cratis/components/Dropdown` (not raw `primereact/dropdown`) — it appends to the document body and stacks correctly above overlays, avoiding the z-index issues raw PrimeReact dropdowns have inside dialogs.

### `DataPage` — query list pages

`DataPage` (from `@cratis/components/DataPage`) owns the data table's subscription, paging, selection, action menubar, and details split — **do not pre-fetch rows and pass an `items` array**. Required props: `title`, `query` (`Constructor<TQuery>`; snapshot and observable queries are auto-detected), `emptyMessage`, and `children`. Other props: `queryArguments`, `dataKey` (pass whenever the read model has an identity), `selection` / `onSelectionChange`, `globalFilterFields` / `defaultFilters` / `clientFiltering`, `detailsComponent` (`React.FC<IDetailsComponentProps<T>>` = `{ item, onRefresh? }`), `onRefresh`, and PrimeReact pass-through `tablePt`/`tableClassName`/`menubarPt`/`menubarClassName`.

Columns and toolbar actions are compositional children:

```tsx
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import { Column } from 'primereact/column';

<DataPage title="Accounts" query={AllAccounts} emptyMessage="No accounts yet.">
    <DataPage.Columns>
        <Column field="name" header="Name" />
    </DataPage.Columns>
    <DataPage.MenuItems>
        <MenuItem label="Add" command={() => showAdd()} disableOnUnselected={false} />
    </DataPage.MenuItems>
</DataPage>
```

`MenuItem` is a PrimeReact menu item (use `command`, not `onClick`); `disableOnUnselected` greys it out until a row is selected. See the **cratis-react-page** skill for the full page workflow.

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

Consistent styling comes from discipline: static styles in CSS files, dynamic values inline, and colors always from PrimeReact's design tokens. This ensures theming works automatically and no component breaks the visual language.

- Use **CSS classes in co-located `.css` files** for static styles.
- Each component must have its own CSS file — never add sub-component styles to the parent's CSS. This keeps styles co-located with the component they belong to.
- The composition root's CSS only contains layout/grid rules for positioning children — it should not style the children themselves.
- Use inline `style` props **only** for runtime-dynamic values (pixel positions, computed sizes).
- Use **PrimeReact CSS variables** for all colors, backgrounds, borders. This ensures the application respects theming and dark/light mode switches:
  - `var(--surface-0)` through `var(--surface-900)`, `var(--surface-card)`, `var(--surface-border)`, `var(--surface-ground)`
  - `var(--text-color)`, `var(--text-color-secondary)`, `var(--primary-color)`, `var(--primary-color-text)`, `var(--highlight-bg)`
  - Never hard-code hex or `rgb()` for UI chrome — it will break when themes change. Only hard-code colors that are intentionally theme-independent (e.g. brand-specific accent dots, traffic-light indicators).
- Name CSS classes with a BEM-like prefix matching the component name.

## Props

Props are a component's public API. They should be clear, minimal, and well-documented.

- Each sub-component declares its own `*Props` interface with JSDoc on every prop.
- Pass only needed props — avoid threading large prop bags through component trees.
- Event handlers follow `on*` naming: `onPointerDown`, `onSelect`.

## Dialogs

See [dialogs.md](./dialogs.md) for the full dialog guide.

**Summary:** Never import `Dialog` from `primereact/dialog`. Use `CommandDialog` from `@cratis/components/CommandDialog` for command-executing dialogs and `Dialog` from `@cratis/components/Dialogs` for data-collection dialogs. Do not render manual `<Button>` components for dialog actions — the dialog components handle footers.

## Icons

Follow these rules when working with SVG icons:

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
