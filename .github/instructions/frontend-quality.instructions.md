---
applyTo: "**/*.{ts,tsx}"
profile: application
paths:
  - "**/*.ts"
  - "**/*.tsx"
---

# Frontend Code Quality

The frontend is software engineering, held to the same bar as the backend — and it is the part most prone to silent degradation under iterative AI editing: orphaned code, duplicated components, sprawling files, shipped placeholders, untested state machines hidden in JSX. This rule is the maintainability contract that prevents that. It complements [react.md](./react.md) (architecture), [typescript.md](./typescript.md) (style), and [frontend-testing.md](./frontend-testing.md) (specs).

Aim for CUPID code: **Composable**, **Unix-like** (small focused pieces), **Predictable**, **Idiomatic**, **Domain-based**.

## Non-negotiables

1. **Never ship placeholder or dead UI as if it were real.** "Coming soon", lorem-ipsum, fake counts, and stubbed fixtures must not reach a screen backing a feature the code actually implements. An empty state is the *designed* empty state for that surface, not a build-time apology. When you implement the real behavior, delete the placeholder and its strings in the same change.
2. **Components stay small and single-responsibility.** A `// Section` comment inside a component means that section is its own component. A presentational `.tsx` over ~150 lines, or with more than one clear responsibility, is decomposed. Parent owns state; children receive props.
3. **State logic lives in a view model, not the component.** Per [react.md](./react.md): extract a `withViewModel` view model (or a tested state module) as soon as a component has 3+ `useState`, any `useCallback`/state-syncing `useEffect`, or derived values. `useCallback`/`useMemo` inside a `withViewModel` component is a smell. Never store React state in a view model; never call hooks inside one — inject the Cratis abstraction. A view model must be constructible in a spec without React.
4. **Reuse before rewrite.** Check the shared component surface and existing style constants before adding a component or a long class string. Never duplicate a primitive that already exists; prefer a Cratis Components wrapper or an existing shared component over a new one.
5. **Test behavior, not implementation trivia.** Non-trivial view-model/helper logic gets BDD specs (see [frontend-testing.md](./frontend-testing.md)). Storybook is visual coverage, not a replacement for behavioral specs.
6. **No `any`, descriptive names, no orphaned code.** `unknown` or a real type, never `any`. After editing a `.tsx`, read past its last closing `};` to confirm no stale `return (...)` block, duplicate body, or unreachable code remains. Comment only a non-obvious *why* — no "what"/provenance comments.

## Smell guards

Stop-and-fix signals during implementation and review:

| Smell | Why it matters | Preferred move |
|---|---|---|
| New component added without checking the shared component surface and `@cratis/components` | Duplicates fragment the design system | Reuse an existing primitive/wrapper, or document the missing one |
| Slice-local helper promoted to the shared component folder after one use | Freezes a one-off as a shared API | Keep it beside the slice until a second real consumer appears |
| Raw `primereact/*` import where a Cratis wrapper exists | Bypasses Arc behavior, overlay fixes, validation | Use the `@cratis/components` subpath wrapper |
| Import from the `@cratis/components` root barrel in new code | Pulls optional-peer-heavy exports, hides intent | Import from subpaths (`CommandDialog`, `DataPage`, `DataTables`, `Dialogs`, `Dropdown`, `Toolbar`, `Common`) |
| Component API has many boolean visual flags (`primary`, `selected`, `muted`, …) | State combinations become undefined | One typed `variant`/`tone` union, or split components |
| A reusable component reaches into domain strings, generated proxies, identity, query, command, or navigation | It is no longer a primitive | Move it to the slice, or make it explicitly Cratis-aware and test/story it as such |
| Class/style string copied 3× or grown hard to scan | Styling changes become search-and-replace bugs | Extract to a shared style constant or a focused child component |
| `style={{ ... }}` with static token names | Bypasses theming and review | Move to `className`; keep `style` only for runtime values |

## Definition of done for a frontend change

- [ ] No placeholder/stub copy ships for behavior that is actually implemented.
- [ ] Each component is single-responsibility; oversized components were split.
- [ ] State/logic crossing the [react.md](./react.md) thresholds lives in a view model.
- [ ] Reused existing primitives/wrappers instead of duplicating.
- [ ] No smell guard above is triggered without a short, intentional reason.
- [ ] File read past its final `};` — no orphaned code.
- [ ] New/changed reusable or stateful components have Storybook stories.
- [ ] New/changed view-model/helper behavior has BDD specs (see [frontend-testing.md](./frontend-testing.md)).
- [ ] Lint, conditional test, and build pass (the project's frontend gates).

## Review discipline

Treat the frontend gates as seriously as the backend build/test gates — a green lint is the floor, not the goal. For any non-trivial frontend change, run a correctness review (and a reuse/dead-code pass) before completing it. Degradation accrues one "it lints, ship it" at a time.

## See also

- [react.md](./react.md) — MVVM, Cratis Components, queries/commands.
- [typescript.md](./typescript.md) — style, type discipline.
- [frontend-testing.md](./frontend-testing.md) — BDD Vitest specs and layout.
