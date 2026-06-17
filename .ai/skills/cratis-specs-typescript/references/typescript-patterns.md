# TypeScript Spec Patterns — Reference

## Frameworks

| Framework | Role |
| --- | --- |
| [Vitest](https://vitest.dev/) | Test runner |
| [Mocha](https://mochajs.org/) | Test structure (`describe`, `it`, `beforeEach`) |
| [Chai](https://www.chaijs.com/) | Assertions — always `.should` fluent interface |
| [SinonJS](https://sinonjs.org/) | Mocking and stubbing |

---

## Chai assertions — always use `.should`

Never use `expect()`. The `.should` style reads as a natural English sentence.

```ts
// Equality
value.should.equal(expected);
value.should.deep.equal({ id: 1, name: 'John' });

// Booleans
flag.should.be.true;
flag.should.be.false;

// Null / undefined
value.should.be.null;
value.should.not.be.null;
value.should.be.undefined;
value.should.not.be.undefined;

// Arrays
array.should.contain(item);
array.should.have.lengthOf(3);
array.should.be.empty;
array.should.not.be.empty;

// Types
value.should.be.instanceOf(MyClass);

// Throwing
(() => throwingFn()).should.throw(ErrorType);
```

---

## Sinon mocking

```ts
import sinon from 'sinon';

// Stub an entire class (all methods become stubs)
const service = sinon.createStubInstance(ConcreteService);

// Stub a global function
const fetchStub = sinon.stub(globalThis, 'fetch');
fetchStub.resolves({ ok: true, json: async () => ({ data: 'value' }) });

// Configure return values
service.getValue.returns('result');
service.getAsync.resolves(42);

// Access call details
service.doSomething.calledOnce.should.be.true;
service.doSomething.calledWith('expected-arg').should.be.true;
service.doSomething.callCount.should.equal(2);

const firstCall = service.doSomething.firstCall;
firstCall.args[0].should.equal('expected');

// Restore stubs after test
afterEach(() => sinon.restore());
```

---

## given() helper — full pattern

The `given()` function instantiates a context class, runs tests with it, and ensures setup is isolated per test.

```ts
import { given } from '../../given';  // import from package root
import { a_my_service } from '../given/a_my_service';

describe('when doing something', given(a_my_service, context => {
    let result: string;

    beforeEach(async () => {
        result = await context.service.doSomething('input');
    });

    it('should return expected result', () => {
        result.should.equal('expected');
    });

    it('should call dependency once', () => {
        context.dependency.process.calledOnce.should.be.true;
    });
}));
```

---

## Reusable context class

```ts
// given/a_my_service.ts
import sinon from 'sinon';
import { MyService } from '../../../MyService';

export class a_my_service {
    dependency: sinon.StubbedInstance<DependencyClass>;
    service: MyService;

    constructor() {
        this.dependency = sinon.createStubInstance(DependencyClass);
        // configure defaults:
        this.dependency.getValue.returns('default');
        this.service = new MyService(this.dependency as unknown as IDependency);
    }
}
```

Properties are **public** — accessed via `context.propertyName` in specs.

---

## Multiple outcomes — folder pattern

```
when_processing/
├── with_valid_input.ts     → happy path
├── with_empty_input.ts     → edge case
└── without_required_field.ts  → failure path
```

Each file has its own `describe()` block, its own `beforeEach`, and its own `it()` assertions.

---

## Async specs

`beforeEach`, `afterEach`, and `it` can all be `async`:

```ts
describe('when loading data', given(a_loader, context => {
    let result: Data[];

    beforeEach(async () => {
        result = await context.loader.load('source');
    });

    it('should return items', () => {
        result.should.have.lengthOf(3);
    });
}));
```

---

## What NOT to spec

Same as C#:
- Simple property getters and setters
- Properties that return constructor parameters directly
- Trivial delegation
- Don't write specs whose `describe` starts with "when getting" or "when returning" — these are almost always testing getters, not behavior
