// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class binding_after_staging : given.a_namespace_bound_append
{
    Exception _error;

    void Because()
    {
        var unit = CreateUnitOfWork();
        unit.AddEvent(EventSequenceId.Log, "source", "event", Causation.Unknown());
        _error = Catch.Exception(() => unit.BindNamespace(_binding));
    }

    [Fact] void should_not_retroactively_protect_existing_work() => _error.ShouldBeOfExactType<NamespaceBindingNotValid>();
}
