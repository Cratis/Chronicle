// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_LegacyChildEveryEventMappings.when_normalizing;

public class and_include_children_is_false : given.a_legacy_definition
{
    void Establish() => _definition = _definition with { FromEvery = _definition.FromEvery with { IncludeChildren = false } };

    void Because() => _result = LegacyChildEveryEventMappings.Normalize(_definition, _schema);

    [Fact] void should_leave_the_root_mapping_in_place() => _result.FromEvery.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_not_add_child_mappings() => _result.Children["children"].All.Properties.ShouldBeEmpty();
    [Fact] void should_leave_child_inclusion_disabled() => _result.FromEvery.IncludeChildren.ShouldBeFalse();
}
