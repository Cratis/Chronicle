// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager.when_scoping;

public class and_running_in_parallel : Specification
{
    const string Request = "Request";
    const string First = "First";
    const string Second = "Second";

    CausationManager _manager;
    IEnumerable<Causation> _firstChain;
    IEnumerable<Causation> _secondChain;
    IEnumerable<Causation> _parentChain;

    void Establish() => _manager = new();

    async Task Because()
    {
        _manager.Add(Request, new Dictionary<string, string>());
        var firstDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = Task.Run(async () =>
        {
            var scope = _manager.BeginScope(First, new Dictionary<string, string>());
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            scope.Dispose();
            firstDisposed.SetResult();
            return _manager.GetCurrentChain();
        });
        var second = Task.Run(async () =>
        {
            using var scope = _manager.BeginScope(Second, new Dictionary<string, string>());
            secondStarted.SetResult();
            await firstDisposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return _manager.GetCurrentChain();
        });

        await Task.WhenAll(first, second);
        _firstChain = await first;
        _secondChain = await second;
        _parentChain = _manager.GetCurrentChain();
    }

    [Fact] void should_remove_the_first_flows_scope() => _firstChain.Count().ShouldEqual(2);
    [Fact] void should_keep_the_inherited_causation_in_the_first_flow() => _firstChain.Last().Type.Value.ShouldEqual(Request);
    [Fact] void should_leave_the_second_flows_scope_intact() => _secondChain.Count().ShouldEqual(3);
    [Fact] void should_keep_the_second_flows_own_causation_last() => _secondChain.Last().Type.Value.ShouldEqual(Second);
    [Fact] void should_leave_the_parent_chain_unchanged() => _parentChain.Count().ShouldEqual(2);
    [Fact] void should_keep_the_inherited_causation_last_in_the_parent_flow() => _parentChain.Last().Type.Value.ShouldEqual(Request);
}
