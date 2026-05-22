# Spec Examples

## Happy path spec

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = MyApp.Projects.Registration.when_registering.and_name_is_unique.context;

namespace MyApp.Projects.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_name_is_unique(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<object>? Result;

        async Task Because()
        {
            Result = await Client.ExecuteCommand<RegisterProject>(
                "/api/projects/registration",
                new RegisterProject(ProjectId.New(), "My Project"));
        }
    }

    [Fact] void should_succeed() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_appended_the_registered_event() =>
        Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

---

## Constraint violation spec (pre-existing state)

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

        async Task Because()
        {
            Result = await Client.ExecuteCommand<RegisterProject>(
                "/api/projects/registration",
                new RegisterProject(ProjectId.New(), ProjectName));
        }
    }

    [Fact] void should_not_succeed() => Context.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_appended_any_additional_events() =>
        Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

---

## Validation failure spec (model validation)

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = MyApp.Projects.Registration.when_registering.and_name_is_empty.context;

namespace MyApp.Projects.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_name_is_empty(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<object>? Result;

        async Task Because()
        {
            Result = await Client.ExecuteCommand<RegisterProject>(
                "/api/projects/registration",
                new RegisterProject(ProjectId.New(), string.Empty));
        }
    }

    [Fact] void should_not_succeed() => Context.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_validation_error_on_name() =>
        Context.Result.ValidationResults.ShouldNotBeEmpty();
}
```

---

## Assertion helpers reference

| Helper | When to use |
|--------|-------------|
| `.ShouldBeTrue()` / `.ShouldBeFalse()` | Boolean assertions |
| `.ShouldEqual(x)` | Value equality |
| `Context.ShouldHaveTailSequenceNumber(n)` | Confirm the tail event sequence number (use `EventSequenceNumber.First` for exactly one event in the log) |
| `Context.Result.ValidationResults.ShouldNotBeEmpty()` | Confirm model validation rejected the command |
| `Context.Result.IsSuccess.ShouldBeFalse()` | Confirm the command did not succeed (constraint or validation) |
| `EventSequenceNumber.First` | Sequence number 0 — the very first event |
