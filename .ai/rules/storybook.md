---
applyTo: "**/*.stories.tsx"
profile: application
paths:
  - "**/*.stories.tsx"
---

# Storybook Story Conventions

Stories are visual coverage and documentation for reusable React components built on Cratis Components — **not** the behavior test suite. Non-trivial view-model/helper behavior still needs BDD Vitest specs (see [frontend-testing.md](./frontend-testing.md)). Every story file is colocated with the component it covers.

## When to write a story

Write stories for **reusable components**: shared component-library primitives and slice-internal components reused across slices. Do **not** story live slice pages wired directly to Arc query/command hooks or a backend — exercise those in the running app. For a presentational view that needs visual coverage, extract it or render its presentational subcomponents with deterministic fixture props.

Good candidates: a presentational card/list-item/form-field with multiple visual states; a component with hover/selected/disabled/error variants; anything reusable across slices.

## Preview infrastructure (what you get for free)

A configured `.storybook/preview` should wrap every story in `CratisComponentsProvider` (from `@cratis/components/Common`) and import `@cratis/components/styles` so PrimeReact-based components render correctly. With that in place:

- **Auto prop tables** via `react-docgen-typescript` — a component's `interface` + per-prop JSDoc renders as a Docs prop table, and union props become `select` controls automatically. Keep a JSDoc comment on every prop. **`argTypes` is an override layer** (change control type, group under `table.category`, disable a control) — don't re-list every prop.
- **Accessibility panel** (`@storybook/addon-a11y`) scans each story with axe-core. Treat new violations as defects.

## Meta block

```tsx
import type { Meta, StoryObj } from '@storybook/react';
import { MyComponent } from './MyComponent';

const meta = {
    title: 'Components/MyComponent',          // sidebar path
    component: MyComponent,
    parameters: { layout: 'centered' },       // 'centered' | 'padded' | 'fullscreen'
    tags: ['autodocs'],                       // always include
} satisfies Meta<typeof MyComponent>;

export default meta;
type Story = StoryObj<typeof meta>;
```

Use `satisfies Meta<typeof Component>` — never `: Meta<T>` (better inference). `layout`: `centered` for atoms, `padded` for compound/container components, `fullscreen` for layout shells.

## Required stories

Adapt depth to the component — rich for complex/stateful, trimmed for atoms:

1. **`Playground`** — controllable: `args` for the happy path, all props tweakable. Use `fn()` from `storybook/test` for callbacks (not `() => {}`).
2. **Showcase** — one or more `render:` stories covering the meaningful axes (`Variants`/`Tones`, `Sizes`, `States`, `WithIcons`). Use the Arc story-kit (`StoryContainer`, `StorySection`, `StoryGrid`, `StoryDivider`, `StoryBadge` from `@cratis/arc.react/stories`) for layout. Note: `StorySection` has no `label` prop (use an `<h3>` inside); `StoryGrid` has no `columns` prop (it auto-wraps).
3. **`InContext`** — the component in a small realistic composition.
4. **`Interactive`** — for **stateful** components: a local `const Demo: React.FC` owning `useState`, plus a `play:` test. **Omit for presentational atoms.**

Add a one-line `/** … */` doc above each story; set `parameters.docs.description.component` on the meta.

## Interaction tests (`play:`) — stateful components only

```tsx
import { expect, fn, userEvent, within } from 'storybook/test';

export const Interactive: Story = {
    render: () => <Demo />,
    play: async ({ canvasElement }) => {
        const canvas = within(canvasElement);
        await userEvent.click(canvas.getAllByRole('tab')[1]);
        await expect(canvas.getAllByRole('tab')[1]).toHaveAttribute('aria-selected', 'true');
    },
};
```

- **Stateful only** — presentational atoms get no `play:`. Thin PrimeReact wrappers (overlay-portal components) are excluded — their behavior is the framework's.
- Prefer role/structure queries over localized text. `play:` runs in the Interactions panel (documentation/QA); the enforced behavioral gate is the Vitest spec.

## Router-dependent components

Wrap with `MemoryRouter` via a story decorator when a component uses `Link`/`useNavigate`.

## Gate

`build-storybook` should be green in CI — it compiles every story and `play:` function (catching broken imports/decorators) but does not execute play assertions.

## See also

- [react.md](./react.md) — component patterns; [components.md](./components.md) — component structure/styling.
- [frontend-testing.md](./frontend-testing.md) — BDD Vitest specs (the behavioral gate).
