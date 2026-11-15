// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.given;

public class a_running_attempt : an_occurrence_client
{
    protected readonly ManualOccurrenceClock _clock = new();
    protected readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly CancellationTokenSource _session = new();
    protected readonly List<long> _renewalTimes = [];
    protected Func<int, Task<CommandResult<CompleteOccurrenceResponse>>> _renew = _ => Task.FromResult(Renewed());
    protected CancellationToken _handlerToken;
    protected CancellationToken _renewalToken;
    protected Task _running;
    protected int _renewalCount;
    protected readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Channel<int> _renewals = Channel.CreateUnbounded<int>();

    void Establish()
    {
        _handler.OnHandle = (_, _, token) =>
        {
            _handlerToken = token;
            _entered.TrySetResult();
            return _release.Task.WaitAsync(token);
        };
        _service.RenewOccurrenceLease(Arg.Any<RenewOccurrenceLeaseRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _renewalToken = call.Arg<CallContext>().CancellationToken;
            _renewalTimes.Add(_clock.GetTimestamp());
            var count = ++_renewalCount;
            var response = _renew(count);
            _renewals.Writer.TryWrite(count);
            return response;
        });
    }

    protected static CommandResult<CompleteOccurrenceResponse> Renewed() => CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new() { Status = OccurrenceLeaseResult.Updated });

    protected static Task<CommandResult<CompleteOccurrenceResponse>> Unavailable() => Task.FromException<CommandResult<CompleteOccurrenceResponse>>(new RpcException(new Status(StatusCode.Unavailable, "temporarily unavailable")));

    protected async Task Start()
    {
        _running = CreateAttempt(_clock).Run(_claim, _clock.GetTimestamp(), _session.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _clock.WaitForTimer(TimeSpan.FromSeconds(20));
    }

    protected async Task RenewAtTwentySeconds()
    {
        await Start();
        _clock.Advance(TimeSpan.FromSeconds(20));
        await NextRenewal();
    }

    protected async Task NextRenewal()
    {
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _renewals.Reader.ReadAsync(guard.Token);
    }

    protected async Task<bool> AdvanceRetry()
    {
        using var stop = new CancellationTokenSource();
        var backoff = _clock.WaitForTimer(TimeSpan.FromSeconds(1), stop.Token);
        if (await Task.WhenAny(backoff, _running) == _running)
        {
            await stop.CancelAsync();
            try { await backoff; }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { /* No retry was scheduled before termination. */ }
            return false;
        }
        await backoff;
        _clock.Advance(TimeSpan.FromSeconds(1));
        await NextRenewal();
        return true;
    }

    protected async Task Finish()
    {
        _release.TrySetResult();
        await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    async Task Destroy()
    {
        await _session.CancelAsync();
        _release.TrySetResult();
        if (_running is not null) await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _session.Dispose();
    }
}
