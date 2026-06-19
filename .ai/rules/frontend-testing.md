---
applyTo: "**/for_*/**/*.{ts,tsx}"
profile: application
paths:
  - "**/for_*/**/*.ts"
  - "**/for_*/**/*.tsx"
---

# Frontend Testing

Frontend tests are specifications. The `for_/when_/and_` shape is intentionally the same BDD language as the backend Cratis Specifications: one subject, one context, clear setup, small assertions, no framework noise. When in doubt, mirror the structure in the Cratis Components source before inventing a new style.

The quality goal is CUPID code: frontend logic that cannot be tested in isolation is usually doing too much or depending on the wrong abstraction.

## Frameworks

- [Vitest](https://vitest.dev/) for running tests; Mocha-style `describe`/`it`/`beforeEach` structure.
- [SinonJS](https://sinonjs.org) for mocking/stubbing.
- [Chai](https://www.chaijs.com) — **always the `.should` fluent interface**, never `expect()`. `result.should.equal(expected)` reads as a sentence and matches the project's prose preference.

## Location and naming

Tests live next to the unit they specify:

```
<Slice>/
    <Subject>.ts
    for_<Subject>/
        when_<context>/
            and_<extra_context>.ts
```

| Shape | Use for |
|---|---|
| `for_<Subject>/when_<context>/and_<extra>.ts` | default — view models, helpers with branches, component specs |
| `for_<Subject>/when_<context>.ts` | tiny pure-helper spec with no useful extra context |

snake_case file/folder names so the path reads as a scenario sentence. Use `should` in `it()` descriptions (spaces, not underscores — TS specs read as human sentences). Don't add `.test.ts`/`.spec.ts` files unless the config changes intentionally.

## BDD structure

```ts
import { describe, beforeEach, it } from 'vitest';
import { MyViewModel } from '../../MyViewModel';

describe('when filtering active candidates', () => {
    let viewModel: MyViewModel;

    beforeEach(() => {
        viewModel = new MyViewModel();
        viewModel.setSearch('senior');
    });

    it('should keep candidates matching the search text', () => {
        viewModel.filteredItems.should.have.lengthOf(1);
    });
});
```

- `describe('when ...')` names the scenario, not the class.
- `beforeEach` does the arrange/act once for the context; each `it('should ...')` asserts one observable outcome.

## What to test

Behavior that can regress without TypeScript catching it:

- View-model state transitions, computed getters, filtering/sorting/selection, wizard steps, validation state, command-value derivation.
- Pure helpers: parsing, formatting, path lookup, grouping, boundary cases.
- Command orchestration outside `CommandDialog`: handling of unauthorized / invalid / exception / success outcomes.
- React component rendering only when markup, wrapper behavior, disabled state, or integration with a wrapped component is the point.

**Do not test:** generated Cratis proxies; framework/PrimeReact internals; CSS pixel-perfection; trivial presentational pass-throughs with no branch/derived behavior.

## View models

Plain TypeScript classes constructible directly in a spec:

- No React hooks; no direct `window`/`localStorage`/`location`/timers/network — inject the Cratis/browser abstraction (see [react.md](./react.md)).
- Derived values are getters, not `useMemo`.
- Commands are methods with explicit inputs, not inline closures hidden in JSX.

Pass small typed fakes or `sinon.stub()` instances for dependencies; avoid broad harnesses until a second spec reuses them.

## Component specs

The environment is `node`. For behavior that doesn't need browser events, use server-rendered markup:

```ts
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { vi } from 'vitest';

vi.mock('primereact/dialog', () => ({
    Dialog: (props: { footer?: React.ReactNode; children?: React.ReactNode }) =>
        React.createElement('div', null, props.footer, props.children),
}));

describe('when rendered while busy', () => {
    let html: string;
    beforeEach(() => { html = renderToStaticMarkup(React.createElement(MyDialog, { isBusy: true })); });
    it('should disable the confirm button', () => { html.should.include('disabled'); });
});
```

- Mock only the external component boundary needed; prefer typed mock props over `any`.
- Add jsdom / Testing Library only when behavior truly requires DOM events; don't make it the default.
- Avoid snapshots — they hide behavior and make refactors noisy.

## Stories vs tests

Storybook covers visual states, documentation, and manual verification; Vitest covers behavior. A component with non-trivial logic usually needs both — Storybook for variants/interactive state, Vitest for view-model/helper behavior and narrow rendered-component behavior.

## Async and time

Use `vi.useFakeTimers()` for time-dependent behavior; restore with `vi.useRealTimers()` in `afterEach`. Never depend on the real current date, random values, network, or browser storage. For suspense-query tests, clear the Arc query caches in teardown.

## Gate

Run the project's frontend test gate (e.g. `yarn test`) for frontend changes; it complements lint and build, it does not replace them.

## See also

- [react.md](./react.md) — MVVM, view-model testability, Arc hooks.
- [frontend-quality.md](./frontend-quality.md) — the engineering bar these specs serve.
- [specs.md](./specs.md) — the backend BDD language this mirrors.
- skill: **write-specs-frontend** — the step-by-step workflow that applies these conventions.
