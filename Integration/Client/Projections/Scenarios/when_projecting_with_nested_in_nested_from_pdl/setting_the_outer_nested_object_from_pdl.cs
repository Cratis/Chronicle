// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested_from_pdl;

/// <summary>
/// Verifies that PDL compilation produces the outer <c>nested command</c> entry
/// with the expected <c>From</c> mapping for <see cref="given.PdlDeepNestedCommandSet"/>.
/// This is the PDL-level counterpart of Phase 1's
/// <c>when_projecting_with_nested_in_nested.setting_the_outer_nested_object</c>, which
/// already verifies that the engine sets the outer command from such a definition.
/// </summary>
public class setting_the_outer_nested_object_from_pdl : given.a_compiled_pdl_nested_projection
{
    [Fact] void should_compile_a_definition() => _projection.ShouldNotBeNull();
    [Fact] void should_have_a_top_level_nested_dictionary() => _projection.Nested.ShouldNotBeNull();
    [Fact] void should_have_the_outer_nested_entry_keyed_by_command() => _projection.Nested.ContainsKey((PropertyPath)"command").ShouldBeTrue();
    [Fact] void should_have_outer_nested_identified_by_not_set() => _outerNested.IdentifiedBy.IsSet.ShouldBeFalse();
    [Fact] void should_have_the_command_set_from_event() => _outerNested.From.ContainsKey((EventType)nameof(given.PdlDeepNestedCommandSet)).ShouldBeTrue();
    [Fact] void should_map_the_command_name_property() => _outerNested.From[(EventType)nameof(given.PdlDeepNestedCommandSet)].Properties[(PropertyPath)"name"].ShouldEqual("name");
}
