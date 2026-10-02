// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_scoping;

public class and_adding_a_causation : Specification
{
    const string Scoped = "Scoped";
    const string Added = "Added";

    CausationManager _manager;
    IEnumerable<Causation> _insideChain;
    IEnumerable<Causation> _afterChain;

    void Establish() => _manager = new();

    void Because()
    {
        using (_manager.BeginScope(Scoped, new Dictionary<string, string>()))
        {
            _manager.Add(Added, new Dictionary<string, string>());
            _insideChain = _manager.GetCurrentChain();
        }

        _afterChain = _manager.GetCurrentChain();
    }

    [Fact] void should_include_the_scoped_and_added_causations_inside_the_scope() => _insideChain.Count().ShouldEqual(3);
    [Fact] void should_have_the_added_causation_last_inside_the_scope() => _insideChain.Last().Type.Value.ShouldEqual(Added);
    [Fact] void should_remove_the_scope_and_everything_added_after_it() => _afterChain.ShouldContainOnly(_manager.Root);
}
