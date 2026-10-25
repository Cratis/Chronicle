// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading;

public class and_the_caller_cancels : given.a_decision_reader
{
    readonly CancellationTokenSource _cancellation = new();
    readonly TaskCompletionSource _dehydrated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _cleanupTokenWasCancelled;

    void Establish()
    {
        _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>())
            .Returns(_ =>
            {
                _cancellation.Cancel();
                return new GetInstanceByKeyResponse { LastHandledEventSequenceNumber = _last, ReadModel = _json };
            });
        _readModels.DehydrateSession(Arg.Any<DehydrateSessionRequest>(), Arg.Any<CallContext>())
            .Returns(call =>
            {
                _cleanupTokenWasCancelled = call.Arg<CallContext>().CancellationToken.IsCancellationRequested;
                _dehydrated.SetResult();
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        await _reader.GetDetached<Model>("source", _cancellation.Token);
        await _dehydrated.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_cancel_the_caller_token() => _cancellation.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_use_a_non_cancelled_cleanup_token() => _cleanupTokenWasCancelled.ShouldBeFalse();
    [Fact] void should_send_dehydrate() => _readModels.Received(1).DehydrateSession(Arg.Any<DehydrateSessionRequest>(), Arg.Is<CallContext>(context => !context.CancellationToken.IsCancellationRequested));
}
