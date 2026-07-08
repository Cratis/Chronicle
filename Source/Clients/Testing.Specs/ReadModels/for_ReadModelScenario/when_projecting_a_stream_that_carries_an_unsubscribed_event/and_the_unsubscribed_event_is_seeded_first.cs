// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_a_stream_that_carries_an_unsubscribed_event;

/// <summary>
/// Seeds the unsubscribed audit/marker event before the subscribed event, verifying the skip is order-independent:
/// the projection still resolves its key and materializes the read model from the subscribed event regardless of
/// where the ignored event falls in the stream.
/// </summary>
public class and_the_unsubscribed_event_is_seeded_first : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    EventSourceId _moduleId;

    void Establish()
    {
        _scenario = new ReadModelScenario<SimpleModule>();
        _moduleId = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleAudited(), new ModuleCreated("My Module"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_name_from_the_subscribed_event() => _scenario.Instance!.Name.ShouldEqual("My Module");
    [Fact] void should_materialize_exactly_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
