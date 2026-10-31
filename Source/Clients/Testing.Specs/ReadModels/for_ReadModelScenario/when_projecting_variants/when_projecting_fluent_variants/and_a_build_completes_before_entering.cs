// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_projecting_fluent_variants;

/// <summary>
/// The fluent <c language="csharp">From</c> on a non-entering event is an update-only join, the same as the model-bound
/// form, so it never creates the variant on its own and is picked up once the entering event arrives.
/// </summary>
public class and_a_build_completes_before_entering : Specification
{
    ReadModelScenario<FluentPullRequestItem> _alone;
    ReadModelScenario<FluentPullRequestItem> _beforeEntering;
    EventSourceId _aloneId;
    EventSourceId _beforeEnteringId;

    void Establish()
    {
        _alone = new ReadModelScenario<FluentPullRequestItem>();
        _beforeEntering = new ReadModelScenario<FluentPullRequestItem>();
        _aloneId = EventSourceId.New();
        _beforeEnteringId = EventSourceId.New();
    }

    async Task Because()
    {
        await _alone.Given.ForEventSource(_aloneId).Events(new BuildCompleted("Passed"));
        await _beforeEntering.Given.ForEventSource(_beforeEnteringId).Events(new BuildCompleted("Passed"), new PullRequestCreated("https://example.com/pr/1"));
    }

    [Fact] void should_not_create_a_pull_request_from_the_build_alone() => _alone.InstanceForEventSourceId(_aloneId).ShouldBeNull();
    [Fact] void should_carry_the_build_status_reported_before_entering() => _beforeEntering.InstanceForEventSourceId(_beforeEnteringId)!.BuildStatus.ShouldEqual("Passed");
}
