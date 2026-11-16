// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_a_webhook_definition_exists : given.a_pruner
{
    void Establish()
    {
        _rows.Add(Replay("webhook"));
        _store.Webhooks.Has((WebhookId)"webhook").Returns(true);
    }

    Task Because() => Prune();

    [Fact] void should_keep_the_row() => _removed.ShouldBeEmpty();
}
