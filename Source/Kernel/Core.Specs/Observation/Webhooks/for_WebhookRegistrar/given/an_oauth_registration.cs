// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.given;

public class an_oauth_registration : a_webhook_registrar
{
    protected EventToAppend[] _batch;
    protected IEventSequence _sequence;
    protected Contracts.Observation.Webhooks.WebhookDefinition _definition;

    void Establish()
    {
        _sequence = AnEventSequenceThatAppendsSuccessfully();
        _sequence.AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>())
            .Returns(call =>
            {
                _batch = call.Arg<IEnumerable<EventToAppend>>().ToArray();
                return new AppendManyResult();
            });
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_sequence);
        var manager = Substitute.For<IWebhooks>();
        manager.GetWebhookDefinitions().Returns([]);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(manager);
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.New, null));
        _encryption.Encrypt(Arg.Any<string>()).Returns("encrypted-secret");
        _definition = new()
        {
            Identifier = "webhook",
            Target = new()
            {
                Url = "https://target.example",
                Authorization = new(new Contracts.Security.OAuthAuthorization
                {
                    Authority = "https://authority.example", ClientId = "client", ClientSecret = "secret", Scope = "api://resource/.default"
                })
            }
        };
    }
}
