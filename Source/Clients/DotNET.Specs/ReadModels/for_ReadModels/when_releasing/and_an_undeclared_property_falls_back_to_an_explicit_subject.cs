// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The default half of the model, pinned against the [Subject] branch: a property that says nothing uses the
/// read model's own subject, and when that subject is declared with [Subject] it is that property — not Id,
/// which is present here precisely so a fallback to it would show.
/// </summary>
public class and_an_undeclared_property_falls_back_to_an_explicit_subject : given.a_recording_compliance_service
{
    record DueSubject(string Id, [property: Subject] string PersonId, [PII] string Advisor, [PII][SubjectFrom(nameof(Id))] string Comment);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject(
        "case-9",
        "person-1",
        Cipher("person-1", "Grace Hopper"),
        Cipher("case-9", "Awaiting counsel")));

    [Fact] void should_release_once_per_subject() => _requests.Count.ShouldEqual(2);
    [Fact] void should_send_the_undeclared_properties_under_the_explicit_subject() => PayloadKeysFor("person-1").ShouldContainOnly([nameof(DueSubject.Id), nameof(DueSubject.PersonId), nameof(DueSubject.Advisor)]);
    [Fact] void should_send_only_the_declared_property_under_the_named_one() => PayloadKeysFor("case-9").ShouldContainOnly([nameof(DueSubject.Comment)]);
    [Fact] void should_release_the_undeclared_value_under_the_explicit_subject() => _result.Advisor.ShouldEqual("Grace Hopper");
    [Fact] void should_release_the_declared_value_under_the_named_subject() => _result.Comment.ShouldEqual("Awaiting counsel");
}
