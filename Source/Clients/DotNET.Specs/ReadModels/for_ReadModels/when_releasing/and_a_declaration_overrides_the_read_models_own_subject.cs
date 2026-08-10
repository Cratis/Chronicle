// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The other outcome the report names: the composed row does resolve a subject, just not the one the lifted
/// value belongs to, so the value is released under the wrong key. Declaring the value's own subject splits
/// the release in two — the declared value under its owner, everything else under the row's own subject,
/// which must keep working exactly as before.
/// </summary>
public class and_a_declaration_overrides_the_read_models_own_subject : given.a_recording_compliance_service
{
    record DueSubject(string Id, string PersonId, [PII] string Advisor, [PII][ReleaseUnder(nameof(PersonId))] string Comment);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject(
        "case-9",
        "person-1",
        Cipher("case-9", "Grace Hopper"),
        Cipher("person-1", "Awaiting counsel")));

    [Fact] void should_release_once_per_subject() => _requests.Count.ShouldEqual(2);
    [Fact] void should_send_only_the_declared_property_to_its_owner() => PayloadKeysFor("person-1").ShouldContainOnly([nameof(DueSubject.Comment)]);
    [Fact] void should_send_everything_else_under_the_rows_own_subject() => PayloadKeysFor("case-9").ShouldContainOnly([nameof(DueSubject.Id), nameof(DueSubject.PersonId), nameof(DueSubject.Advisor)]);
    [Fact] void should_release_the_declared_value_under_its_owner() => _result.Comment.ShouldEqual("Awaiting counsel");
    [Fact] void should_release_the_undeclared_value_under_the_rows_own_subject() => _result.Advisor.ShouldEqual("Grace Hopper");
}
