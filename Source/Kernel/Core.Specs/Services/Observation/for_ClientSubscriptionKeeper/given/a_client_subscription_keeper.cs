// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.given;

public class a_client_subscription_keeper : Specification
{
    protected const int ChecksBeforeTheStreamCloses = 3;
    static readonly TimeSpan _period = TimeSpan.FromMilliseconds(10);
    static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);

    protected int _checks;
    protected bool _completed;
    readonly CancellationTokenSource _streamClosed = new();

    void Destroy() => _streamClosed.Dispose();

    protected virtual Task Check(int check) => Task.CompletedTask;

    protected async Task KeepUntilTheStreamCloses()
    {
        var keeping = ClientSubscriptionKeeper.KeepWhileConnected(
            async () =>
            {
                var check = Interlocked.Increment(ref _checks);
                if (check == ChecksBeforeTheStreamCloses)
                {
                    await _streamClosed.CancelAsync();
                }

                await Check(check);
            },
            _period,
            "the-client",
            NullLogger.Instance,
            _streamClosed.Token);

        _completed = await Task.WhenAny(keeping, Task.Delay(_patience)) == keeping;
    }
}
