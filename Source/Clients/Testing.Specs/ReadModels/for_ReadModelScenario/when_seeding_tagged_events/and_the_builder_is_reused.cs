// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events;

public class and_the_builder_is_reused : given.a_tagged_scenario
{
    protected override Type ReducerType => typeof(Combined);

    async Task Because()
    {
        var original = _scenario.Given.ForEventSource(_source).OnStream("Orders", "Payments");
        var tags = new[] { "dynamic" };
        var tagged = original.WithTags(tags);
        tags[0] = "red";
        await original.Events(new UntaggedCredit(100));
        await tagged.Events(new UntaggedCredit(3));
        await tagged.WithTags("dynamic").Events(new UntaggedCredit(4));
    }

    [Fact] void should_leave_the_original_builder_untagged() => _scenario.Instance!.Amount.ShouldEqual(7);
    [Fact] void should_snapshot_and_deduplicate_tags() => _scenario.Instance!.Tags.ShouldContainOnly("dynamic");
    [Fact] void should_preserve_the_route_through_with_tags() => _scenario.Instance!.SourceType.Value.ShouldEqual("Orders");
}
