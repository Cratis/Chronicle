// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.when_recording;

public class and_the_ingress_is_replayed : given.an_occurrence_storage
{
    OccurrenceRecordResult _result;
    async Task Establish() => await _storage.Record(_occurrence);
    async Task Because() => _result = await Recreate().Record(_occurrence);
    [Fact] void should_report_the_existing_occurrence() => _result.ShouldEqual(OccurrenceRecordResult.AlreadyRecorded);
}
