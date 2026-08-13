// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The compatibility guarantee, asserted where it matters — on the release itself. Every assertion here is
/// the one and_a_declaration_overrides_the_read_models_own_subject makes with [SubjectFrom]; a read model
/// still written against the released [ReleaseUnder] must split the release identically.
/// </summary>
public class and_the_declaration_uses_the_superseded_attribute : given.a_recording_compliance_service
{
#pragma warning disable CS0618 // the superseded attribute is the subject of this specification
    record DueSubject(string Id, string PersonId, [PII] string Advisor, [PII][ReleaseUnder(nameof(PersonId))] string Comment);
#pragma warning restore CS0618

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
