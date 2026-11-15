// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Cratis.Chronicle.Contracts.Federation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationServeService.given;

/// <summary>
/// A hosted federation authority with a subject that has an inbound policy, and the serve service that accepts its dialer.
/// </summary>
public class a_serve_service : Federation.given.a_peer_admission
{
    protected static readonly FederationConsumerTiming Timing = new(
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(5),
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromSeconds(2));

    protected IFederationCaller _caller;
    protected Federation.given.FakeInbox _inbox;
    protected Federation.given.FakeSubscriptions _subscriptions;
    protected Federation.given.FakeStatus _status;
    protected IFederationConsumerMetrics _consumerMetrics;
    protected Dictionary<string, FederationSession> _sessions;
    protected FederationServeService _service;
    protected X509Certificate2 _presented;

    void Establish()
    {
        _sessions = [];
        _inbox = new();
        _subscriptions = new();
        _subscriptions.Subscriptions.Add(new(["type-a", "type-b"], []));
        _status = new();
        _consumerMetrics = Substitute.For<IFederationConsumerMetrics>();

        _caller = Substitute.For<IFederationCaller>();
        _caller.ClientCertificate.Returns(_ => _presented);

        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IFederationSession>(Arg.Any<string>(), Arg.Any<string?>()).Returns(callInfo =>
        {
            var key = callInfo.ArgAt<string>(0);
            if (!_sessions.TryGetValue(key, out var session))
            {
                _sessions[key] = session = new();
            }

            return session;
        });

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));

        _service = new(
            _caller,
            _admission,
            _inbox,
            _subscriptions,
            _status,
            new FederationRateLimiter(TimeProvider.System),
            new FederationMetrics(meterFactory),
            _consumerMetrics,
            grains,
            Options.Create(_options),
            NullLogger<FederationServeService>.Instance,
            Timing);
    }

    protected static ServeBatch Answer(ServePullRequest request, string origin, params ulong[] sequenceNumbers) => new()
    {
        RequestId = request.RequestId,
        Batch = new()
        {
            Status = FederatedEventBatchStatus.Ok,
            OriginEventStore = origin,
            Events = [.. sequenceNumbers.Select(_ => Event(_, "type-a"))],
            NextSequenceNumber = sequenceNumbers.Length == 0 ? request.FromSequenceNumber : sequenceNumbers.Max() + 1,
            TailSequenceNumber = sequenceNumbers.Length == 0 ? 0 : sequenceNumbers.Max()
        }
    };

    protected static Contracts.Federation.FederatedEvent Event(ulong sequenceNumber, string eventType) => new()
    {
        SequenceNumber = sequenceNumber,
        EventType = eventType,
        Generation = 1,
        EventSourceType = "type",
        EventSourceId = "source",
        EventStreamType = "stream",
        EventStreamId = "stream",
        Occurred = DateTimeOffset.UtcNow,
        CorrelationId = Guid.NewGuid().ToString(),
        Content = "{}"
    };

    /// <summary>
    /// Plays the dialer: answers every request with what <paramref name="answer"/> says, until <paramref name="stop"/> says to stop or the call ends.
    /// </summary>
    /// <param name="answer">What to answer a request with.</param>
    /// <param name="stop">Whether to stop after seeing the request.</param>
    /// <returns>Every frame the acceptor sent.</returns>
    protected async Task<List<ServePullRequest>> Dial(Func<ServePullRequest, ServeBatch> answer, Func<List<ServePullRequest>, bool> stop)
    {
        var answers = Channel.CreateUnbounded<ServeBatch>();
        var frames = new List<ServePullRequest>();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await foreach (var frame in _service.Serve(answers.Reader.ReadAllAsync(cancellation.Token), cancellation.Token).WithCancellation(cancellation.Token))
        {
            frames.Add(frame);
            if (frame.Status == FederatedEventBatchStatus.Denied || stop(frames))
            {
                break;
            }

            await answers.Writer.WriteAsync(answer(frame), cancellation.Token);
        }

        answers.Writer.TryComplete();
        return frames;
    }
}
