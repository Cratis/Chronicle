// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// The model-bound release path end to end: <c>[Unique]</c> on the event type and <c>[RemoveConstraint]</c> on the
/// event that ends the cycle. The attribute was already resolved into a removal event on the client definition and
/// then discarded on the way to the contract, so the documented behavior compiled, registered, and did nothing -
/// the second cycle was refused forever with no indication of why.
/// </summary>
public class when_a_released_event_type_constraint_is_claimed_again : Specification, IDisposable
{
    static readonly EventSourceId _borrower = EventSourceId.New();

    EventScenario _scenario;
    AppendResult _firstCheckout;
    AppendResult _return;
    AppendResult _checkoutAfterTheReturn;
    AppendResult _secondCheckoutInTheSameCycle;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _firstCheckout = await _scenario.When
            .ForEventSource(_borrower)
            .Events(new LoanCheckedOut("Dune"));

        _return = await _scenario.When
            .ForEventSource(_borrower)
            .Events(new LoanReturned());

        _checkoutAfterTheReturn = await _scenario.When
            .ForEventSource(_borrower)
            .Events(new LoanCheckedOut("Neuromancer"));

        _secondCheckoutInTheSameCycle = await _scenario.When
            .ForEventSource(_borrower)
            .Events(new LoanCheckedOut("Snow Crash"));
    }

    [Fact] void should_accept_the_first_checkout() => _firstCheckout.ShouldBeSuccessful();
    [Fact] void should_accept_the_return() => _return.ShouldBeSuccessful();
    [Fact] void should_accept_a_checkout_after_the_return() => _checkoutAfterTheReturn.ShouldBeSuccessful();
    [Fact] void should_reject_a_second_checkout_within_the_same_cycle() => _secondCheckoutInTheSameCycle.ShouldHaveConstraintViolation(LoanCheckedOut.ConstraintName);

    public void Dispose() => _scenario.Dispose();
}
