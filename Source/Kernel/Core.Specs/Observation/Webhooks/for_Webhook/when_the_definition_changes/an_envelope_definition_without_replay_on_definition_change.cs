// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhook.when_the_definition_changes;

public class an_envelope_definition_without_replay_on_definition_change : given.a_webhook_grain_without_replay_on_definition_change
{
    async Task Establish() => _namespacesGrain.GetAll().Returns([new("namespace")]);

    async Task Because() => await _grain.SetDefinition(_definition with { DeliveryFormat = WebhookDeliveryFormat.EnvelopeV1 });

    [Fact] async Task should_not_recommend_a_replay() => await _recommendations.DidNotReceiveWithAnyArgs().Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(default!, default!);
}
