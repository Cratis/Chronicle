// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events;

public class and_tags_only_label_the_reducer : given.a_tagged_scenario
{
    protected override Type ReducerType => typeof(Unfiltered);

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source).OnStream("Other", "Other").Events(new UntaggedCredit(3));
        await _scenario.Given.ForEventSource(_source).WithTags("red").Events(new UntaggedCredit(4));
    }

    [Fact] void should_keep_no_filter_and_wildcard_behavior() => _scenario.Instance!.Amount.ShouldEqual(7);
    [Fact] void should_not_use_observer_labels_as_filters() => _scenario.Instance!.Tags.ShouldContainOnly("red");
}
