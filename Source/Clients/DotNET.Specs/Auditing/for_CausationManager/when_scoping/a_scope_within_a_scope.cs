// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_scoping;

public class a_scope_within_a_scope : Specification
{
    const string Outer = "Outer";
    const string Inner = "Inner";

    CausationManager _manager;

    public a_scope_within_a_scope()
    {
        // The specification runner uses IAsyncLifetime, which puts Establish and Because in a different async
        // context than the assertions. The chain is ambient state, so it has to be built here.
        _manager = new();

        using (_manager.BeginScope(Outer, new Dictionary<string, string>()))
        {
            using (_manager.BeginScope(Inner, new Dictionary<string, string>()))
            {
                InnerChain = _manager.GetCurrentChain();
            }

            OuterChain = _manager.GetCurrentChain();
        }

        AfterChain = _manager.GetCurrentChain();
    }

    IEnumerable<Causation> InnerChain { get; }

    IEnumerable<Causation> OuterChain { get; }

    IEnumerable<Causation> AfterChain { get; }

    [Fact] void should_hold_both_causations_inside_the_inner_scope() => InnerChain.Count().ShouldEqual(3);
    [Fact] void should_have_the_inner_causation_last() => InnerChain.Last().Type.Value.ShouldEqual(Inner);
    [Fact] void should_have_the_outer_causation_one_level_up() => InnerChain.ElementAt(1).Type.Value.ShouldEqual(Outer);
    [Fact] void should_drop_the_inner_causation_when_it_is_disposed() => OuterChain.Any(_ => _.Type.Value == Inner).ShouldBeFalse();
    [Fact] void should_keep_the_outer_causation_when_the_inner_is_disposed() => OuterChain.Last().Type.Value.ShouldEqual(Outer);
    [Fact] void should_leave_only_the_root_behind() => AfterChain.Count().ShouldEqual(1);
}
