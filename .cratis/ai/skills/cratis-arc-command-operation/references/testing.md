<!-- cratis-ai-managed: skills/cratis-arc-command-operation/references/testing.md -->
# Specify decisions, adapters, and pipeline composition

Verified at **Arc v22.48.1**. These class excerpts follow the product's
[standalone lesson](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/testing/command-operations.md)
and [Chronicle rejection lesson](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/testing/command-operations-with-chronicle.md).
Use `Cratis.Specifications.XUnit`, NSubstitute, and matching Arc packages in a
separate spec project. Keep standalone and Chronicle scenarios in separate
projects: Chronicle's discovered scenario extender changes the test environment.

The standalone examples reuse `ReservationId`, `SeatId`, `ISeatReservations`,
`ReserveSeat`, `BookSeat`, `SeatReservation`, and `BookSeats` from the
[application declarations](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/commands/operations/implementing.md).
Add these imports to each standalone spec file (and its own namespace):

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SeatBooking;
using Xunit;
```

## Decision: direct Handle

No provider or container is needed. Fixed inputs and record equality prove the
proposed work, not its execution or authorization.

```csharp
public class when_deciding_to_book : Specification
{
    readonly ReservationId _id = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    readonly SeatId _seat = new("A-12");
    (ReservationId Response, ReserveSeat Operation) _decision;

    void Because() => _decision = new BookSeat(_id, _seat).Handle();

    [Fact] void should_return_the_request_identifier() => _decision.Response.ShouldEqual(_id);
    [Fact] void should_declare_the_reservation() =>
        _decision.Operation.ShouldEqual(new ReserveSeat(_id, _seat));
}
```

For batches assert membership and order, including conditional/empty decisions.
Do not infer that calling `Handle()` performed any external work.

## Adapter: direct Execute and Compensate

This tests the provider mapping, not Arc's recovery selection:

```csharp
public class when_reserving_directly : Specification
{
    readonly ReservationId _id = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    readonly SeatId _seat = new("A-12");
    ISeatReservations _reservations = null!;

    void Establish()
    {
        _reservations = Substitute.For<ISeatReservations>();
        _reservations.Reserve(_id, _seat, CancellationToken.None).Returns(Task.CompletedTask);
    }

    Task Because() => new ReserveSeat(_id, _seat).Execute(_reservations, CancellationToken.None);

    [Fact] Task should_reserve_the_requested_seat() =>
        _reservations.Received(1).Reserve(_id, _seat, CancellationToken.None);
    [Fact] Task should_not_cancel() =>
        _reservations.DidNotReceive().Cancel(Arg.Any<ReservationId>(), Arg.Any<CancellationToken>());
}
```

For the reversal's own spec, arrange `Cancel` to return `Task.CompletedTask`,
call `new ReserveSeat(_id, _seat).Compensate(_reservations, CancellationToken.None)`
in `Because`, and assert `Received(1).Cancel(_id, CancellationToken.None)` plus no
`Reserve` call. A direct throwing `Execute` does not automatically compensate.

## Composition: forward failure and reverse compensation

Reference `Cratis.Arc.Testing`. `CommandScenario<TCommand>` runs the real pipeline
and invokers, not a recording stub. Register substitutes through `Services`
before first execution; do not register a production external client.

This case fails the second reservation. Both entered invocations must be
compensated; the third must never start.

```csharp
public class ReservationProviderUnavailable() : Exception("Reservation provider unavailable.");

public class when_the_second_reservation_fails : Specification
{
    readonly CommandScenario<BookSeats> _scenario = new();
    readonly ReservationId _first = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    readonly ReservationId _second = new(Guid.Parse("b52a8554-fab3-4050-af81-554d105d9b9d"));
    readonly ReservationId _third = new(Guid.Parse("0aeb4b2f-0515-44fe-8e79-7c8f304d2c65"));
    ISeatReservations _reservations = null!;
    CommandResult _result = null!;

    void Establish()
    {
        _reservations = Substitute.For<ISeatReservations>();
        _reservations.Reserve(Arg.Any<ReservationId>(), Arg.Any<SeatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _reservations.Reserve(_second, Arg.Any<SeatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ReservationProviderUnavailable()));
        _reservations.Cancel(Arg.Any<ReservationId>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_reservations);
    }

    async Task Because() => _result = await _scenario.Execute(new BookSeats(
    [
        new SeatReservation(_first, new SeatId("A-12")),
        new SeatReservation(_second, new SeatId("A-13")),
        new SeatReservation(_third, new SeatId("A-14"))
    ]));

    [Fact] void should_preserve_failure() => _result.ShouldNotBeSuccessful();
    [Fact] void should_preserve_the_original_error() =>
        _result.ExceptionMessages.ShouldContain("Reservation provider unavailable.");
    [Fact] void should_record_only_entered_invocations() => _scenario.Operations.Count.ShouldEqual(2);
    [Fact] void should_observe_the_first_execution_completing() =>
        _scenario.Operations[0].ExecutionCompleted.ShouldBeTrue();
    [Fact] void should_observe_the_throwing_execution() =>
        _scenario.Operations[1].ExecutionCompleted.ShouldBeFalse();
    [Fact] Task should_not_start_the_third() =>
        _reservations.DidNotReceive().Reserve(_third, Arg.Any<SeatId>(), Arg.Any<CancellationToken>());
    [Fact] void should_compensate_in_reverse_order() => Received.InOrder(() =>
    {
        _ = _reservations.Cancel(_second, Arg.Any<CancellationToken>());
        _ = _reservations.Cancel(_first, Arg.Any<CancellationToken>());
    });
    [Fact] void should_observe_both_compensations() => _result.Recovery!.CompensatedCount.ShouldEqual(2);
    [Fact] void should_report_callback_completion() =>
        _result.Recovery!.Status.ShouldEqual(CommandRecoveryStatus.Completed);

    void Destroy() => _scenario.Dispose();
}
```

`Operations` snapshots the latest `Execute`'s entered invocations, including the
throwing one. `ShouldHaveExecutedOperation<T>()` requires at least one matching
execution to have **completed**; it does not mean every operation of that type
succeeded. Pair it with exact counts, indices, and provider arguments as needed.
`ShouldHaveCompensatedOperation<T>()` likewise proves a callback returned, not
provider durability. A lone unsuccessful-result assertion could pass because
DI failed before any work began.

## Chronicle: a real commit rejection

Reference `Cratis.Arc.Chronicle.Testing` and the `Cratis.Arc` test-host dependency.
Reuse the `Onboarding` declarations from the linked Chronicle lesson:
`StartOnboarding(EventSourceId, OrganizationNumber, ReservationKey)` returns
`OnboardingStarted` plus `ReserveOnboardingCapacity`. The event declares
`UniqueOrganizationNumber` uniqueness. Import `Onboarding` instead of
`SeatBooking`, plus `Cratis.Arc.Chronicle.Testing.Commands` and
`Cratis.Chronicle.Events`; retain the other imports above.

```csharp
public class when_the_organization_is_already_onboarding : Specification, IAsyncDisposable
{
    CommandScenario<StartOnboarding> _scenario = null!;
    readonly OrganizationNumber _organization = new("ORG-OPERATIONS");
    readonly ReservationKey _key = new(Guid.Parse("96ce8c18-1e67-4e44-b5e7-4fb2b2cb3b97"));
    EventSourceId _newPartner = null!;
    IOnboardingReservations _reservations = null!;
    CommandResult _result = null!;

    async Task Establish()
    {
        _scenario = new();
        _newPartner = EventSourceId.New();
        _reservations = Substitute.For<IOnboardingReservations>();
        _reservations.Reserve(_key, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _reservations.Cancel(_key, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_reservations);
        await _scenario.EventScenario.Given.ForEventSource(EventSourceId.New())
            .Events(new OnboardingStarted(_organization));
    }

    async Task Because() =>
        _result = await _scenario.Execute(new StartOnboarding(_newPartner, _organization, _key));

    [Fact] void should_report_the_constraint() =>
        _result.ShouldHaveConstraintViolationFor("UniqueOrganizationNumber");
    [Fact] void should_observe_forward_execution() =>
        _scenario.ShouldHaveExecutedOperation<ReserveOnboardingCapacity>();
    [Fact] void should_observe_compensation() =>
        _scenario.ShouldHaveCompensatedOperation<ReserveOnboardingCapacity>();
    [Fact] void should_call_the_provider_in_order() => Received.InOrder(() =>
    {
        _ = _reservations.Reserve(_key, Arg.Any<CancellationToken>());
        _ = _reservations.Cancel(_key, Arg.Any<CancellationToken>());
    });
    [Fact] void should_report_noncommit() =>
        _result.Recovery!.CommitDisposition.ShouldEqual(CommandCommitDisposition.NotCommitted);
    [Fact] void should_report_callback_completion() =>
        _result.Recovery!.Status.ShouldEqual(CommandRecoveryStatus.Completed);
    [Fact] async Task should_not_persist_the_rejected_event() =>
        (await _scenario.EventScenario.EventLog.HasEventsFor(_newPartner)).ShouldBeFalse();

    async Task Destroy() => await ((IAsyncDisposable)this).DisposeAsync();
    ValueTask IAsyncDisposable.DisposeAsync() => _scenario.DisposeAsync();
}
```

Seed the constraint in the **event log** through `EventScenario.Given`, not only
in a read-model dictionary. This follows the maintained
[real-rejection spec](https://github.com/Cratis/Arc/blob/v22.48.1/Source/DotNET/Chronicle.Specs/Commands/for_CommandScenario/operations/when_a_real_constraint_rejects_the_commit.cs).
It proves known non-commit and eligible compensation with the in-process store,
not uncertain remote acknowledgments or the external provider's durability.

## Extend the failure coverage

- Happy path: assert provider arguments, `ShouldHaveExecutedOperation<ReserveSeat>()`,
  `Recovery.Status == CommandRecoveryStatus.NotNeeded`, and the actual response.
- `Validate(command)`: no `Provide`, `Handle`, scope lifecycle, or operations;
  assert the specific validation rule and `DidNotReceive` provider calls.
- Execution-path rejection: `Execute` invalid input, assert the specific verdict
  and `ShouldHaveNoOperationInvocations()`.
- Cancellation: `Execute(command, cancellationToken)`; cancel deterministically
  inside a substitute and prove compensation got a separate uncanceled token.
- Missing/throwing compensation: `Incomplete`, `NotAvailable`/`Failed`, exact
  `UncompensatedCount`/`FailedCompensationCount`, and remaining callbacks attempted.

Arc's [failure recipes](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/testing/command-operation-failures.md)
contain the full cases. Run `dotnet test <Your.Specs.csproj>`; assert behavior,
not just command success. These corpus excerpts are source-checked, not compiled
by the corpus text-verification gate. Test provider ownership, repeated finalized
requests, conflicting keys, and delayed creates at the provider integration boundary.

## Migrate an existing service-backed Handle

Follow the [migration recipe](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/commands/operations/migrating.md):

1. Separate acquired inputs, decision, and service writes. Preserve the input,
   endpoint, and response; move the write into an operation and return it with
   the existing response. Remove the old call, including any manual `Execute`.
2. Keep a direct service interaction if its receipt is needed to construct the
   decision/response. Do not hide the write in `Provide` or redesign persistence
   solely for syntactic purity.
3. Keep provider DI; remove an obsolete custom response handler only if its sole
   purpose was this side effect. Keep specialized framework handlers and events.
4. Audit custom scopes, early commits, and ownership-safe reversal before enabling
   the new path. Do not add rollback stacks or relabel a committing scope.
5. Prove decision data, provider effects, response, validation/authorization,
   failure/compensation, and (with Chronicle) actual commit rejection. Rebuild
   the proxy: it must still expose the original response, not the operation.
