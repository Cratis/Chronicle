// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.given;

public class a_client_subscription_keeper : Specification
{
    protected const int ChecksBeforeTheStreamCloses = 3;
    protected static readonly TimeSpan Revise = TimeSpan.FromMilliseconds(10);
    protected static readonly TimeSpan SafetyNet = TimeSpan.FromHours(1);
    protected static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    protected int _checks;
    protected bool _completed;
    protected TaskCompletionSource _siloLeft = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected ISiloDepartures _siloDepartures;
    protected readonly CancellationTokenSource _streamClosed = new();

    void Establish()
    {
        _siloDepartures = Substitute.For<ISiloDepartures>();
        _siloDepartures.Next.Returns(_ => _siloLeft.Task);
    }

    void Destroy() => _streamClosed.Dispose();

    /// <summary>
    /// Performs one check.
    /// </summary>
    /// <param name="check">The number of the check, starting at 1.</param>
    /// <returns>Whether the subscription was found intact.</returns>
    protected virtual Task<bool> Check(int check) => Task.FromResult(false);

    protected virtual TimeSpan Settle => TimeSpan.FromHours(1);

    protected virtual bool CloseTheStreamAt(int check) => check == ChecksBeforeTheStreamCloses;

    protected Task Keep() => ClientSubscriptionKeeper.KeepWhileConnected(
        async () =>
        {
            var check = Interlocked.Increment(ref _checks);
            if (CloseTheStreamAt(check))
            {
                await _streamClosed.CancelAsync();
            }

            return await Check(check);
        },
        new SubscriptionCheckSchedule(Revise, SafetyNet, Settle),
        _siloDepartures,
        TimeProvider.System,
        "the-client",
        NullLogger.Instance,
        _streamClosed.Token);

    protected async Task KeepUntilTheStreamCloses()
    {
        try
        {
            await Keep().WaitAsync(Patience);
            _completed = true;
        }
        catch (TimeoutException)
        {
            _completed = false;
        }
    }

    protected async Task WaitForChecks(int checks)
    {
        var waiting = System.Diagnostics.Stopwatch.StartNew();
        while (Volatile.Read(ref _checks) < checks && waiting.Elapsed < Patience)
        {
            await Task.Delay(Revise);
        }
    }
}
