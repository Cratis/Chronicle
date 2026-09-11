<!-- cratis-ai-managed: skills/cratis-specifications-typescript/references/typescript-patterns.md -->
# TypeScript specification patterns

Detail for the `given()` surface: assertions, stubbing, asynchronous
specifications, and folder layout.

## Frameworks

| Framework | Role |
| --- | --- |
| Vitest | Runs the specifications |
| Mocha-style structure | `describe`, `it`, `beforeEach`, `afterEach` |
| Chai | Assertions, always through `.should` |
| Sinon | Stubbing and call verification |

Run specifications with `yarn test` from the package root.

## Chai assertions

Always use `.should`. Never use `expect()`.

```ts
// Equality
value.should.equal(<expected>);
value.should.deep.equal({ <property>: <value> });

// Booleans
flag.should.be.true;
flag.should.be.false;

// Null and undefined
value.should.be.null;
value.should.not.be.null;
value.should.be.undefined;
value.should.not.be.undefined;

// Collections
items.should.contain(<expected>);
items.should.have.lengthOf(<count>);
items.should.be.empty;
items.should.not.be.empty;

// Types
value.should.be.instanceOf(<Type>);

// Throwing
(() => <throwingCall>()).should.throw(<ErrorType>);
```

Assert on values and types, never on a presentation message string.

## Sinon stubbing

```ts
import sinon from 'sinon';

// Stub an entire class — every method becomes a stub
const <collaborator> = sinon.createStubInstance(<CollaboratorClass>);

// Stub a global
const <stub> = sinon.stub(globalThis, '<globalName>');
<stub>.resolves(<value>);

// Configure return values
<collaborator>.<method>.returns(<value>);
<collaborator>.<asyncMethod>.resolves(<value>);

// Verify calls
<collaborator>.<method>.calledOnce.should.be.true;
<collaborator>.<method>.calledWith(<expected>).should.be.true;
<collaborator>.<method>.callCount.should.equal(<count>);

// Inspect an individual call
const firstCall = <collaborator>.<method>.firstCall;
firstCall.args[0].should.equal(<expected>);

// Restore global stubs
afterEach(() => sinon.restore());
```

`sinon.restore()` matters only for stubs installed on shared objects such as
`globalThis`. A `createStubInstance` built inside a context class is recreated
per specification by `given()` and needs no restore.

## The `given()` helper

`given()` instantiates the context class, hands it to the suite, and keeps
setup isolated per specification.

```ts
import { given } from '../../given';
import { a_<system_under_test> } from '../given/a_<system_under_test>';

describe('when <behavior>', given(a_<system_under_test>, context => {
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

Import `given` from the package root. The relative depth of that import depends
on how deep the specification sits in the `for_`/`when_` tree.

## Reusable context class

```ts
// given/a_<system_under_test>.ts
import sinon from 'sinon';
import { <ClassName> } from '../../../<ClassName>';

export class a_<system_under_test> {
    <collaborator>: sinon.StubbedInstance<<CollaboratorType>>;
    <sut>: <ClassName>;

    constructor() {
        this.<collaborator> = sinon.createStubInstance(<CollaboratorClass>);
        this.<collaborator>.<method>.returns(<default>);
        this.<sut> = new <ClassName>(
            this.<collaborator> as unknown as <CollaboratorType>);
    }
}
```

Properties are public so the specification can read them through `context`.
Configure default stub behavior in the constructor and override it per
specification where an outcome needs a different value.

## Asynchronous specifications

`beforeEach`, `afterEach`, and `it` may each be `async`:

```ts
describe('when <behavior>', given(a_<system_under_test>, context => {
    let result: <ResultType>[];

    beforeEach(async () => {
        result = await context.<sut>.<action>(<input>);
    });

    it('should <expected outcome>', () => {
        result.should.have.lengthOf(<count>);
    });
}));
```

Await the promise in `beforeEach` rather than inside each `it()`, so the action
under specification runs exactly once.

## One outcome per file

```
when_<behavior>/
├── with_<valid_condition>.ts      → the happy path
├── with_<edge_condition>.ts       → an edge case
└── without_<requirement>.ts       → the failure path
```

Each file carries its own `describe`, its own `beforeEach`, and its own `it()`
assertions. Allowed prefixes: `and_`, `with_`, `without_`, `having_`, `given_`.
