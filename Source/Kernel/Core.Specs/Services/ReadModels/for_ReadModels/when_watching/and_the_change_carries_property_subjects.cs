// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_the_change_carries_property_subjects : a_watch_with_the_real_compliance_chain
{
    protected override string ReleasedSubject => "property-owner";

    async Task Because() => await Push(
        new JsonObject
        {
            ["id"] = Key,
            ["name"] = EncryptedName,
            [WellKnownProperties.Subject] = "legacy-owner",
            [WellKnownProperties.Subjects] = new JsonObject { ["name"] = "legacy-property-owner" }
        },
        ChangeAt(42) with
        {
            Subject = "default-owner",
            Subjects = new Dictionary<string, string> { ["name"] = ReleasedSubject }
        });

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_use_the_property_subject_from_the_change() => Streamed["name"].GetValue<string>().ShouldEqual(DecryptedName);
    [Fact] void should_not_stream_the_default_subject() => Streamed.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
    [Fact] void should_not_stream_the_property_subjects() => Streamed.ContainsKey(WellKnownProperties.Subjects).ShouldBeFalse();
}
