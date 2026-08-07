// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The composed row the report is about: a query method builds it from rows it fetched itself, its identity
/// is deliberately not named <c>Id</c>, and it lifts a value encrypted under the person's own subject. With
/// nothing declared the release pass never runs and the ciphertext travels to the caller. Declaring the
/// subject is what makes it run.
/// </summary>
public class and_no_subject_resolves_but_one_is_declared : given.a_recording_compliance_service
{
    record DueSubject(string SubjectId, [PII][ReleaseUnder(nameof(SubjectId))] string Comment);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject("person-1", Cipher("person-1", "Awaiting counsel")));

    [Fact] void should_release_once() => _requests.Count.ShouldEqual(1);
    [Fact] void should_release_under_the_declared_subject() => _requests[0].Subject.ShouldEqual("person-1");
    [Fact] void should_send_only_the_declared_property() => PayloadKeysFor("person-1").ShouldContainOnly([nameof(DueSubject.Comment)]);
    [Fact] void should_return_the_released_value() => _result.Comment.ShouldEqual("Awaiting counsel");
    [Fact] void should_keep_the_subject_property() => _result.SubjectId.ShouldEqual("person-1");
}
