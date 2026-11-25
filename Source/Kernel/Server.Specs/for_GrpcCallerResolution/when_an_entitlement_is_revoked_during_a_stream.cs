// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution;

public class when_an_entitlement_is_revoked_during_a_stream : given.a_grpc_host_with_store_entitlements
{
    readonly List<given.ProbeReply> _received = [];
    Exception _error;

    async Task Because()
    {
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ended = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = _probe.Watch(new() { Store = Store }, As(EntitledSubject)).Subscribe(
            reply =>
            {
                _received.Add(reply);
                first.TrySetResult();
            },
            error => ended.TrySetResult(error),
            () => ended.TrySetResult(new InvalidOperationException("The stream completed.")));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await first.Task.WaitAsync(deadline.Token);

        Volatile.Write(ref _revoked, true);
        _changes.OnNext(Unit.Default);
        _replies.OnNext(new() { Permitted = false });
        _replies.OnCompleted();

        _error = await ended.Task.WaitAsync(deadline.Token);
    }

    [Fact] void should_deliver_only_the_item_before_revocation() => _received.Select(_ => _.Permitted).ShouldEqual([true]);
    [Fact] void should_end_the_stream_as_refused() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
