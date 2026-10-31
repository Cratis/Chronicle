// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class without_oauth_scope : given.an_oauth_registration
{
    void Establish() => _definition.Target.Authorization!.Value2!.Scope = string.Empty;

    async Task Because() => await _registrar.Add("store", [_definition]);

    [Fact] void should_append_the_original_authorization_event() => _sequence.Received(1).Append(
        Arg.Any<EventSourceId>(),
        Arg.Is<object>(_ => _ is OAuthAuthorizationSetForWebhook),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>());

    [Fact] async Task should_not_append_a_scope_batch() => await _sequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>());
}
