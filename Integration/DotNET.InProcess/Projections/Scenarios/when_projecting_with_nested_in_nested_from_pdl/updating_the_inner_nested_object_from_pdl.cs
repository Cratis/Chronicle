// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested_from_pdl;

/// <summary>
/// Verifies that PDL compilation handles a second <c>from</c> on the inner nested block —
/// the validation update event — and maps its renamed property (<c>newRules</c>) onto the
/// same <c>rules</c> read-model property. This is the PDL-level counterpart of Phase 1's
/// <c>when_projecting_with_nested_in_nested.updating_the_inner_nested_object</c>, which
/// already verifies that the engine updates the inner validation from such a definition.
/// </summary>
public class updating_the_inner_nested_object_from_pdl : given.a_compiled_pdl_nested_projection
{
    [Fact] void should_have_the_validation_updated_from_event() => _innerNested.From.ContainsKey((EventType)nameof(given.PdlDeepNestedValidationUpdated)).ShouldBeTrue();
    [Fact] void should_map_the_updated_rules_property_to_rules() => _innerNested.From[(EventType)nameof(given.PdlDeepNestedValidationUpdated)].Properties[(PropertyPath)"rules"].ShouldEqual("newRules");
    [Fact] void should_keep_the_initial_validation_configured_from_event() => _innerNested.From.ContainsKey((EventType)nameof(given.PdlDeepNestedValidationConfigured)).ShouldBeTrue();
}
