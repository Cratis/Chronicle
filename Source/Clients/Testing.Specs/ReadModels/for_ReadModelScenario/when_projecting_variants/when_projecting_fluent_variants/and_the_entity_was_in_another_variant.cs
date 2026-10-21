// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_projecting_fluent_variants;

/// <summary>
/// A fluent variant group is cross-wired when every variant has been discovered, so the scenario has to discover
/// the siblings too. Otherwise entering one variant would leave the entity behind in the others, which is not what
/// a running kernel does.
/// </summary>
public class and_the_entity_was_in_another_variant : Specification
{
    ReadModelScenario<FluentBacklogItem> _backlog;
    ReadModelScenario<FluentPullRequestItem> _pullRequest;
    EventSourceId _id;

    void Establish()
    {
        _backlog = new ReadModelScenario<FluentBacklogItem>();
        _pullRequest = new ReadModelScenario<FluentPullRequestItem>();
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
}
