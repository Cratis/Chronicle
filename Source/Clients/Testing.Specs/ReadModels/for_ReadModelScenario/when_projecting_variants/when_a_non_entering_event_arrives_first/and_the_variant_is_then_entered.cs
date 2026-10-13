// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_a_non_entering_event_arrives_first;

/// <summary>
/// A non-entering handler is an update-only join, and the engine re-resolves a join against the last stored event
/// of the joined stream whenever the read model handles its own event. So a <see cref="BuildCompleted"/> that was
/// appended before the entering <see cref="PullRequestCreated"/> must be on the <see cref="PullRequestItem"/> the
/// moment it is created, exactly as it is under a running kernel.
/// </summary>
public class and_the_variant_is_then_entered : Specification
{
    ReadModelScenario<PullRequestItem> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<PullRequestItem>();
        _id = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(_id).Events(new BuildCompleted("Passed"), new PullRequestCreated("https://example.com/pr/1"));

    [Fact] void should_create_the_pull_request_item() => _scenario.InstanceForEventSourceId(_id).ShouldNotBeNull();
    [Fact] void should_carry_the_entering_events_value() => _scenario.InstanceForEventSourceId(_id)!.PullRequestUrl.ShouldEqual("https://example.com/pr/1");
    [Fact] void should_carry_the_build_status_reported_before_entering() => _scenario.InstanceForEventSourceId(_id)!.BuildStatus.ShouldEqual("Passed");
}
