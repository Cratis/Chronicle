// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_scoping;

/// <summary>
/// Two scoped causations one after the other describe two independent pieces of work. The second must not read as
/// caused by the first - that is an ordering nothing established, and anything mining the chain would learn it as
/// a fact.
/// </summary>
public class two_scopes_one_after_the_other : Specification
{
    const string First = "First";
    const string Second = "Second";

    CausationManager _manager;

    public two_scopes_one_after_the_other()
    {
        // The specification runner uses IAsyncLifetime, which puts Establish and Because in a different async
        // context than the assertions. The chain is ambient state, so it has to be built here.
        _manager = new();

        using (_manager.BeginScope(First, new Dictionary<string, string>()))
        {
        }

        using (_manager.BeginScope(Second, new Dictionary<string, string>()))
        {
            SecondChain = _manager.GetCurrentChain();
        }
    }

    IEnumerable<Causation> SecondChain { get; }

    [Fact] void should_hold_the_root_and_the_second_causation() => SecondChain.Count().ShouldEqual(2);
    [Fact] void should_hold_the_second_causation() => SecondChain.Last().Type.Value.ShouldEqual(Second);
    [Fact] void should_not_hold_the_first_causation() => SecondChain.Any(_ => _.Type.Value == First).ShouldBeFalse();
    [Fact] void should_leave_only_the_root_behind() => _manager.GetCurrentChain().Count.ShouldEqual(1);
}
