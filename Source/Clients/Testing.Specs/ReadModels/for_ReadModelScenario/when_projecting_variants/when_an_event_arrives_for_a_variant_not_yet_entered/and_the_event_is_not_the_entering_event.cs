// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_an_event_arrives_for_a_variant_not_yet_entered;

/// <summary>
/// The resurrection guard: <see cref="PullRequestItem"/> declares a handler for <see cref="BuildCompleted"/>
/// that is not its entering event. If that handler stayed an ordinary create-or-update <c language="csharp">From</c>
/// (the bug the whole design exists to prevent), a stray <see cref="BuildCompleted"/> for an entity that never
/// had a pull request created would still create a <see cref="PullRequestItem"/> row. Reclassifying it into an
/// update-only join means a join never creates a document, so this must not happen.
/// </summary>
public class and_the_event_is_not_the_entering_event : Specification
{
    ReadModelScenario<PullRequestItem> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<PullRequestItem>();
        _id = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(_id).Events(new BuildCompleted("Passed"));

    [Fact] void should_not_create_a_pull_request_item() => _scenario.InstanceForEventSourceId(_id).ShouldBeNull();
}
