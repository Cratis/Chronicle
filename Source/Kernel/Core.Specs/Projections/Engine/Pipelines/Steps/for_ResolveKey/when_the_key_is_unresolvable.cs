// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveKey;

public class when_the_key_is_unresolvable : given.a_resolve_key_step
{
    void Establish() => ResolverReturns(KeyResolverResult.Unresolvable());

    async Task Because() => _result = await _resolveKey.Perform(_projection, _context);

    [Fact] void should_mark_the_context_unresolvable() => _result.IsUnresolvable.ShouldBeTrue();
    [Fact] void should_not_set_a_key() => _result.Key.ShouldEqual(Key.Undefined);
}
