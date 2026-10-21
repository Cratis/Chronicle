// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_registering_a_definition;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_commit_acknowledgment_is_lost(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    Exception _error;
    ScheduleDefinition _stored;
    async Task Because()
    {
        var definition = new ScheduleDefinition(_token, 1, "first", "{}", _now, false);
        _error = await Catch.Exception(() => new given.CommitThenFailOccurrencesStorage(_storage, _schedules).TryAddDefinition(definition));
        _stored = (await RecreateSchedules().GetDefinition(_token))!;
    }
    [Fact] void should_surface_acknowledgment_loss() => _error.ShouldBeOfExactType<given.AcknowledgmentLost>();
    [Fact] void should_keep_effective_from_with_the_definition() => _stored.EffectiveFrom.ShouldEqual(DateTimeOffset.UnixEpoch.AddDays(1));
}
