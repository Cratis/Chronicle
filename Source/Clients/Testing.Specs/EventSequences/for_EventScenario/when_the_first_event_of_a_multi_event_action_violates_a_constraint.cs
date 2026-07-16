// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_the_first_event_of_a_multi_event_action_violates_a_constraint : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _result;
    AppendResult _siblingReplayResult;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(EventSourceId.New())
            .Events(new SubscriberRegistered(new("first@cratis.io")));

        // The first event of the action re-registers the already-taken email (violates the unique
        // constraint); the sibling would be valid on its own. A correct multi-event append rejects the
        // whole action on that first violation rather than continuing and masking it with the sibling's success.
        _result = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(
                new SubscriberRegistered(new("first@cratis.io")),
                new SubscriberRegistered(new("sibling@cratis.io")));

        // If the rejected first event had not stopped the action, the sibling would have landed and claimed
        // "sibling@cratis.io". Re-registering it on a fresh source succeeding proves the action was atomic.
        _siblingReplayResult = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new SubscriberRegistered(new("sibling@cratis.io")));
    }

    [Fact] void should_have_failed() => _result.ShouldBeFailed();
    [Fact] void should_have_the_constraint_violation() => _result.ShouldHaveConstraintViolation(SubscriberRegistered.UniqueEmailConstraint);
    [Fact] void should_not_have_landed_the_sibling_event() => _siblingReplayResult.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
