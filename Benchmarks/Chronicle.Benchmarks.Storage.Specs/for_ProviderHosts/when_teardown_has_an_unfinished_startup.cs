// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_ProviderHosts;

public class when_teardown_has_an_unfinished_startup : Specification
{
    readonly TaskCompletionSource<IStorageProviderHost> _startup = new();
    DisposalHost _running = default!;
    Task _teardown = default!;
    async Task Because()
    {
        _running = new(() => ValueTask.CompletedTask);
        _teardown = ProviderHosts.DisposeAll([_startup.Task, Task.FromResult<IStorageProviderHost>(_running)], TimeSpan.FromSeconds(60));
        await _teardown.WaitAsync(TimeSpan.FromSeconds(1));
    }
    [Fact] void should_finish_without_waiting_for_startup() => _teardown.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_leave_the_unfinished_startup_unawaited() => _startup.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_dispose_the_running_host() => _running.DisposalCount.ShouldEqual(1);
}
