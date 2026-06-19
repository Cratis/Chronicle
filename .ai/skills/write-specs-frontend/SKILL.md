---
name: write-specs-frontend
description: Use this skill to write specs for the React/TypeScript surface of a Cratis APPLICATION slice — view models, helpers, and component behavior (Vitest + Mocha-style describe/it + Sinon + Chai .should). The frontend peer to write-specs (backend). For framework @cratis/* package specs use cratis-specs-typescript instead.
---

Write specs for a slice's React/TypeScript surface — view models, helpers, and narrow component behavior. Full conventions live in `.ai/rules/frontend-testing.md`; this skill is the **application** frontend peer to **write-specs** (backend). For **framework** `@cratis/*` package specs (the `given()` helper), use **cratis-specs-typescript** instead.

## Placement & naming

Specs live next to the unit, snake_case so the path reads as a sentence:

```
<Slice>/
  <Subject>.ts
  for_<Subject>/
    when_<context>/
      and_<extra_context>.ts
```

`describe('when ...')` names the scenario, not the class; `it('should ...')` uses **spaces**; each `it` asserts one observable outcome.

## Stack

Vitest (`describe`/`beforeEach`/`it`) · SinonJS (`sinon.stub()` / `createStubInstance`) · Chai **`.should`** fluent — never `expect()`.

## What to cover

- **View models** (the primary target): state transitions, computed getters, filtering/sorting/selection, wizard steps, validation state, command-value derivation.
- **Command orchestration outside `CommandDialog`:** unauthorized / invalid / exception / success handling.
- **Pure helpers:** parsing, formatting, grouping, boundary cases.
- **Component rendering** only when markup, disabled state, or wrapper behavior is the point.

**Don't spec:** generated Cratis proxies, PrimeReact/framework internals, CSS pixel-perfection, snapshots, or trivial presentational pass-throughs.

## View-model spec (default)

View models are plain classes constructible directly — no React hooks; no direct `window`/`localStorage`/timers/network (inject the abstraction); derived values are getters.

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

Pass small typed fakes / `sinon.stub()` for dependencies; don't build a broad harness until a second spec reuses it.

## Component spec (when rendering is the point)

Environment is `node`; render server-side markup and mock only the external component boundary:

```ts
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { vi } from 'vitest';

vi.mock('primereact/dialog', () => ({
    Dialog: (p: { footer?: React.ReactNode; children?: React.ReactNode }) =>
        React.createElement('div', null, p.footer, p.children),
}));

describe('when rendered while busy', () => {
    let html: string;
    beforeEach(() => { html = renderToStaticMarkup(React.createElement(MyDialog, { isBusy: true })); });
    it('should disable the confirm button', () => html.should.include('disabled'));
});
```

Add jsdom / Testing Library only when DOM events are truly required; avoid snapshots.

## Async & time

`vi.useFakeTimers()` / `vi.useRealTimers()` in `afterEach`; never depend on the real date, random values, network, or storage. Clear the Arc query caches in teardown for suspense-query tests.

## Gate

Run `yarn test` (or the project's frontend test gate) plus `yarn lint` and `npx tsc -b` — specs complement lint and build, they don't replace them.

## See also

- `frontend-testing.md` — the full conventions this applies.
- `react.md` — MVVM, view-model testability, Arc hooks.
- `write-specs` — the backend peer; `cratis-specs-typescript` — framework `@cratis/*` package specs.
