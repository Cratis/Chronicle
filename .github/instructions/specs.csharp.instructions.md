---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
---

# 🧪 How to Write C# Specs

Use the base instructions for writing specs can be found in [Specs Instructions](./specs.instructions.md) and
then adapt with the C# specific conventions below.

## Test Frameworks & Conventions

- **Frameworks:**
  - Uses [Xunit](https://xunit.net/) as test framework and execution.
  - Uses [NSubstitute](https://nsubstitute.github.io/) for mocking.
  - Uses [Cratis Specifications](https://github.com/Cratis/Specifications/blob/main/README.md) for BDD style specification by example testing.
  - Use separate projects for specs, e.g. `Applications.Specs`.

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

## Reusable Context

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
- If the system being specified is not constructed in the reusable context, don't keep the variable for it in the context, instead create it in the specific behavior spec
- Reusable contexts can inherit from other reusable contexts to build upon setups
- Use `Establish` method for setup logic in reusable contexts
- When it makes sense, create a root reusable context called `all_dependencies` that sets up all common dependencies for the unit under test that more specific contexts can inherit from
- `Because`method should not be used in reusable contexts, it should be in the specific behavior spec files

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

## Properties and Simple Members

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
