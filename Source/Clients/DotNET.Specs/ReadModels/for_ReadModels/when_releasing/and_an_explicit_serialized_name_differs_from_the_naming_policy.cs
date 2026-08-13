// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

public class and_an_explicit_serialized_name_differs_from_the_naming_policy : given.a_recording_compliance_service
{
    record DueSubject(
        string PersonId,
        [property: JsonPropertyName("PrivateNote")][PII][SubjectFrom(nameof(PersonId))] string Comment);

    DueSubject _result;

    void Establish()
    {
        _jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper;
        _jsonSerializerOptions.PropertyNameCaseInsensitive = true;
    }

    async Task Because() => _result = await _readModels.Release(new DueSubject(
        "person-1",
        Cipher("person-1", "Awaiting counsel")));

    [Fact] void should_prefer_the_case_sensitive_explicit_name_over_the_naming_policy() => PayloadKeysFor("person-1").ShouldContainOnly(["PrivateNote"]);
    [Fact] void should_release_the_declared_value() => _result.Comment.ShouldEqual("Awaiting counsel");
}
