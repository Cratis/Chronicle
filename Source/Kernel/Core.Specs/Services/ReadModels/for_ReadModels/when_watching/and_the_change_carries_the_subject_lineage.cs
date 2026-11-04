// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_the_change_carries_the_subject_lineage : a_watch_with_the_real_compliance_chain
{
    protected override string ReleasedSubject => "subject-1";

    async Task Because() => await Push(
        new JsonObject { ["id"] = Key, ["name"] = EncryptedName },
        ChangeAt(42) with { Subject = "subject-1" });

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_release_under_the_subject_of_the_change() => Streamed["name"].GetValue<string>().ShouldEqual(DecryptedName);
    [Fact] void should_not_stream_the_subject() => Streamed.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
}
