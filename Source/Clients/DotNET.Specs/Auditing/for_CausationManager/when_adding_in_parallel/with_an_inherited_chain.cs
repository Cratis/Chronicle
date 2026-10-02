// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_adding_in_parallel;

public class with_an_inherited_chain : Specification
{
    const string Request = "Request";
    const string First = "First";
    const string Second = "Second";

    CausationManager _manager;
    IImmutableList<Causation> _firstChain;
    IImmutableList<Causation> _secondChain;
    IImmutableList<Causation> _parentChain;

    void Establish() => _manager = new();

    async Task Because()
    {
        _manager.Add(Request, new Dictionary<string, string>());
        var firstAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAdded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = Task.Run(async () =>
        {
            _manager.Add(First, new Dictionary<string, string>());
            firstAdded.SetResult();
            await secondAdded.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return _manager.GetCurrentChain();
        });
        var second = Task.Run(async () =>
        {
            _manager.Add(Second, new Dictionary<string, string>());
            secondAdded.SetResult();
            await firstAdded.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return _manager.GetCurrentChain();
        });

        await Task.WhenAll(first, second);
        _firstChain = await first;
        _secondChain = await second;
        _parentChain = _manager.GetCurrentChain();
    }

    [Fact] void should_keep_the_root_in_the_first_flow() => _firstChain[0].ShouldEqual(_manager.Root);
    [Fact] void should_keep_the_inherited_causation_in_the_first_flow() => _firstChain[1].Type.Value.ShouldEqual(Request);
    [Fact] void should_have_only_three_causations_in_the_first_flow() => _firstChain.Count.ShouldEqual(3);
    [Fact] void should_have_the_first_flows_causation_last() => _firstChain[^1].Type.Value.ShouldEqual(First);
    [Fact] void should_keep_the_root_in_the_second_flow() => _secondChain[0].ShouldEqual(_manager.Root);
    [Fact] void should_keep_the_inherited_causation_in_the_second_flow() => _secondChain[1].Type.Value.ShouldEqual(Request);
    [Fact] void should_have_only_three_causations_in_the_second_flow() => _secondChain.Count.ShouldEqual(3);
    [Fact] void should_have_the_second_flows_causation_last() => _secondChain[^1].Type.Value.ShouldEqual(Second);
    [Fact] void should_leave_the_parent_chain_unchanged() => _parentChain.Count.ShouldEqual(2);
    [Fact] void should_keep_the_inherited_causation_last_in_the_parent_flow() => _parentChain[^1].Type.Value.ShouldEqual(Request);
}
