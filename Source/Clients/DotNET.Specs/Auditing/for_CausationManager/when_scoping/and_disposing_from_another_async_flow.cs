// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_scoping;

public class and_disposing_from_another_async_flow : Specification
{
    const string Scoped = "Scoped";
    const string Added = "Added";

    CausationManager _manager;
    IEnumerable<Causation> _afterChain;

    void Establish() => _manager = new();

    async Task Because()
    {
        var scope = _manager.BeginScope(Scoped, new Dictionary<string, string>());
        _manager.Add(Added, new Dictionary<string, string>());
        await Task.Run(scope.Dispose);
        _afterChain = _manager.GetCurrentChain();
    }

    [Fact] void should_remove_the_scope_and_everything_added_after_it_in_the_originating_flow() => _afterChain.ShouldContainOnly(_manager.Root);
}
