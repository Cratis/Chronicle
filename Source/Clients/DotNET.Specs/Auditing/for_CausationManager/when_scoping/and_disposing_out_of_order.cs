// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_scoping;

public class and_disposing_out_of_order : Specification
{
    const string Outer = "Outer";
    const string Inner = "Inner";
    const string Added = "Added";

    CausationManager _manager;
    IEnumerable<Causation> _afterOuterChain;
    IEnumerable<Causation> _afterInnerChain;

    void Establish() => _manager = new();

    void Because()
    {
        var outer = _manager.BeginScope(Outer, new Dictionary<string, string>());
        var inner = _manager.BeginScope(Inner, new Dictionary<string, string>());
        outer.Dispose();
        _afterOuterChain = _manager.GetCurrentChain();

        _manager.Add(Added, new Dictionary<string, string>());
        inner.Dispose();
        _afterInnerChain = _manager.GetCurrentChain();
    }

    [Fact] void should_remove_both_scopes_after_disposing_the_outer_scope() => _afterOuterChain.ShouldContainOnly(_manager.Root);
    [Fact] void should_keep_causations_added_after_the_outer_scope_was_disposed() => _afterInnerChain.Count().ShouldEqual(2);
    [Fact] void should_not_remove_the_new_causation_after_disposing_the_inner_scope() => _afterInnerChain.Last().Type.Value.ShouldEqual(Added);
}
