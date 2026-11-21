// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_IWebhookDefinitionBuilder;

public class when_using_a_legacy_builder : Specification
{
    IWebhookDefinitionBuilder _builder;
    IWebhookDefinitionBuilder _continuation;
    Exception _exception;

    void Establish() => _builder = new LegacyBuilder();

    void Because()
    {
        _continuation = _builder.WithSigning(WebhookSigningMode.None);
        _exception = Catch.Exception(() => _builder.WithSigning(WebhookSigningMode.Ed25519));
    }

    [Fact] void should_allow_the_unsigned_default() => _continuation.ShouldEqual(_builder);
    [Fact] void should_reject_unsupported_signing() => _exception.ShouldBeOfExactType<WebhookSigningNotSupported>();

    class LegacyBuilder : IWebhookDefinitionBuilder
    {
        public IWebhookDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId) => this;
        public IWebhookDefinitionBuilder WithBasicAuth(string username, string password) => this;
        public IWebhookDefinitionBuilder WithBearerToken(string token) => this;
        public IWebhookDefinitionBuilder WithHeader(string key, string value) => this;
        public IWebhookDefinitionBuilder WithEventType<TEventType>() => this;
        public IWebhookDefinitionBuilder NotReplayable() => this;
        public IWebhookDefinitionBuilder NotActive() => this;
    }
}
