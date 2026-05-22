---
name: cratis-specs-typescript
description: Step-by-step guidance for writing TypeScript specs in Cratis using BDD-style Specification by Example — the given()/describe/it pattern, for_/when_/ folder hierarchy, reusable context classes, Sinon mocking, and Chai assertions. Use whenever writing TypeScript specs or tests, creating spec files/folders, using the given() helper, mocking with sinon.createStubInstance or sinon.stub, asserting with Chai .should, or understanding how yarn test runs specs.
---

## Core philosophy

Same BDD philosophy as C# specs — specs describe behaviors, not implementations. The `given()` helper + context class mirrors the C# `Specification` base class: setup is separated from the action, each `it()` verifies a single outcome.

---

## Step 1 — Create the folder structure

```
for_<ClassName>/
├── given/
│   └── a_<system>.ts             ← reusable context class
├── when_<behavior>/              ← behavior with multiple outcomes
│   ├── with_<condition>.ts
│   ├── without_<condition>.ts
│   └── and_<condition>.ts
└── when_<simple_behavior>.ts     ← single outcome = single file
```

Example:
```
for_EventsCommandResponseValueHandler/
├── given/
│   └── an_events_command_response_value_handler.ts
├── when_checking_can_handle/
│   ├── with_valid_events_collection.ts
│   ├── with_null_value.ts
│   └── without_event_source_id.ts
└── when_handling/
    ├── empty_events_collection.ts
    └── multiple_events_collection.ts
```

---

## Step 2 — Write a reusable context class

```ts
// for_AuthorService/given/an_author_service.ts
import sinon from 'sinon';
import { AuthorService } from '../../../AuthorService';

export class an_author_service {
    eventLog: sinon.StubbedInstance<IEventLog>;
    service: AuthorService;

    constructor() {
        this.eventLog = sinon.createStubInstance(EventLog);
        this.service = new AuthorService(this.eventLog);
    }
}
```

Properties are **public** (unlike C# protected fields) — tests access them via `context.propertyName`.

---

## Step 3 — Write a spec using `given()`

```ts
// for_AuthorService/when_registering/with_valid_name.ts
import { an_author_service } from '../given/an_author_service';
import { given } from '../../given';   // import from package root

describe('when registering with valid name', given(an_author_service, context => {
    beforeEach(async () => {
        await context.service.register('John Doe');
    });

    it('should append an event', () => {
        context.eventLog.append.calledOnce.should.be.true;
    });

    it('should pass the author name', () => {
        const call = context.eventLog.append.firstCall;
        call.args[1].name.should.equal('John Doe');
    });
}));
```

---

## Step 4 — Simple spec (no shared context)

For behaviors without shared setup:

```ts
describe('when replacing route parameters', () => {
    let result: { route: string; unusedParameters: object };

    beforeEach(() => {
        result = UrlHelpers.replaceRouteParameters('/api/items/{id}', { id: '123' });
    });

    it('should replace the route parameter', () => {
        result.route.should.equal('/api/items/123');
    });

    it('should remove used parameters', () => {
        Object.keys(result.unusedParameters).should.have.lengthOf(0);
    });
});
```

---

## Naming conventions

| Element | Convention | Example |
| --- | --- | --- |
| `describe()` text | Natural language sentence | `'when registering with valid name'` |
| `it()` text | Starts with "should", uses **spaces** | `'should append an event'` |
| Context class | `a_` or `an_` prefix | `an_author_service` |
| Spec file | Descriptive, `with_` / `without_` / `and_` | `with_valid_name.ts` |

**Always use spaces in `it()` descriptions** — never underscores.

---

## Behavior isolation rule

Keep one primary behavior per spec file/folder. Do not mix orthogonal behaviors in one spec.

- Good: separate folders for delta semantics:
    - `when_items_are_added_as_delta/and_item_is_identified_by_a_guid.ts`
    - `when_items_are_removed_as_delta/and_item_is_identified_by_a_guid.ts`
- Avoid: a single file that validates both add-delta and remove-delta behavior.

This keeps failures precise and prevents unrelated behavior from becoming tangled in one spec.

---

## Running specs

```bash
yarn test   # from the package root
```

---

## Reference files

- `references/typescript-patterns.md` — Chai assertions, Sinon patterns, async specs, full examples
