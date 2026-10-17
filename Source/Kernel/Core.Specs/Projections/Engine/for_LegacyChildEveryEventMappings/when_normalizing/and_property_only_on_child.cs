// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_LegacyChildEveryEventMappings.when_normalizing;

public class and_property_only_on_child : given.a_legacy_definition
{
    void Establish()
    {
        _definition.FromEvery.Properties["unknown"] = "value";
        _definition.FromEvery.Properties["children.lastValue"] = "value";
        _definition.Children["children"].All.Properties["existing"] = "$value(preserved)";
    }

    void Because() => _result = LegacyChildEveryEventMappings.Normalize(_definition, _schema);

    [Fact] void should_move_the_bare_child_property() => _result.Children["children"].FromEvery.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_keep_both_child_definition_views_consistent() => _result.Children["children"].All.ShouldBeSame(_result.Children["children"].FromEvery);
    [Fact] void should_include_the_childs_descendants() => _result.Children["children"].All.IncludeChildren.ShouldBeTrue();
    [Fact] void should_remove_the_child_mapping_from_root() => _result.FromEvery.Properties.ContainsKey("lastValue").ShouldBeFalse();
    [Fact] void should_preserve_the_root_mapping() => _result.FromEvery.Properties["source"].ShouldEqual("$eventContext(eventSourceId)");
    [Fact] void should_not_copy_parent_fields_into_the_child() => _result.Children["children"].All.Properties.ContainsKey("source").ShouldBeFalse();
    [Fact] void should_preserve_explicit_child_mappings() => _result.Children["children"].All.Properties["existing"].ShouldEqual("$value(preserved)");
    [Fact] void should_leave_unmatched_properties_at_root() => _result.FromEvery.Properties["unknown"].ShouldEqual("value");
    [Fact] void should_leave_non_bare_paths_at_root() => _result.FromEvery.Properties["children.lastValue"].ShouldEqual("value");
    [Fact] void should_leave_the_registered_root_definition_unchanged() => _definition.FromEvery.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_leave_the_registered_child_definition_unchanged() => _definition.Children["children"].All.Properties.ContainsKey("lastValue").ShouldBeFalse();
    [Fact] void should_be_idempotent() => LegacyChildEveryEventMappings.Normalize(_result, _schema).Children["children"].All.Properties.OrderBy(mapping => mapping.Key.Path).SequenceEqual(_result.Children["children"].All.Properties.OrderBy(mapping => mapping.Key.Path)).ShouldBeTrue();
}
