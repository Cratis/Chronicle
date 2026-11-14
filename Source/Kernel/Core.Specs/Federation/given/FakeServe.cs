// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake acceptor: sends the scripted pull requests as frames, one at a time, and keeps the answers it gets.
/// </summary>
public class FakeServe : IFederationServe
{
    /// <summary>
    /// Gets the pull requests to send, in order. Every call replays the script.
    /// </summary>
    public List<ServePullRequest> Script { get; } = [];

    /// <summary>
    /// Gets the answers received, in order, across calls.
    /// </summary>
    public List<ServeBatch> Received { get; } = [];

    /// <summary>
    /// Gets or sets whether a call ends once the script is done. Otherwise it stays open until cancelled, like an acceptor with nothing to ask for.
    /// </summary>
    public bool EndAfterScript { get; set; }

    /// <summary>
    /// Gets how many calls were opened.
    /// </summary>
    public int Calls => _calls;

    int _calls;

    /// <inheritdoc/>
    public async IAsyncEnumerable<ServePullRequest> Serve(IAsyncEnumerable<ServeBatch> batches, CallContext callContext = default)
    {
        Interlocked.Increment(ref _calls);
        var cancellationToken = callContext.CancellationToken;
        await using var answers = batches.GetAsyncEnumerator(cancellationToken);
        foreach (var request in Script.ToArray())
        {
            yield return request;
            if (request.Status == FederatedEventBatchStatus.Denied)
            {
                yield break;
            }

            if (!await answers.MoveNextAsync())
            {
                yield break;
            }

            lock (Received)
            {
                Received.Add(answers.Current);
            }
        }

        if (!EndAfterScript)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}
