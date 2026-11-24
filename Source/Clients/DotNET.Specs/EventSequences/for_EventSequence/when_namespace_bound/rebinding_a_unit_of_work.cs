// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class rebinding_a_unit_of_work : given.a_namespace_bound_append
{
    Exception _error;

    void Because()
    {
        var unit = CreateUnitOfWork();
        unit.BindNamespace(_binding);
        _error = Catch.Exception(() => unit.BindNamespace(_binding with { Incarnation = Guid.NewGuid() }));
    }

    [Fact] void should_refuse_to_switch_incarnations() => _error.ShouldBeOfExactType<NamespaceBindingNotValid>();
}
