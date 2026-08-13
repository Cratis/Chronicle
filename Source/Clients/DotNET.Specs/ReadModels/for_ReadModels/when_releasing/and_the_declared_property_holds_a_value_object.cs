// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// A declaration is written on the read model's own property, and everything that property holds travels
/// with it — including the personal values nested inside a value object, which are what the compliance walk
/// actually reaches.
/// </summary>
public class and_the_declared_property_holds_a_value_object : given.a_recording_compliance_service
{
    record Postponement([PII] string Comment, string RecordedBy);

    record DueSubject(string PersonId, [SubjectFrom(nameof(PersonId))] Postponement Postponement);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject(
        "person-1",
        new Postponement(Cipher("person-1", "Awaiting counsel"), "advisor-3")));

    [Fact] void should_release_once() => _requests.Count.ShouldEqual(1);
    [Fact] void should_release_under_the_declared_subject() => _requests[0].Subject.ShouldEqual("person-1");
    [Fact] void should_send_the_whole_value_object() => PayloadKeysFor("person-1").ShouldContainOnly([nameof(DueSubject.Postponement)]);
    [Fact] void should_keep_the_value_object_shape() => _result.Postponement.RecordedBy.ShouldEqual("advisor-3");
}
