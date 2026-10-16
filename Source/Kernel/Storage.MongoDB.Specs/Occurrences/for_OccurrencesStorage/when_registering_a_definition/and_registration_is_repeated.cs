// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_registering_a_definition;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_registration_is_repeated(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    ScheduleDefinition _definition;
    ScheduleDefinition _stored;
    bool _result;
    async Task Establish()
    {
        _definition = new(_token, 1, "first", "{}", _now, false);
        await _schedules.TryAddDefinition(_definition);
    }
    async Task Because()
    {
        _result = await RecreateSchedules().TryAddDefinition(_definition with { EffectiveFrom = _now.AddDays(3) });
        _stored = (await _schedules.GetDefinition(_token))!;
    }
    [Fact] void should_not_overwrite_registration() => _result.ShouldBeFalse();
    [Fact] void should_keep_the_durable_effective_from() => _stored.EffectiveFrom.ShouldEqual(DateTimeOffset.UnixEpoch.AddDays(1));
}
