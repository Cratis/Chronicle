// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

/// <summary>
/// A record written before standbys were recorded has no such property and reads as having none.
/// </summary>
public class with_a_durable_subscription_written_before_standbys : Specification
{
    const string OldRecord = """{"Kind":1,"DeliversPages":false,"Filters":null,"Targets":[{"ConnectionId":"connection-1","Silo":"silo-a"}]}""";

    ObserverDurableSubscription _restored;

    void Because() => _restored = JsonSerializer.Deserialize<ObserverDurableSubscription>(OldRecord)!;

    [Fact] void should_keep_the_targets() => _restored.Targets.Select(target => target.ConnectionId).ShouldContainOnly("connection-1");
    [Fact] void should_have_no_standbys() => (_restored.Standbys ?? []).ShouldBeEmpty();
}
