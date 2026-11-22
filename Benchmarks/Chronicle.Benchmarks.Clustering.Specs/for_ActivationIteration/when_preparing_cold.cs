// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ActivationIteration;

public class when_preparing_cold : Specification
{
    readonly List<string> _calls = [];
    readonly TaskCompletionSource _deactivation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ActivationIteration _iteration = null!;
    string[] _beforeCompletion = [];

    void Establish() => _iteration = new(
        () =>
        {
            _calls.Add("prime");
            return Task.CompletedTask;
        },
        () =>
        {
            _calls.Add("deactivate");
            return _deactivation.Task;
        },
        () => throw new InvalidOperationException("Must not restart"),
        () => _calls.Add("capture"));

    async Task Because()
    {
        var preparation = _iteration.Prepare(GrainActivationMode.Cold);
        _beforeCompletion = [.. _calls];
        _deactivation.SetResult();
        await preparation;
        _iteration.Verify(() => _calls.Add("verify"));
    }

    [Fact] void should_await_target_deactivation() => _beforeCompletion.ShouldEqual(["prime", "deactivate"]);
    [Fact] void should_capture_only_after_deactivation() => _calls.ShouldEqual(["prime", "deactivate", "capture", "verify"]);
}
