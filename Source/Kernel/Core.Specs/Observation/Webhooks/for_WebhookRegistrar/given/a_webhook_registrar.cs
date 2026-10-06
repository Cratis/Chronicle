// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.given;

public class a_webhook_registrar : Specification
{
    protected WebhookRegistrar _registrar;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IWebhookDefinitionComparer _webhookDefinitionComparer;
    protected IEncryption _encryption;
    protected IOAuthClient _oauthClient;
    protected IWebhookMediator _webhookMediator;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _webhookDefinitionComparer = Substitute.For<IWebhookDefinitionComparer>();
        _encryption = Substitute.For<IEncryption>();
        _oauthClient = Substitute.For<IOAuthClient>();
        _webhookMediator = Substitute.For<IWebhookMediator>();
        _registrar = new WebhookRegistrar(_grainFactory, _webhookDefinitionComparer, _encryption, _oauthClient, _webhookMediator, Options.Create(new ChronicleOptions()));
    }

    protected static IEventSequence AnEventSequenceThatAppendsSuccessfully()
    {
        var eventSequence = Substitute.For<IEventSequence>();
        eventSequence.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(_ => AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        return eventSequence;
    }
}
