// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events;

public class and_tags_and_routes_must_match : given.a_tagged_scenario
{
    protected override Type ReducerType => typeof(Combined);

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source).WithTags("dynamic").OnStream("Other", "Payments").Events(new UntaggedCredit(100));
        await _scenario.Given.ForEventSource(_source).OnStream("Orders", "Other").WithTags("dynamic").Events(new UntaggedCredit(100));
        await _scenario.Given.ForEventSource(_source).OnStream("Orders", "Payments").WithTags("red").Events(new UntaggedCredit(100));
        await _scenario.Given.ForEventSource(_source).WithTags("dynamic").OnStream("Orders", "Payments").Events(new UntaggedCredit(3));
    }

    [Fact] void should_require_every_filter_category() => _scenario.Instance!.Amount.ShouldEqual(3);
    [Fact] void should_keep_tags_through_on_stream() => _scenario.Instance!.Tags.ShouldContainOnly("dynamic");
}
