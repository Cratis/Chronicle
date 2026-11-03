// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Regression for #4040: an observer whose activation fails on every attempt must not stop the kernel from becoming
/// ready, neither on first start nor when it is started again against the same backlog (a restart).
/// </summary>
public class and_observer_activation_keeps_failing_across_a_restart : given.a_startup_task_with_a_persisted_reactor
{
    bool _readyAfterFirstStartup;
    Exception _firstError;
    Task _firstRecoverableWork;
    Exception _secondError;

    void Establish() => _reactorObserver.Ensure().Returns(async _ =>
    {
        await Task.Delay(5);
        throw new TimeoutException("Planted activation failure");
    });

    async Task Because()
    {
        _firstError = await Catch.Exception(Execute);
        _readyAfterFirstStartup = _bootstrapClientsEnsured;
        _firstRecoverableWork = RecoverableWork;
        _bootstrapClientsEnsured = false;
        _secondError = await Catch.Exception(Execute);
    }

    void Destroy() => Stop().GetAwaiter().GetResult();

    [Fact] void should_complete_the_first_startup() => _firstError.ShouldBeNull();
    [Fact] void should_be_ready_after_the_first_startup() => _readyAfterFirstStartup.ShouldBeTrue();
    [Fact] void should_complete_the_second_startup() => _secondError.ShouldBeNull();
    [Fact] void should_be_ready_after_the_second_startup() => _bootstrapClientsEnsured.ShouldBeTrue();

    /// <summary>
    /// Both passes share the in-process work runner, which never runs the same item twice concurrently. Whichever pass
    /// registers the failing activation first keeps retrying it and the other skips it, so the pair is asserted.
    /// </summary>
    [Fact] void should_still_be_retrying_in_the_background() => (_firstRecoverableWork.IsCompleted && RecoverableWork.IsCompleted).ShouldBeFalse();
}
