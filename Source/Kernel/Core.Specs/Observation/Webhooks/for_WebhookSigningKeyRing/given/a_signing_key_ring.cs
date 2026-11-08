// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.given;

public class a_signing_key_ring : Specification
{
    protected static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_000_000);

    protected readonly TestKitSilo _silo = new();
    protected IWebhookSigningKeyRing _ring;
    protected IEncryption _encryption;
    protected IEventSequence _eventSequence;
    protected List<(EventSourceId EventSource, object Event)> _appended;
    protected bool _appendSucceeds = true;
    protected readonly List<string> _encrypted = [];
    protected readonly RecordingKeyRingLogger _logger = new();

    protected WebhookSigningKeyRingState State => _silo.StorageManager.GetStorage<WebhookSigningKeyRingState>(typeof(WebhookSigningKeyRing).FullName!).State;

    protected static WebhookSigningKey Key(string id, WebhookSigningKeyStatus status, DateTimeOffset? overlapEndsAt = null) =>
        new(id, WebhookSigningAlgorithm.Ed25519, status, $"whpk_{id}", $"enc:whsk_{id}", null, Now.AddDays(-10), overlapEndsAt, null);

    protected static WebhookSigningKey Secret(string id, WebhookId webhook, WebhookSigningKeyStatus status, DateTimeOffset? overlapEndsAt = null) =>
        new(id, WebhookSigningAlgorithm.HmacSha256, status, string.Empty, $"enc:whsec_{id}", webhook, Now.AddDays(-10), overlapEndsAt, null);

    void Establish()
    {
        _appended = [];
        _encryption = Substitute.For<IEncryption>();
        _encryption.Encrypt(Arg.Any<string>()).Returns(call =>
        {
            _encrypted.Add(call.Arg<string>());
            return "enc:" + call.Arg<string>();
        });
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(call =>
            {
                if (!_appendSucceeds)
                {
                    return AppendResult.Failed(CorrelationId.New(), [new AppendError("refused")]);
                }

                _appended.Add((call.Arg<EventSourceId>(), call.Arg<object>()));
                return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
            });
        _silo.AddProbe<IEventSequence>(_ => _eventSequence);
        _silo.AddService(_encryption);
        _silo.AddService<ILogger<WebhookSigningKeyRing>>(_logger);
        _silo.AddService<TimeProvider>(new FixedTimeProvider(Now));
    }

    protected async Task CreateRing(params WebhookSigningKey[] existing)
    {
        _silo.StorageManager.GetStorage<WebhookSigningKeyRingState>(typeof(WebhookSigningKeyRing).FullName!).State = new() { Keys = existing };
        _ring = await _silo.CreateGrainAsync<WebhookSigningKeyRing>("the-store");
    }

    sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
