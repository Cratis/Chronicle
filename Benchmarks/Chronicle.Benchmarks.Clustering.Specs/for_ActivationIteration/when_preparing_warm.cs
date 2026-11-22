// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ActivationIteration;

public class when_preparing_warm : given.an_iteration
{
    async Task Because()
    {
        _preparation = _iteration.Prepare(GrainActivationMode.Warm);
        _beforeCompletion = [.. _calls];
        _completion.SetResult();
        await _preparation;
        _iteration.Verify(() => _calls.Add("verify"));
    }

    [Fact] void should_await_priming_before_capturing() => _beforeCompletion.ShouldEqual(["prime"]);
    [Fact] void should_keep_the_same_host_and_activation() => _calls.ShouldEqual(["prime", "capture", "verify"]);
}
