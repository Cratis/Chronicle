// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_ProviderHosts;

public class when_host_disposal_never_completes : Specification
{
    readonly TaskCompletionSource _disposal = new();
    Task _teardown = default!;
    async Task Because()
    {
        var host = new DisposalHost(() => new(_disposal.Task));
        _teardown = ProviderHosts.DisposeAll([Task.FromResult<IStorageProviderHost>(host)], TimeSpan.Zero);
        await _teardown.WaitAsync(TimeSpan.FromSeconds(1));
    }
    [Fact] void should_finish_at_the_shutdown_deadline() => _teardown.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_require_disposal_to_finish() => _disposal.Task.IsCompleted.ShouldBeFalse();
}
