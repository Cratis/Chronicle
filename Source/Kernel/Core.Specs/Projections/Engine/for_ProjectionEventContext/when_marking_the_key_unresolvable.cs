// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext;

public class when_marking_the_key_unresolvable : given.a_context
{
    ProjectionEventContext _copy;

    void Establish() => _copy = _context with { NeedsInitialState = true };
    void Because() => _context.MarkKeyUnresolvable();

    [Fact] void should_mark_the_context_unresolvable() => _context.IsUnresolvable.ShouldBeTrue();
    [Fact] void should_not_change_the_scalar_flag_on_an_existing_copy() => _copy.IsUnresolvable.ShouldBeFalse();
}
