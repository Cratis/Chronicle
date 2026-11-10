// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_RunFailure;

/// <summary>
/// Specifies that timeouts are could-not-run results, not dirty durability verdicts.
/// </summary>
public class when_classifying_incomplete_runs : Specification
{
    RunFailure _failure = null!;

    void Because() => _failure = RunFailure.From(1, "run", new TimeoutException("control operation timed out"), userCanceled: false);

    [Fact] void should_use_the_could_not_run_exit_code() => _failure.ExitCode.ShouldEqual(1);
    [Fact] void should_record_timeout_status() => _failure.Status.ShouldEqual("timeout");
    [Fact] void should_preserve_the_seed() => _failure.Seed.ShouldEqual(1);
    [Fact] void should_preserve_diagnostics() => _failure.Reason.ShouldContain("control operation timed out");
    [Fact] void should_classify_deadline_cancellation_as_timeout() => RunFailure.From(1, "run", new OperationCanceledException(), userCanceled: false).Status.ShouldEqual("timeout");
    [Fact] void should_distinguish_user_cancellation() => RunFailure.From(1, "run", new OperationCanceledException(), userCanceled: true).Status.ShouldEqual("canceled");
    [Fact] void should_not_call_an_infrastructure_error_a_timeout() => RunFailure.From(1, "run", new InvalidOperationException("startup failed"), userCanceled: false).Status.ShouldEqual("could-not-run");
}
