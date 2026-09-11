---
name: cratis-application-react-specifications
description: Write specifications for the React and TypeScript surface of a Cratis application slice — view models, helpers, command orchestration, and narrow component behavior — using Vitest with Mocha-style describe/it, Sinon, and the Chai should interface. Use when adding or changing frontend behavior in an application that consumes Cratis. Do not use for backend scenarios or for specifications inside a Cratis framework package.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-application-react-specifications/SKILL.md -->

# Cratis application React specifications

Frontend specifications are specifications, not tests. The `for_ / when_ / and_`
shape is deliberately the same BDD language as the backend, so one subject, one
context, clear setup, and small assertions read as sentences.

This is the **application** frontend peer of the backend slice specification
skill. Specifications for a Cratis framework package (`@cratis/*`) follow that
repository's own TypeScript specification conventions instead.

## Stack

- **Vitest** for the runner, with Mocha-style `describe` / `beforeEach` / `it`.
- **Sinon** for stubs and fakes (`sinon.stub()`, `sinon.createStubInstance()`).
- **Chai** with the **`.should` fluent interface** — never `expect()`.
  `result.should.equal(expected)` reads as a sentence.

## Placement and naming

Specifications live next to the unit they specify, in snake_case so the path
reads as a scenario sentence:

```
<Slice>/
    <Subject>.ts
    for_<Subject>/
        when_<context>/
            and_<extra_context>.ts
```

| Shape | Use for |
| --- | --- |
| `for_<Subject>/when_<context>/and_<extra>.ts` | the default — view models, branching helpers, component specs |
| `for_<Subject>/when_<context>.ts` | a tiny pure helper with no useful extra context |

`describe('when ...')` names the scenario, not the class. `it('should ...')`
uses spaces, not underscores, and asserts one observable outcome. Do not add
`.test.ts` or `.spec.ts` files unless the project's configuration changes
deliberately.

## What to specify

Behavior that can regress without TypeScript catching it:

- **View models** — the primary target. State transitions, computed getters,
  filtering, sorting, selection, wizard steps, validation state, and
  command-value derivation.
- **Command orchestration outside a command dialog** — the unauthorized,
  invalid, exception, and success branches.
- **Pure helpers** — parsing, formatting, grouping, boundary cases.
- **Component rendering** only when the markup, a disabled state, or wrapper
  behavior is genuinely the point.

Do **not** specify generated Cratis proxies, PrimeReact or framework internals,
CSS pixel-perfection, snapshots, or trivial presentational pass-throughs.

## View-model specification — the default shape

A view model is a plain class: constructible directly, with no React hooks, no
direct `window`, `localStorage`, timer, or network access (inject the Cratis
abstraction instead), and derived values as getters.

```ts
import { describe, beforeEach, it } from 'vitest';
import { CandidatesViewModel } from '../../CandidatesViewModel';

describe('when filtering by search text', () => {
    let viewModel: CandidatesViewModel;

    beforeEach(() => {
        viewModel = new CandidatesViewModel();
        viewModel.setSearch('senior');
    });

    it('should keep only matching candidates', () =>
        viewModel.filteredItems.should.have.lengthOf(1));
});
```

`beforeEach` does the arrange and act once for the context; each `it` asserts
one outcome. Pass small typed fakes or `sinon.stub()` instances for
dependencies, and do not build a shared harness until a second specification
actually reuses it.

A view model that cannot be constructed without React is doing too much or
depending on the wrong abstraction — that is a design signal, not a testing
problem.

## Command-result branches

When a command runs outside a command dialog, each granular flag is a branch
worth specifying: `isAuthorized`, `isValid` with its `validationResults`,
`hasExceptions`, and the success path. Stub the command and return a result
object per branch rather than reaching for the network.

## Component specification — when rendering is the point

The environment is `node`. For behavior that does not need browser events,
render server-side markup and mock only the external component boundary:

```ts
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, beforeEach, it, vi } from 'vitest';

vi.mock('primereact/dialog', () => ({
    Dialog: (props: { footer?: React.ReactNode; children?: React.ReactNode }) =>
        React.createElement('div', null, props.footer, props.children),
}));

describe('when rendered while busy', () => {
    let html: string;

    beforeEach(() => {
        html = renderToStaticMarkup(React.createElement(MyDialog, { isBusy: true }));
    });

    it('should disable the confirm button', () => html.should.include('disabled'));
});
```

Prefer typed mock props over `any`. Add jsdom or Testing Library only when the
behavior truly requires DOM events — do not make it the default. Avoid
snapshots; they hide behavior and make refactors noisy.

Cratis Components mount inside `CratisComponentsProvider`, so a specification
that renders one for real must provide it, and jsdom needs a `ResizeObserver`
stub for the table and split-pane components.

## Async and time

Use `vi.useFakeTimers()` for time-dependent behavior and restore with
`vi.useRealTimers()` in `afterEach`. Never depend on the real current date,
random values, the network, or browser storage. For suspense-query
specifications, clear the Arc query caches in teardown with
`clearSuspenseQueryCache()` and `clearSuspenseObservableQueryCache()` from
`@cratis/arc.react/queries`.

## Stories are not specifications

Storybook covers visual states, documentation, and manual verification; Vitest
covers behavior. A component with non-trivial logic usually needs both. A story
file is colocated with its component, uses
`satisfies Meta<typeof Component>` rather than a type annotation, and carries
`tags: ['autodocs']`. Write stories for reusable components — shared primitives
and slice-internal components reused across slices — not for live slice pages
wired to Arc query and command hooks; exercise those in the running app.

An interaction (`play:`) function belongs on stateful components only, and it is
documentation and QA rather than the enforced gate. The enforced behavioral gate
is the Vitest specification.

## The engineering bar these specifications serve

- No placeholder or dead UI ships for behavior the code actually implements.
- Components stay small and single-responsibility; a `// Section` comment inside
  a component means that section is its own component.
- State logic lives in a view model or a tested state module, not in the
  component — extract as soon as there are three or more `useState` calls, any
  state-synchronizing `useEffect`, or derived values.
- Reuse an existing Cratis Components wrapper or shared primitive before adding
  a new component.
- `unknown` or a real type, never `any`.
- After editing a `.tsx`, read past its final closing brace to confirm no stale
  return block or unreachable code remains.

## Gate

Run the project's frontend test gate (typically `yarn test`) plus lint and the
TypeScript build. Specifications complement lint and build; they do not replace
them.

## Verify

- Files sit in `for_<Subject>/when_<context>/` beside the unit, in snake_case.
- `describe` names the scenario; `it` descriptions start with "should" and use
  spaces.
- Assertions use the Chai `.should` interface, never `expect()`.
- Each `it` asserts one observable outcome.
- View models are constructed directly, with stubs for dependencies and no
  React involved.
- No specification covers a generated proxy, a framework internal, or a
  snapshot.
- Fake timers are restored in `afterEach` and query caches are cleared for
  suspense specifications.
- Lint, the frontend test gate, and the TypeScript build all pass.
