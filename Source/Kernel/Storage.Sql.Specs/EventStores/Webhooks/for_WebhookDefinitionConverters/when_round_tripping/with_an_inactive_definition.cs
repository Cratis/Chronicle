// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookDefinitionConverters.when_round_tripping;

public class with_an_inactive_definition : Specification
{
    Concepts.Observation.Webhooks.WebhookDefinition _definition;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish() => _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 1)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()), IsReplayable: false, IsActive: false);

    void Because() => _result = _definition.ToSql().ToKernel();

    [Fact] void should_keep_it_inactive() => _result.IsActive.ShouldBeFalse();
    [Fact] void should_keep_it_non_replayable() => _result.IsReplayable.ShouldBeFalse();
}
