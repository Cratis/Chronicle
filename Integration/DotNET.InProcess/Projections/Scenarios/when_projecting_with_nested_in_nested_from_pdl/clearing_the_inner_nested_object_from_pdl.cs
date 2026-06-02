// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested_from_pdl;

/// <summary>
/// Verifies that the inner <c>clear with PdlDeepNestedValidationRemoved</c> directive
/// compiles into a <c>RemovedWith</c> entry on the inner nested definition, while the outer
/// block continues to carry its own command-cleared <c>RemovedWith</c>. This is the PDL-level
/// counterpart of Phase 1's
/// <c>when_projecting_with_nested_in_nested.clearing_the_inner_nested_object</c>, which already
/// verifies that the engine clears only the inner validation when this event is appended.
/// </summary>
public class clearing_the_inner_nested_object_from_pdl : given.a_compiled_pdl_nested_projection
{
    [Fact] void should_have_inner_removed_with_for_validation_removed() => _innerNested.RemovedWith.ContainsKey((EventType)nameof(given.PdlDeepNestedValidationRemoved)).ShouldBeTrue();
    [Fact] void should_not_remove_the_outer_command_with_validation_removed() => _outerNested.RemovedWith.ContainsKey((EventType)nameof(given.PdlDeepNestedValidationRemoved)).ShouldBeFalse();
}
