// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.when_recording;

public class and_the_identity_is_replayed : Specification
{
    readonly OccurrencesStorage _storage = new();
    readonly Occurrence _occurrence = new("identity", "trigger", OccurrenceKind.Application, "fingerprint", "{}", 1, DateTimeOffset.UnixEpoch, null, null, OccurrenceState.Pending, 0, 0, null, null, null, null);
    OccurrenceRecordResult _result;

    async Task Establish() => await _storage.Record(_occurrence);
    async Task Because() => _result = await _storage.Record(_occurrence);

    [Fact] void should_report_the_existing_identity() => _result.ShouldEqual(OccurrenceRecordResult.AlreadyRecorded);
}
