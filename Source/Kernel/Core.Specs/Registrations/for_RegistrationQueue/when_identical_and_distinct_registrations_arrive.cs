// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationQueue;

public class when_identical_and_distinct_registrations_arrive : Specification
{
    const int Limit = 2;
    const int Identical = 20;
    const int Distinct = 6;

    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Dictionary<string, int> _applications = [];
    readonly object _lock = new();
    RegistrationQueue _queue;
    Task[] _callers;
    int _running;
    int _mostRunning;
    int _runningWhileHeld;
    int _waitingWhileHeld;

    void Establish() => _queue = new(Limit, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5), new ManualTimeProvider());

    async Task Because()
    {
        Task Submit(string key) => _queue.Run(key, async _ =>
        {
            lock (_lock)
            {
                _applications[key] = _applications.GetValueOrDefault(key) + 1;
                _running++;
                _mostRunning = Math.Max(_mostRunning, _running);
            }

            await _release.Task;
            lock (_lock)
            {
                _running--;
            }
        });

        _callers =
        [
            .. Enumerable.Range(0, Identical).Select(_ => Submit("same")),
            .. Enumerable.Range(0, Distinct).Select(index => Submit($"distinct-{index}")),
            .. Enumerable.Range(0, Identical).Select(_ => Submit("same"))
        ];
        _runningWhileHeld = _queue.Running;
        _waitingWhileHeld = _queue.Waiting;
        _release.SetResult();
        await Task.WhenAll(_callers);
    }

    [Fact] void should_run_no_more_than_the_limit_at_once() => _mostRunning.ShouldEqual(Limit);
    [Fact] void should_run_the_limit_while_the_rest_wait() => _runningWhileHeld.ShouldEqual(Limit);
    [Fact] void should_queue_every_other_distinct_registration() => _waitingWhileHeld.ShouldEqual(Distinct + 1 - Limit);
    [Fact] void should_apply_every_distinct_registration_once() => _applications.Values.All(count => count == 1).ShouldBeTrue();
    [Fact] void should_apply_each_distinct_key() => _applications.Count.ShouldEqual(Distinct + 1);
    [Fact] void should_complete_every_caller() => _callers.All(task => task.IsCompletedSuccessfully).ShouldBeTrue();
    [Fact] void should_leave_nothing_running() => _queue.Running.ShouldEqual(0);
}
