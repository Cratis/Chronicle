// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested_from_pdl;

/// <summary>
/// Verifies that PDL compilation produces the inner <c>nested validation</c> entry inside
/// the outer command block, with the expected <c>From</c> mapping for
/// <see cref="given.PdlDeepNestedValidationConfigured"/>. This is the PDL-level counterpart of
/// Phase 1's <c>when_projecting_with_nested_in_nested.setting_the_inner_nested_object</c>,
/// which already verifies that the engine sets the inner validation from such a definition.
/// </summary>
public class setting_the_inner_nested_object_from_pdl : given.a_compiled_pdl_nested_projection
{
    [Fact] void should_have_an_inner_nested_dictionary_on_the_outer() => _outerNested.Nested.ShouldNotBeNull();
    [Fact] void should_have_the_inner_nested_entry_keyed_by_validation() => _outerNested.Nested.ContainsKey((PropertyPath)"validation").ShouldBeTrue();
    [Fact] void should_have_inner_nested_identified_by_not_set() => _innerNested.IdentifiedBy.IsSet.ShouldBeFalse();
    [Fact] void should_have_the_validation_configured_from_event() => _innerNested.From.ContainsKey((EventType)nameof(given.PdlDeepNestedValidationConfigured)).ShouldBeTrue();
    [Fact] void should_map_the_validation_rules_property() => _innerNested.From[(EventType)nameof(given.PdlDeepNestedValidationConfigured)].Properties[(PropertyPath)"rules"].ShouldEqual("rules");
}
