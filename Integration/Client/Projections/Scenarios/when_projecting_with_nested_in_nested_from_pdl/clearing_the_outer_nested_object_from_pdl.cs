// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested_from_pdl;

/// <summary>
/// Verifies that the outer <c>clear with PdlDeepNestedCommandCleared</c> directive
/// compiles into a <c>RemovedWith</c> entry on the outer nested definition, while the
/// inner block has its own (different) <c>RemovedWith</c>. This is the PDL-level counterpart
/// of Phase 1's <c>when_projecting_with_nested_in_nested.clearing_the_outer_nested_object</c>,
/// which already verifies that the engine clears the outer command (and discards the inner
/// validation along with it) when this event is appended.
/// </summary>
public class clearing_the_outer_nested_object_from_pdl : given.a_compiled_pdl_nested_projection
{
    [Fact] void should_have_outer_removed_with_for_command_cleared() => _outerNested.RemovedWith.ContainsKey((EventType)nameof(given.PdlDeepNestedCommandCleared)).ShouldBeTrue();
    [Fact] void should_not_have_inner_removed_with_for_command_cleared() => _innerNested.RemovedWith.ContainsKey((EventType)nameof(given.PdlDeepNestedCommandCleared)).ShouldBeFalse();
}
