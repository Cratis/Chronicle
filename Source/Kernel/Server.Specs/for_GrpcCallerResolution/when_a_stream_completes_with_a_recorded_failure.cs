// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution;

public class when_a_stream_completes_with_a_recorded_failure : given.a_grpc_host_with_store_entitlements
{
    Exception _error;

    async Task Because()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _error = await Catch.Exception(() => _probe.CompleteWithRecordedFailure(new() { Store = Store }, As(EntitledSubject))
            .LastOrDefaultAsync().ToTask(deadline.Token));
    }

    [Fact] void should_end_the_stream_with_the_recorded_status() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
