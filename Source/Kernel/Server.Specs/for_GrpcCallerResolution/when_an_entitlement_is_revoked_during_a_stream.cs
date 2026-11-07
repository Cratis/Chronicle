// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution;

public class when_an_entitlement_is_revoked_during_a_stream : given.a_grpc_host_with_store_entitlements
{
    bool _permittedBeforeRevocation;
    Exception _error;

    async Task Because()
    {
        var stream = _probe.Watch(new() { Store = Store }, As(EntitledSubject)).Replay();
        using var connection = stream.Connect();
        _permittedBeforeRevocation = (await stream.FirstAsync().Timeout(TimeSpan.FromSeconds(10))).Permitted;

        Volatile.Write(ref _revoked, true);
        _changes.OnNext(Unit.Default);

        _error = await Catch.Exception(async () => await stream.LastOrDefaultAsync().Timeout(TimeSpan.FromSeconds(10)));
    }

    [Fact] void should_stream_while_entitled() => _permittedBeforeRevocation.ShouldBeTrue();
    [Fact] void should_end_the_stream_as_refused() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
