// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// The fluent release path end to end: <c>builder.Unique&lt;T&gt;().RemovedWith&lt;TRemoval&gt;()</c>. The release is
/// per event source, so one employee ending a shift opens their own next cycle and nobody else's.
/// </summary>
public class when_a_fluently_released_event_type_constraint_is_claimed_again : Specification, IDisposable
{
    static readonly EventSourceId _employee = EventSourceId.New();
    static readonly EventSourceId _anotherEmployee = EventSourceId.New();

    EventScenario _scenario;
    AppendResult _firstShift;
    AppendResult _shiftAfterTheEnd;
    AppendResult _secondShiftInTheSameCycle;
    AppendResult _shiftForAnotherEmployee;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _firstShift = await _scenario.When
            .ForEventSource(_employee)
            .Events(new ShiftStarted("Warehouse"));

        await _scenario.Given
            .ForEventSource(_employee)
            .Events(new ShiftEnded());

        _shiftAfterTheEnd = await _scenario.When
            .ForEventSource(_employee)
            .Events(new ShiftStarted("Warehouse"));

        _secondShiftInTheSameCycle = await _scenario.When
            .ForEventSource(_employee)
            .Events(new ShiftStarted("Front desk"));

        _shiftForAnotherEmployee = await _scenario.When
            .ForEventSource(_anotherEmployee)
            .Events(new ShiftStarted("Warehouse"));
    }

    [Fact] void should_accept_the_first_shift() => _firstShift.ShouldBeSuccessful();
    [Fact] void should_accept_a_shift_after_the_previous_one_ended() => _shiftAfterTheEnd.ShouldBeSuccessful();
    [Fact] void should_reject_a_second_shift_within_the_same_cycle() => _secondShiftInTheSameCycle.ShouldHaveConstraintViolation(OneOpenShiftPerEmployee.Name);
    [Fact] void should_accept_a_first_shift_for_another_employee() => _shiftForAnotherEmployee.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
