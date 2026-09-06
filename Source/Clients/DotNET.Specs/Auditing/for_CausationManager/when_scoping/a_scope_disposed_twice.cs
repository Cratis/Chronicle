// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_scoping;

public class a_scope_disposed_twice : Specification
{
    const string Scoped = "Scoped";
    const string Unscoped = "Unscoped";

    CausationManager _manager;

    public a_scope_disposed_twice()
    {
        // The specification runner uses IAsyncLifetime, which puts Establish and Because in a different async
        // context than the assertions. The chain is ambient state, so it has to be built here.
        _manager = new();

        var scope = _manager.BeginScope(Scoped, new Dictionary<string, string>());
        scope.Dispose();

        _manager.Add(Unscoped, new Dictionary<string, string>());
        scope.Dispose();

        Chain = _manager.GetCurrentChain();
    }

    IEnumerable<Causation> Chain { get; }

    [Fact] void should_not_remove_anything_the_second_time() => Chain.Any(_ => _.Type.Value == Unscoped).ShouldBeTrue();
    [Fact] void should_still_have_removed_the_scoped_causation() => Chain.Any(_ => _.Type.Value == Scoped).ShouldBeFalse();
}
