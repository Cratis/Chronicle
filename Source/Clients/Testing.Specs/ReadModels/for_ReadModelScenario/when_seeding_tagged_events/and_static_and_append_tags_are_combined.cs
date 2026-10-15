// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events;

public class and_static_and_append_tags_are_combined : given.a_tagged_scenario
{
    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source).WithTags("red").Events(new UntaggedCredit(100));
        await _scenario.Given.ForEventSource(_source).WithTags("dynamic").Events(new UntaggedCredit(3));
        await _scenario.Given.ForEventSource(_source).WithTags("dynamic", "static", "dynamic").Events(new Credited(4));
    }

    [Fact] void should_accept_either_filter_tag_and_exclude_nonmatching_input() => _scenario.Instance!.Amount.ShouldEqual(7);
    [Fact] void should_include_inherited_static_tags_and_distinct_append_tags() => _scenario.Instance!.Tags.ShouldContainOnly("static", "dynamic");
    [Fact] void should_retain_keyed_instance_access() => _scenario.InstanceForEventSourceId(_source)!.Amount.ShouldEqual(7);
}
