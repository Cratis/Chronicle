---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
---

# 🧪 How to Write Specs

We call automated tests for specs or specifications based on Specification by Example related to BDD (Behavior Driven Development).
Keep tests focused, isolated, and descriptive!

## Test Frameworks & Conventions

- **Frameworks:**
  - Uses [Xunit](https://xunit.net/) as test framework and execution.
  - Uses [NSubstitute](https://nsubstitute.github.io/) for mocking.
  - Uses [Cratis Specifications](https://github.com/Cratis/Specifications/blob/main/README.md) for BDD style specification by example testing.

- **File/Folder Structure:**
  - Organize tests by feature/domain, e.g. `Events/Constraints/for_UniqueConstraintProvider/when_providing.cs`.
  - Use separate projects for specs, e.g. `Applications.Specs`.
  - Use descriptive folder and file names:
    - `for_<TypeUnderTest>/` for the unit under test
    - `when_<behavior>/` for behaviors with multiple outcomes
    - `when_<behavior>.cs` for simple behaviors with single outcomes
    - Example: `for_UnitOfWork/when_committing/and_it_has_events_and_append_returns_constraints_and_errors.cs`
  - Tests are found in alongside the code being tested in folders starting with either `for_`, `when_` or `given_` (for reusable contexts).

## Base class

- At the root of inheritance, `Specification` must be the base class.

## Test Class Pattern

- Use BDD-style methods:
  - `void Establish()` for setup.
  - `void Because()` for the action under test.
  - `[Fact] void should_<expected_behavior>()` for assertions.
  - Keep them focused on a single behavior or aspect.

**Example:**

```csharp
public class when_adding : Specification
{
    EventsToAppend events;
    string @event;

    void Establish()
    {
        events = [];
        @event = "Forty Two";
    }

    void Because() => events.Add(@event);

    [Fact] void should_hold_the_added_event() => events.First().ShouldEqual(@event);
}
```

**Example with multiple outcomes:**

```
for_EventsCommandResponseValueHandler/
├── given/
│   └── an_events_command_response_value_handler.cs
├── when_checking_can_handle/
│   ├── with_valid_events_collection.cs
│   ├── with_null_value.cs
│   └── without_event_source_id.cs
└── when_handling/
    ├── empty_events_collection.cs
    ├── single_event_collection.cs
    └── multiple_events_collection.cs
```

Each test class inherits from `given.an_events_command_response_value_handler`:

```csharp
public class with_valid_events_collection : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    bool _result;

    void Establish()
    {
        _events = [new TestEvent("Test"), new AnotherTestEvent(42)];
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _events);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
```

- Establish method can also be async if it needs to perform tasks that are async.

## What to specify

- Write specs that verify what is promised from the signature of methods, not based on its implementation - we want to catch problems with the implementation.
- **Focus on behaviors (methods that perform actions)**, not simple state (properties that hold or return values).
- **Ignore simple properties** - properties that just return constructor parameters, field values, or delegate to other objects without transformation should not have specs.
- Do not test logging - it's too fragile and has no value.

## Naming

- Use clear, descriptive names for test classes and methods.
- Folder: `for_<Unit>`.
- Class: Single condition - `when_<action>[_and_<aspect>]`. Use descriptive prepositions that make the condition clear:
  - `and_*` for additional conditions or compound scenarios
  - `or_*` for alternative outcomes or error paths
  - `with_*`, `without_*`, `when_*`, `having_*`, `given_*` for specific contexts
  - Example: `when_committing_and_validation_fails`, `when_processing_with_invalid_data`
- Method: `should_<expected_result>`

## Behaviors

- Capture the behavior in naming, not just necessarily use names that reflect the code or method name, it should be meaningful as to what the code is doing and trying to accomplish
- **A behavior corresponds to a public method** of the unit under test
- Every public method should be tested separately, never write a single file for an entire unit under test
- Each behavior (public method) should have its own dedicated folder structure when it has multiple outcomes
- For simple behaviors with single outcomes, use a single file: `when_<behavior>.cs`
- For complex behaviors with multiple aspects/outcomes, use folder structure: `when_<behavior>/` with multiple test files inside

## Multiple outcomes of a behavior

- When a behavior (public method) has multiple outcomes, aspects, or edge cases to test, organize them using folder structure:
  - Create a folder named `when_<behavior>` (e.g., `when_checking_can_handle`, `when_handling`)
  - Within this folder, create separate test files for each specific outcome or aspect
  - Use descriptive names that clearly indicate the specific condition being tested:
    - `with_valid_events_collection.cs`
    - `with_null_value.cs`
    - `without_event_source_id.cs`
    - `empty_events_collection.cs`
    - `multiple_events_collection.cs`
- Do not limit naming to `and_*` - use any preposition or descriptor that makes the condition clear:
  - `and_*` for additional conditions (e.g., `and_it_has_events`, `and_validation_fails`)
  - `or_*` for alternative scenarios (e.g., `or_timeout_occurs`, `or_connection_fails`)
  - `with_*` for scenarios with specific data/state (e.g., `with_valid_events_collection`, `with_null_value`)
  - `without_*` for scenarios missing data/state (e.g., `without_event_source_id`, `without_permissions`)
  - `when_*` for temporal or conditional aspects (e.g., `when_timeout_occurs`, `when_validation_passes`)
  - `having_*` for possession/state-based conditions (e.g., `having_multiple_items`, `having_no_data`)
  - `given_*` for precondition scenarios (e.g., `given_empty_collection`, `given_invalid_state`)
- Keep a single specific outcome or aspect in each file - don't combine multiple scenarios
- Each test file should inherit from the appropriate reusable context (usually `given.a_<unit_name>`)

## Namespaces

- The namespace should start with the same as the unit having specs for it.
- Do not include `.Specs` in the namespace.

## Reusable Context

- Context can be encapsulated into reusable contexts that can be leveraged between specs.
- Create a `given` folder within the unit folder (e.g., `for_<Unit>/given/`)
- Add reusable context classes with descriptive names starting with `a_` or `an_` (e.g., `a_events_command_response_value_handler.cs`)
- Namespace should include `given` in the name (e.g., `Infrastructure.ReadModels.for_EventsCommandResponseValueHandler.given`)
- Members that are to be reused should be `protected`
- Members are initialized using the `Establish` method, same as regular specs
- Specs inherit from the context by doing `given.a_specific_context`, example: `public class when_performing_a_behavior : given.a_specific_context`
- Remember the inheritance rule: `Specification` must be in the inheritance chain at the root
- Shared variables should be `protected` fields, not properties and they should follow the `_camelCase` naming convention
- **For behaviors with multiple outcomes:**
  - The reusable context should be in the unit's `given/` folder (e.g., `for_<Unit>/given/`)
  - All test files within behavior folders (e.g., `when_<behavior>/`) inherit from this shared context
  - This allows consistent setup across all variations of the behavior
- If a specific behavior needs additional setup, create behavior-specific contexts in `when_<behavior>/given/` folder

## Async

- Any of the methods (`Establish`, `Because`, `Cleanup`) can be async if needed.

## Substitutes

- Concrete classes can be substituted/mocked, not just interfaces or abstract classes.
- When substituting concrete classes, ensure they have virtual methods or properties to allow mocking behavior.
- Pass constructor parameters as needed when substituting concrete classes. For example, `Substitute.For<ConcreteClass>(param1, param2)`.

## Test Utilities

- Use `Substitute.For<T>()` for mocks.
- Cratis Specifications has a set of assertion extension methods, use these
  - `.ShouldEqual()`
  - `.ShouldBeTrue()`
  - `.ShouldBeFalse()`
  - `.ShouldBeNull()`
  - `.ShouldNotBeNull()`
  - `.ShouldBeOfExactType<T>()`
  - `.ShouldContain()`
  - `.ShouldContainOnly()`
  - `.ShouldNotContain()`
  - `.ShouldBeEmpty()`
  - `.ShouldNotBeEmpty()`
  - `.ShouldBeInRange()`
  - `.ShouldNotBeInRange()`
  - `.ShouldBeGreaterThan()`
  - `.ShouldBeGreaterThanOrEqual()`
  - `.ShouldBeLessThan()`
  - `.ShouldBeLessThanOrEqual()`
- Use custom helpers from `XUnit.Integration` for integration/event-based assertions.

## Using statements

- Common usings are provided in `GlobalUsings.Specs.cs` (e.g., `using Xunit;`, `using NSubstitute;`), don't add any using statements already in this file.
- Don't add using statement for namespace of the system under test.

## Formatting

- Don't worry about lines being too long for the `should_` methods - prefer one-line lambda methods for readability of the should statements rather than breaking them into multiple lines.
- With multiple `should_` methods, do not add unnecessary whitespace between them.

## Properties and Simple Members

**NEVER write specs for simple properties or getters** - Properties are not behaviors and should not be tested unless they contain complex business logic.

**Do NOT create specs for:**
- Simple auto-properties (e.g., `public string Name { get; set; }`)
- Properties that return constructor parameters (e.g., `public string TableName => tableName;`)
- Properties that return field values (e.g., `public Key Key => key;`)
- Properties that delegate to other objects without transformation (e.g., `public IEnumerable<Property> Properties => mapper.Properties;`)
- Expression-bodied properties that return simple values (e.g., `public Type PropertyName => someValue;`)
- Properties that perform simple null checks or basic validation without complex logic
- Getters that return injected dependencies or configuration values

**Examples of properties to IGNORE:**
```csharp
// ❌ Do NOT write specs for these
public string TableName => tableName;                    // Returns constructor parameter
public Key Key => key;                                   // Returns constructor parameter
public IEnumerable<Property> Properties => mapper.Properties; // Simple delegation
public bool IsValid => _value is not null;              // Simple validation
public ILogger Logger { get; }                          // Injected dependency
```

**Only write specs for properties with complex business logic:**
```csharp
// ✅ These might need specs if they contain complex logic
public decimal TotalCost => Items.Sum(i => i.Cost * i.Quantity * (1 + i.TaxRate));
public bool CanProcess => ValidateComplexBusinessRules() && CheckExternalConditions();
```

**Avoid file names starting with:**
- `when_getting_*` (e.g., `when_getting_key.cs`, `when_getting_properties.cs`)
- `when_returning_*` for simple return values
- Any spec that just verifies a property returns what was passed in the constructor

Focus specs on **behaviors** (methods that perform actions) rather than **state** (properties that hold or return values).
