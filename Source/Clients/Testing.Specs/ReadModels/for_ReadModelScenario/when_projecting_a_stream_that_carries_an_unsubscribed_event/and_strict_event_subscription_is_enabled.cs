// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_a_stream_that_carries_an_unsubscribed_event;

/// <summary>
/// Opts in to strict event subscription and seeds the same subscribed + unsubscribed pair. Where the default
/// lenient mode silently skips the unsubscribed event (mirroring the production engine), strict mode surfaces
/// it as a loud <see cref="UnsubscribedEventSeeded"/> so a spec can catch the genuine mistake of seeding the
/// wrong event type.
/// </summary>
public class and_strict_event_subscription_is_enabled : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    EventSourceId _moduleId;
    Exception _error;

    void Establish()
    {
        _scenario = new ReadModelScenario<SimpleModule>().WithStrictEventSubscription();
        _moduleId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleCreated("My Module"), new ModuleAudited());

        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_reject_the_unsubscribed_event() => _error.ShouldBeOfExactType<UnsubscribedEventSeeded>();
    [Fact] void should_name_the_unsubscribed_event_type() => _error.Message.ShouldContain(nameof(ModuleAudited));
}
