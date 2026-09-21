// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_entering_a_variant;

/// <summary>
/// Proves the mutual-exclusion claim end to end, against the real projection engine (not just the built
/// definition shape): entering one variant removes the entity's row from every other variant it was in, and
/// creates a row in the entered variant. This is the single most load-bearing runtime spec for the feature —
/// everything else about the design rests on this actually happening at projection time.
/// </summary>
public class and_the_entity_was_in_another_variant : Specification
{
    ReadModelScenario<BacklogItem> _backlog;
    ReadModelScenario<PullRequestItem> _pullRequest;
    EventSourceId _id;

    void Establish()
    {
        _backlog = new ReadModelScenario<BacklogItem>();
        _pullRequest = new ReadModelScenario<PullRequestItem>();
        _id = EventSourceId.New();
    }

    async Task Because()
    {
        var events = new object[] { new IssueCreated("Add variant support"), new PullRequestCreated("https://example.com/pr/1") };
        await _backlog.Given.ForEventSource(_id).Events(events);
        await _pullRequest.Given.ForEventSource(_id).Events(events);
    }

    [Fact] void should_remove_the_entity_from_the_backlog_variant() => _backlog.InstanceForEventSourceId(_id).ShouldBeNull();
    [Fact] void should_create_the_entity_in_the_pull_request_variant() => _pullRequest.InstanceForEventSourceId(_id).ShouldNotBeNull();
    [Fact] void should_carry_the_entering_events_value() => _pullRequest.InstanceForEventSourceId(_id)!.PullRequestUrl.ShouldEqual("https://example.com/pr/1");
}
