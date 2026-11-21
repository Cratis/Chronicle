// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_ProviderHosts;

public class when_an_abandoned_startup_finishes_later : Specification
{
    DisposalHost _host = default!;
    async Task Because()
    {
        var startup = new TaskCompletionSource<IStorageProviderHost>();
        var disposed = new TaskCompletionSource();
        _host = new(() =>
        {
            disposed.SetResult();
            return ValueTask.CompletedTask;
        });
        await ProviderHosts.DisposeAll([startup.Task], TimeSpan.Zero).WaitAsync(TimeSpan.FromSeconds(1));
        startup.SetResult(_host);
        await disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }
    [Fact] void should_dispose_the_late_host() => _host.DisposalCount.ShouldEqual(1);
}
