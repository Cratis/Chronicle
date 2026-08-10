// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The multi-subject composed row. A read model releases under one subject, so before declarations existed
/// at most one of these two values could ever come back — this shape was not expressible at all.
/// </summary>
public class and_two_values_belong_to_different_subjects : given.a_recording_compliance_service
{
    record Duplicate(
        string ContactPointHash,
        string FirstPersonId,
        string SecondPersonId,
        [PII][ReleaseUnder(nameof(FirstPersonId))] string FirstName,
        [PII][ReleaseUnder(nameof(SecondPersonId))] string SecondName);

    Duplicate _result;

    async Task Because() => _result = await _readModels.Release(new Duplicate(
        "hash-1",
        "person-1",
        "person-2",
        Cipher("person-1", "Erik Hansen"),
        Cipher("person-2", "Erik Hanssen")));

    [Fact] void should_release_once_per_person() => _requests.Count.ShouldEqual(2);
    [Fact] void should_send_the_first_name_only_to_the_first_person() => PayloadKeysFor("person-1").ShouldContainOnly([nameof(Duplicate.FirstName)]);
    [Fact] void should_send_the_second_name_only_to_the_second_person() => PayloadKeysFor("person-2").ShouldContainOnly([nameof(Duplicate.SecondName)]);
    [Fact] void should_release_the_first_name() => _result.FirstName.ShouldEqual("Erik Hansen");
    [Fact] void should_release_the_second_name() => _result.SecondName.ShouldEqual("Erik Hanssen");
}
