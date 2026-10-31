// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events;

public class and_typed_filters_include_tags : given.a_tagged_scenario
{
    protected override Type ReducerType => typeof(Typed);

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source).OnStream("RoutedOrder", "Payments").Events(new UntaggedCredit(100));
        await _scenario.Given.ForEventSource(_source).WithTags("dynamic").OnStream("Other", "Payments").Events(new UntaggedCredit(100));
        await _scenario.Given.ForEventSource(_source).OnStream("RoutedOrder", "Payments").WithTags("dynamic").Events(new UntaggedCredit(3));
    }

    [Fact] void should_use_registered_routes_and_tag_filters_together() => _scenario.Instance!.Amount.ShouldEqual(3);
}
