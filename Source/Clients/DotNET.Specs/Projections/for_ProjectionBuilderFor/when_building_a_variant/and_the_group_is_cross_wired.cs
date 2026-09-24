// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building_a_variant;

/// <summary>
/// A variant cannot be told what removes it while it is being built, because its siblings are not known yet.
/// The cross-wiring pass runs once every variant of an identity has been built and gives each one a removal for
/// every sibling's entering event - and none for its own.
/// </summary>
public class and_the_group_is_cross_wired : Specification
{
    static readonly EventType _issueCreated = new() { Id = "issue-created", Generation = 1 };
    static readonly EventType _pullRequestCreated = new() { Id = "pull-request-created", Generation = 1 };

    ProjectionDefinition _backlogItem;
    ProjectionDefinition _pullRequestItem;

    void Establish()
    {
        _backlogItem = new ProjectionDefinition { RemovedWith = new Dictionary<EventType, RemovedWithDefinition>() };
        _pullRequestItem = new ProjectionDefinition { RemovedWith = new Dictionary<EventType, RemovedWithDefinition>() };
    }

    void Because() => VariantReclassifier.CrossWireGroups(
        new Dictionary<Type, ProjectionDefinition>
        {
            [typeof(BacklogItem)] = _backlogItem,
            [typeof(PullRequestItem)] = _pullRequestItem
        },
        new Dictionary<Type, FluentVariantDeclaration>
        {
            [typeof(BacklogItem)] = new(typeof(WorkItem), [_issueCreated]),
            [typeof(PullRequestItem)] = new(typeof(WorkItem), [_pullRequestCreated])
        });

    [Fact] void should_remove_the_backlog_item_when_the_pull_request_item_is_entered() => _backlogItem.RemovedWith.Keys.ShouldContain(et => et.IsEqual(_pullRequestCreated));

    [Fact] void should_not_remove_the_backlog_item_on_its_own_entering_event() => _backlogItem.RemovedWith.Keys.ShouldNotContain(et => et.IsEqual(_issueCreated));

    [Fact] void should_remove_the_pull_request_item_when_the_backlog_item_is_entered() => _pullRequestItem.RemovedWith.Keys.ShouldContain(et => et.IsEqual(_issueCreated));

    [Fact] void should_not_remove_the_pull_request_item_on_its_own_entering_event() => _pullRequestItem.RemovedWith.Keys.ShouldNotContain(et => et.IsEqual(_pullRequestCreated));
}
