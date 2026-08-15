// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_a_nested_object;

/// <summary>
/// The one clear that already worked. A scalar clear is a separate mechanism from the nested-object clear, and
/// this pins that the nested one keeps behaving as it did - clearing the whole object rather than a member of it.
/// </summary>
public class and_the_clearing_event_has_occurred : Specification
{
    ReadModelScenario<SecurityPass> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<SecurityPass>();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new SecurityBadgeIssued("A-114", "Reactor"), new SecurityBadgeRevoked());

    [Fact] void should_have_materialized_the_read_model() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_clear_the_whole_badge() => _scenario.Instance!.Badge.ShouldBeNull();
}
