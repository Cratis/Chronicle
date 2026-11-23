// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_NamespaceBoundAppendStorage;

public class when_another_binding_is_created : given.a_bound_storage
{
    NamespaceCommitContext _next;

    async Task Because()
    {
        _next = _context with { Incarnation = NamespaceIncarnationId.New() };
        var replacement = new NamespaceBoundAppendStorage(_provider, _next);
        await _bound.Append(_event);
        await replacement.Append(_event);
        await _bound.Append(_event);
    }

    [Fact] async Task should_keep_the_first_binding_on_its_original_incarnation() => await _provider.Received(2).Append(_context, _event);
    [Fact] async Task should_only_use_the_new_incarnation_from_the_new_binding() => await _provider.Received(1).Append(_next, _event);
    [Fact] void should_not_refresh_the_captured_activation() => _next.ActivationIncarnation.ShouldEqual(_context.ActivationIncarnation);
}
