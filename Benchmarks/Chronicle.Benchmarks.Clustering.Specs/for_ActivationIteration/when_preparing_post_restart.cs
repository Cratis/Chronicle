// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ActivationIteration;

public class when_preparing_post_restart : given.an_iteration
{
    async Task Because()
    {
        _preparation = _iteration.Prepare(GrainActivationMode.PostRestart);
        _beforeCompletion = [.. _calls];
        _completion.SetResult();
        await _preparation;
        _iteration.Verify(() => _calls.Add("verify"));
    }

    [Fact] void should_await_host_replacement_and_reference_refresh() => _beforeCompletion.ShouldEqual(["restart-and-refresh"]);
    [Fact] void should_not_prime_the_replacement_target() => _calls.ShouldEqual(["restart-and-refresh", "capture", "verify"]);
}
