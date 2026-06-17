# Spec Examples

The default is the **in-process scenario family** — no HTTP, no fixture. Each file is wrapped in `#if DEBUG … #endif`. The out-of-process host pattern is in the **Advanced** section at the end.

## Happy path spec (`CommandScenario`)

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace MyApp.Projects.Registration.when_registering;

public class and_name_is_unique : Specification
{
    readonly CommandScenario<RegisterProject> _scenario = new();
    readonly ProjectId _id = ProjectId.New();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new RegisterProject(_id, "My Project"));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_appended_the_registered_event() =>
        await _scenario.ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>(_id, e => e.Name == "My Project");
}
#endif
```

---

## Validation failure spec (`CommandScenario`)

Assert **both** that the command did not succeed and that it failed *for validation* — never on message strings.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace MyApp.Projects.Registration.when_registering;

public class and_name_is_empty : Specification
{
    readonly CommandScenario<RegisterProject> _scenario = new();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new RegisterProject(ProjectId.New(), string.Empty));

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
#endif
```

---

## Constraint violation spec (`EventScenario`, pre-existing state)

Constraints are append-level — exercise them with `EventScenario` and assert the constraint **name**. (See the **write-specs-events** skill for the full pattern.)

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace MyApp.Projects.Registration.when_registering;

public class and_name_already_exists : Specification
{
    readonly EventScenario _scenario = new();
    const string ProjectName = "My Project";
    IAppendResult _result;

    async Task Establish() =>
        await _scenario.Given.ForEventSource(ProjectId.New()).Events(new ProjectRegistered(ProjectName));

    async Task Because() =>
        _result = await _scenario.EventLog.Append(ProjectId.New(), new ProjectRegistered(ProjectName));

    [Fact] void should_be_failed() => _result.ShouldBeFailed();
    [Fact] void should_violate_the_unique_name_constraint() =>
        _result.ShouldHaveConstraintViolationFor(ProjectConstraintNames.UniqueName);
}
#endif
```

---

## Assertion helpers reference

| Helper | When to use |
|--------|-------------|
| `_result.ShouldBeSuccessful()` / `ShouldNotBeSuccessful()` | `CommandResult` success / failure |
| `_result.ShouldHaveValidationErrors()` | Confirm a validation rejection (pair with `ShouldNotBeSuccessful()`) |
| `_result.ShouldNotBeAuthorized()` | Authorization rejection (no validation errors present) |
| `_scenario.ShouldHaveAppendedEvent<TCommand, TEvent>(id, predicate)` | Confirm a command appended the expected event (`async Task`) |
| `_result.ShouldBeFailed()` (`IAppendResult`) | Confirm an `EventScenario` append failed |
| `_result.ShouldHaveConstraintViolationFor(name)` | Confirm the named constraint was violated (assert the name, never the message) |

---

## Advanced — out-of-process Chronicle host spec

Reserve for the host/transport boundary the scenario helpers can't reach (HTTP → command → append → projection against a real Chronicle store). The "unit" is the whole slice, so there is no `for_` folder — files live under `when_<behavior>/`.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = MyApp.Projects.Registration.when_registering.and_name_already_exists.context;

namespace MyApp.Projects.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_name_already_exists(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public const string ProjectName = "My Project";
        public CommandResult<object>? Result;

        async Task Establish() =>
            await EventStore.EventLog.Append(ProjectId.New(), new ProjectRegistered(ProjectName));

        async Task Because() =>
            Result = await Client.ExecuteCommand<RegisterProject>(
                "/api/projects/registration",
                new RegisterProject(ProjectId.New(), ProjectName));
    }

    [Fact] void should_not_succeed() => Context.Result!.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_appended_any_additional_events() =>
        Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

`ExecuteCommand<TCommand>(url, cmd)` returns `CommandResult<object>?`; `ExecuteCommand<TCommand, TResult>(url, cmd)` returns `CommandResult<TResult>?`. Sequence numbers are zero-based (`EventSequenceNumber.First` = 0).
