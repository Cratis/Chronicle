// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_a_shared_handler_event_arrives;

/// <summary>
/// A <c language="csharp">[GlobalFor&lt;T&gt;]</c>-mapped event updates whichever variant is currently active,
/// and — because the merged mapping is reclassified exactly like the variant's own handlers — creates none.
/// </summary>
public class and_the_variant_is_currently_active : Specification
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
        var events = new object[] { new IssueCreated("Original title"), new TitleChanged("Updated title") };
        await _backlog.Given.ForEventSource(_id).Events(events);
        await _pullRequest.Given.ForEventSource(_id).Events(events);
    }

    [Fact] void should_update_the_title_on_the_active_variant() => _backlog.InstanceForEventSourceId(_id)!.Title.ShouldEqual("Updated title");
    [Fact] void should_not_create_a_row_in_a_variant_the_entity_never_entered() => _pullRequest.InstanceForEventSourceId(_id).ShouldBeNull();
}
