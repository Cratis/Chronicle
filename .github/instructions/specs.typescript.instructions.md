---
applyTo: "**/for_*/**/*.ts, **/when_*/**/*.ts"
---

# 🧪 How to Write TypeScript Specs

Use the base instructions for writing specs can be found in [Specs Instructions](./specs.instructions.md) and
then adapt with the C# specific conventions below.

## Test Frameworks & Conventions

- **Frameworks:**
  - Uses [Mocha](https://mochajs.org) as test framework and execution.
  - Uses [SinonJS](https://sinonjs.org) for mocking.
  - Uses [Chai](https://www.chaijs.com) for assertions.
  - Uses Vitest for running tests. See [Vitest Documentation](https://vitest.dev/).
  - Uses yarn as package manager.
  - Tests can be run using `yarn test` from every package.
  - Tests are found in alongside the code being tested in folders starting with either `for_`, `when_` or `given_` (for reusable contexts).

- **File/Folder Structure:**
  - Organize tests by feature/domain, e.g. `Events/Constraints/for_UniqueConstraintProvider/when_providing.ts`.
  - Use descriptive folder and file names:
    - `for_<TypeUnderTest>/` for the unit under test
    - `when_<behavior>/` for behaviors with multiple outcomes
    - `when_<behavior>.ts` for simple behaviors with single outcomes
    - Example: `for_UnitOfWork/when_committing/and_it_has_events_and_append_returns_constraints_and_errors.ts`

## Test Class Pattern

- Use BDD-style methods:
  - `void Establish()` for setup.
  - `void Because()` for the action under test.
  - `[Fact] void should_<expected_behavior>()` for assertions.
  - Keep them focused on a single behavior or aspect.

**Example:**

```typescript
describe("when_adding", () => {
    let events: EventsToAppend;
    let event: string;

    beforeEach(() => {
        events = [];
        event = "Forty Two";
    
        events.Add(event);
    });

    it("should_hold_the_added_event", () => {
        events[0].should.equal(event);
    });
});
```

**Example with multiple outcomes:**

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
    ├── single_event_collection.ts
    └── multiple_events_collection.ts
```

Each test uses the `given` function that takes the type of the context to use. **IMPORTANT**: Import the `given` function from the root of the package (e.g., `import { given } from '../../../given';` when in a `when_behavior/` folder):

```typescript
import { an_events_command_response_value_handler } from '../given/an_events_command_response_value_handler';
import { given } from '../../../given'; // Import the given function from the package root
import { expect } from 'chai';

describe("with_valid_events_collection", given(an_events_command_response_value_handler, context => {
    let events: object[];
    let result: boolean;

    beforeEach(() => {
        events = [new TestEvent("Test"), new AnotherTestEvent(42)];
    
        result = context.handler.CanHandle(context.commandContext, events);
    });

    it("should_return_true", () => {
        result.should.be.true;
    });
}));
```

The context would be defined as follows:

```typescript
export class an_events_command_response_value_handler {
    handler: EventsCommandResponseValueHandler; // Public for access in tests
    commandContext: CommandContext; // Public for access in tests

    constructor() {
        this.commandContext = /* setup command context */;
        this.handler = new EventsCommandResponseValueHandler(/* dependencies */);
    }
}
```

## Reusable Context

- Context can be encapsulated into reusable contexts that can be leveraged between specs.
- Create a `given` folder within the unit folder (e.g., `for_<Unit>/given/`)
- Add reusable context classes with descriptive names starting with `a_` or `an_` (e.g., `an_events_command_response_value_handler.ts`)
- **IMPORTANT**: Import the `given` function from the package root: `import { given } from '../../given';` (adjust path as needed)
- Make context properties public (not protected) so tests can access them via `context.propertyName`
- Use the `given` function for tests that benefit from shared setup and state
- Simple tests without complex setup don't need to use the reusable context

## Async

- Any of the methods (`beforeEach`, `afterEach`) can be async if needed.

## Substitutes

- Use sinon for creating substitutes/mocks.
- Pass constructor parameters as needed when substituting concrete classes. For example, `sinon.createStubInstance(ConcreteClass, { param1, param2 })`.

## Test Utilities

- Never use the expect() method for assertions. Always use the fluent interface from Chai.
    - `value.should.equal(expected);`
    - `value.should.be.true;`
    - `value.should.be.false;`
    - `value.should.be.null;`
    - `value.should.not.be.null;`
    - `value.should.deep.equal(expected);`
    - `value.should.be.instanceOf(Type);`
    - `array.should.contain(item);`
    - `array.should.have.lengthOf(number);`
    - `function.should.throw(ErrorType);`
