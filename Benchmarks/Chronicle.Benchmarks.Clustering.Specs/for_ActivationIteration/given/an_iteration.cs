// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ActivationIteration.given;

public class an_iteration : Specification
{
    protected List<string> _calls = [];
    protected ActivationIteration _iteration = null!;
    protected TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected Task _preparation = Task.CompletedTask;
    protected string[] _beforeCompletion = [];

    void Establish() => _iteration = new(
        () => Record("prime"),
        () => Record("deactivate"),
        () => Record("restart-and-refresh"),
        () => _calls.Add("capture"));

    async Task Record(string action)
    {
        _calls.Add(action);
        await _completion.Task;
    }
}
