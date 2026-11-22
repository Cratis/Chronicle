// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_reusing_an_event_context_mapper : given.a_reused_dynamic_mapper
{
    void Because()
    {
        var first = given.an_appended_event.with_slot("first");
        var second = first with { Context = first.Context with { EventType = new("second", 1) } };
        first = first with { Context = first.Context with { EventType = new("first", 1) } };
        Map("values.$eventContext.eventType.id", first, second);
    }

    [Fact] void should_resolve_the_first_event() => _first.PropertyPath.Path.ShouldEqual("values.first");
    [Fact] void should_resolve_the_second_event() => _second.PropertyPath.Path.ShouldEqual("values.second");
    [Fact] void should_report_a_new_value() => _first.Changed.ShouldEqual("value");
    [Fact] void should_report_no_previous_value_for_a_new_key() => _second.Original.ShouldBeNull();
    [Fact] void should_preserve_the_array_indexers() => _second.ArrayIndexers.ShouldEqual(ArrayIndexers.NoIndexers);
    [Fact] void should_not_report_assignment_as_removal() => _second.IsRemoval.ShouldBeFalse();
    [Fact] void should_keep_the_first_key_when_mapping_another_event() => _repeated.Original.ShouldEqual("value");
    [Fact] void should_return_to_the_first_resolved_path() => _repeated.PropertyPath.Path.ShouldEqual("values.first");
    [Fact] void should_report_the_removal_path() => _removal.PropertyPath.Path.ShouldEqual("values.second");
    [Fact] void should_report_the_removed_value() => _removal.Original.ShouldEqual("value");
    [Fact] void should_mark_the_difference_as_a_removal() => _removal.IsRemoval.ShouldBeTrue();
    [Fact] void should_leave_no_value_in_the_removed_difference() => _removal.Changed.ShouldBeNull();
    [Fact] void should_remove_only_the_second_key() => ((IDictionary<string, object>)((dynamic)_target).values).Keys.ShouldEqual<string>("first");
}
