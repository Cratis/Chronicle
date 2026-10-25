// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_a_failing_index : an_event_sequence
{
    protected readonly List<EventSequenceNumber> _attempts = [];
    protected Func<EventSequenceNumber, Task> _update = _ => Task.CompletedTask;
    protected AppendResult _result;

    async Task Establish()
    {
        _currentValidation = new ConstraintValidation([new FailingValidator(new UniqueConstraintDefinition("failing", [new(_eventType.Id, [])]), number =>
        {
            _attempts.Add(number);
            return _update(number);
        })]);
        await _eventSequence.RefreshConstraints();
    }

    protected void UseDeterministicRetryClock()
    {
        var clock = new RepairClock();
        _eventSequence.ConstraintIndexTimeProvider = clock;
        clock.TimerCreated = due =>
        {
            if (due.TotalMilliseconds is 100 or 400) clock.Advance(due);
        };
    }

    protected class FailingValidator(IConstraintDefinition definition, Func<EventSequenceNumber, Task> update) : IConstraintValidator, IHaveUpdateConstraintIndex
    {
        public IConstraintDefinition Definition => definition;
        public bool CanValidate(ConstraintValidationContext context) => true;
        public Task<ConstraintValidationResult> Validate(ConstraintValidationContext context) => Task.FromResult(ConstraintValidationResult.Success);
        public IUpdateConstraintIndex GetUpdateFor(ConstraintValidationContext context) => new FailingUpdater(update);
    }

    protected class FailingUpdater(Func<EventSequenceNumber, Task> update) : IUpdateConstraintIndex
    {
        public Task Update(EventSequenceNumber eventSequenceNumber) => update(eventSequenceNumber);
    }
}
