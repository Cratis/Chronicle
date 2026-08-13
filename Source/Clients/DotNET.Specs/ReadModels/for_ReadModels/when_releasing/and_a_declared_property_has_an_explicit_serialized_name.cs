// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

public class and_a_declared_property_has_an_explicit_serialized_name : given.a_recording_compliance_service
{
    record DueSubject(
        string PersonId,
        [property: JsonPropertyName("private_note")][PII][SubjectFrom(nameof(PersonId))] string Comment);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject(
        "person-1",
        Cipher("person-1", "Awaiting counsel")));

    [Fact] void should_send_the_property_under_its_explicit_serialized_name() => PayloadKeysFor("person-1").ShouldContainOnly(["private_note"]);
    [Fact] void should_release_the_declared_value() => _result.Comment.ShouldEqual("Awaiting counsel");
}
