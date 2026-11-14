// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The dialer's side of producing: it answers the pull requests the acceptor sends over the connection it dialed with batches from its own
/// outbox, so the one connection carries events in both directions.
/// </summary>
/// <remarks>
/// It owns one background loop per connection. The loop keeps the call open, and when the acceptor refuses the direction or the call fails it
/// waits and opens it again on the same connection, so the direction the dialer consumes in is never disturbed. When the connection is
/// replaced, the loop is stopped and started again on the new one; the acceptor holds the cursor, so nothing is lost or repeated.
/// </remarks>
/// <param name="pullService">The <see cref="IFederationPullService"/> that reads the permitted public events of the outbox.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
/// <param name="timing">The optional <see cref="FederationConsumerTiming"/>.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
public sealed class FederationServeClient(
    IFederationPullService pullService,
    IOptions<ChronicleOptions> options,
    ILogger<FederationServeClient> logger,
    FederationConsumerTiming? timing = null,
    TimeProvider? timeProvider = null) : IDisposable
{
    /// <summary>
    /// The link id the reads of the dialer's own outbox are recorded against.
    /// </summary>
    public const string LinkId = "serve";

    readonly FederationConsumerTiming _timing = timing ?? FederationConsumerTiming.Default;
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
#pragma warning disable CA2213 // Disposed once the loop that uses its token has ended, by Stop.
    CancellationTokenSource? _cancellation;
#pragma warning restore CA2213
    Task _loop = Task.CompletedTask;

    /// <summary>
    /// Gets a value indicating whether the serve call is currently open and answering.
    /// </summary>
    public bool IsServing { get; private set; }

    /// <summary>
    /// Gets how many times the acceptor refused the direction since this client started.
    /// </summary>
    public int Refusals { get; private set; }

    /// <summary>
    /// Start serving over a connection, replacing any earlier loop.
    /// </summary>
    /// <param name="connection">The connection the pull is made over, and now the serve call too.</param>
    public void Start(IFederationPullConnection connection)
    {
        Stop();
        var dial = options.Value.Federation.Dial;
        if (!dial.Serve)
        {
            return;
        }

        _cancellation = new();
        var token = _cancellation.Token;
        _loop = DetachedExecution.Run(() => Run(connection, dial, token));
    }

    /// <summary>
    /// Stop serving.
    /// </summary>
    public void Stop()
    {
        var cancellation = _cancellation;
        _cancellation = null;
        IsServing = false;
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        _ = _loop.ContinueWith(_ => cancellation.Dispose(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    /// <inheritdoc/>
    public void Dispose() => Stop();

    static FederatedEventBatchResponse ToResponse(FederatedEventBatch batch) => new()
    {
        Status = batch.Status,
        OriginEventStore = batch.OriginEventStore,
        Events = [.. batch.Events.Select(_ => _.ToContract())],
        NextSequenceNumber = batch.NextSequenceNumber,
        TailSequenceNumber = batch.TailSequenceNumber
    };

    async Task Run(IFederationPullConnection connection, FederationDialOptions dial, CancellationToken cancellationToken)
    {
        var backoff = TimeSpan.Zero;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (await Call(connection, dial, cancellationToken))
                {
                    Refusals++;
                    await Task.Delay(_timing.DeniedBackoff, _timeProvider, cancellationToken);
                    continue;
                }

                backoff = TimeSpan.Zero;
                await Task.Delay(_timing.InitialBackoff, _timeProvider, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                IsServing = false;
                backoff = _timing.NextBackoff(backoff);
                logger.ServeCallFailed(exception.GetType().Name, backoff);
                try
                {
                    await Task.Delay(backoff, _timeProvider, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        IsServing = false;
    }

    async Task<bool> Call(IFederationPullConnection connection, FederationDialOptions dial, CancellationToken cancellationToken)
    {
        var answers = Channel.CreateUnbounded<ServeBatch>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        var grant = new FederationGrant(
            !dial.ServeEventTypes.Any(),
            dial.ServeEventTypes.Select(_ => new EventTypeId(_)).ToHashSet());
        try
        {
            var requests = connection.ServeService.Serve(answers.Reader.ReadAllAsync(cancellationToken), new CallContext(new CallOptions(cancellationToken: cancellationToken)));
            await foreach (var request in requests.WithCancellation(cancellationToken))
            {
                if (request.Status == FederatedEventBatchStatus.Denied)
                {
                    IsServing = false;
                    logger.ServeRefused();
                    return true;
                }

                IsServing = true;
                var batch = await pullService.Read(
                    LinkId,
                    dial.EventStore,
                    dial.Namespace,
                    request.FromSequenceNumber,
                    request.Limit,
                    request.EventTypes,
                    [grant]);
                await answers.Writer.WriteAsync(new ServeBatch { RequestId = request.RequestId, Batch = ToResponse(batch) }, cancellationToken);
            }

            return false;
        }
        finally
        {
            answers.Writer.TryComplete();
        }
    }
}
