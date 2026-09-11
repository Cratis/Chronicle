---
name: cratis-specifications-typescript
description: Write TypeScript specifications in the Cratis BDD style using the given() helper, reusable context classes, Sinon stubbing, and the Chai .should fluent interface. Use when adding or restructuring TypeScript specifications, building a given/ context class, or laying out a for_/when_ specification folder. Do not use for C# specifications, and do not use it to decide what the code under specification should do.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-specifications-typescript/SKILL.md -->

# Cratis TypeScript specifications

Specifications describe behavior, not implementation. The `given()` helper and
its context class are the TypeScript counterpart of the C# `Specification`
base: setup is separated from the action, and each `it()` verifies one outcome.

## Verified product sources

This skill is verified against the frameworks these specifications run on:

| Framework | Role |
| --- | --- |
| Vitest | Test runner (`yarn test` from the package root) |
| Mocha-style structure | `describe`, `it`, `beforeEach`, `afterEach` |
| Chai | Assertions, always through the `.should` fluent interface |
| Sinon | Stubbing and call verification |

The `given()` helper is supplied by the Cratis TypeScript package under
specification and is imported from that package root. Reverify the helper's
location and signature against the owning repository before relying on a
different import path.

## Route near misses

- The specification is C#: use `cratis-specifications-csharp` instead.
- The subject is an application view model or React component rather than a
  Cratis TypeScript package: the plain `describe`/`beforeEach` frontend style
  applies, not the `given()` helper in this skill.
- The question is what the code under specification *should do*: resolve the
  behavior first; this skill only specifies already decided behavior.

## Step 1 — Create the folder structure

Specifications live alongside the source they describe:

```
for_<ClassName>/
├── given/
│   └── a_<system_under_test>.ts   ← reusable context class
├── when_<behavior>/               ← a behavior with multiple outcomes
│   ├── with_<condition>.ts
│   ├── without_<condition>.ts
│   └── and_<condition>.ts
└── when_<simple_behavior>.ts      ← a single outcome is a single file
```

Paths read as English sentences. Allowed outcome prefixes are `and_`, `with_`,
`without_`, `having_`, and `given_`.

**`when` belongs only in a `when_<behavior>` folder name.** A specification file
or non-`when_` folder must never contain the word `when`. Fold the context into
the `when_` folder name and use preposition files for the outcomes. Add a
sub-folder under `when_` only when that condition has its own multiple outcomes.

## Step 2 — Write a reusable context class

```ts
// for_<ClassName>/given/a_<system_under_test>.ts
import sinon from 'sinon';
import { <ClassName> } from '../../../<ClassName>';

export class a_<system_under_test> {
    <collaborator>: sinon.StubbedInstance<<CollaboratorType>>;
    <sut>: <ClassName>;

    constructor() {
        this.<collaborator> = sinon.createStubInstance(<CollaboratorClass>);
        this.<collaborator>.<method>.returns(<default>);
        this.<sut> = new <ClassName>(this.<collaborator>);
    }
}
```

Context properties are **public** — the specification reads them through the
`context` parameter. This differs from C#, where `given/` fields are
`protected`. Name the class `a_` or `an_` so it reads as "given a service, when
registering".

A context captures the world as it exists *before* the action. Never put the
action under specification in a context.

## Step 3 — Write the specification

```ts
// for_<ClassName>/when_<behavior>/with_<condition>.ts
import { a_<system_under_test> } from '../given/a_<system_under_test>';
import { given } from '../../given';

describe('when <behavior> with <condition>', given(a_<system_under_test>, context => {
    let result: <ResultType>;

    beforeEach(async () => {
        result = await context.<sut>.<action>(<input>);
    });

    it('should <expected outcome>', () => {
        result.should.equal(<expected>);
    });

    it('should <other expected outcome>', () => {
        context.<collaborator>.<method>.calledOnce.should.be.true;
    });
}));
```

`given()` instantiates the context class per specification so setup stays
isolated. `beforeEach` performs the single action under specification; each
`it()` asserts one outcome.

## Step 4 — Skip the context when nothing is shared

A behavior with no shared setup needs no context class:

```ts
describe('when <behavior>', () => {
    let result: <ResultType>;

    beforeEach(() => {
        result = <TypeUnderSpecification>.<action>(<input>);
    });

    it('should <expected outcome>', () => {
        result.<property>.should.equal(<expected>);
    });
});
```

## Step 5 — Assert with the `.should` fluent interface

**Always use `.should`. Never use `expect()`.** The fluent style reads as a
sentence and matches the rest of the corpus.

```ts
value.should.equal(<expected>);
value.should.deep.equal(<expectedObject>);
flag.should.be.true;
value.should.be.null;
value.should.not.be.undefined;
items.should.contain(<expected>);
items.should.have.lengthOf(<count>);
items.should.be.empty;
value.should.be.instanceOf(<Type>);
(() => <throwingCall>()).should.throw(<ErrorType>);
```

Full Sinon and Chai detail is in
[typescript-patterns.md](references/typescript-patterns.md).

## Step 6 — Apply the naming conventions

| Element | Convention | Example |
| --- | --- | --- |
| `describe()` text | Natural-language sentence | `'when registering with a valid name'` |
| `it()` text | Starts with "should", **uses spaces** | `'should append an event'` |
| Context class | `a_` or `an_` prefix | `an_author_service` |
| Specification file | Preposition prefix | `with_valid_name.ts` |

Use spaces in `it()` descriptions, never underscores — they appear in runner
output as human-readable sentences. This is the deliberate difference from the C#
`should_` method naming.

## Step 7 — Keep one behavior per file

Do not mix orthogonal behaviors in one specification. Separate folders per
behavior keep failures precise:

```
when_items_are_added_as_delta/and_item_is_identified_by_a_guid.ts
when_items_are_removed_as_delta/and_item_is_identified_by_a_guid.ts
```

A single file validating both add-delta and remove-delta behavior tangles
unrelated outcomes and hides which one broke.

## What not to specify

- Simple property getters and setters.
- Properties that return a constructor parameter directly.
- Trivial delegation.
- Anything a `describe` beginning "when getting" or "when returning" would
  describe — that is a getter, not a behavior.

## Verify

- Each specification file states one `describe`, one action in `beforeEach`, and
  one or more single-outcome `it()` assertions.
- No path contains `when` outside a `when_<behavior>` folder name.
- Outcome files use an allowed preposition prefix.
- Context classes carry public properties, are named `a_`/`an_`, and contain no
  action under specification.
- Every assertion uses `.should`; no `expect()` appears.
- `it()` descriptions start with "should" and use spaces.
- Stubs are restored between specifications where Sinon state would otherwise
  leak.
- Nothing trivial is specified.
- `yarn test` passes from the package root.
