// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

/// <summary>
/// A silo of the previous release sends no subject on the change; the subject and an undeclared watermark are on the document.
/// </summary>
public class and_the_change_comes_from_a_previous_release : a_watch_with_the_real_compliance_chain
{
    protected override string ReleasedSubject => "subject-1";

    async Task Because() => await Push(
        new JsonObject
        {
            ["id"] = Key,
            ["name"] = EncryptedName,
            [WellKnownProperties.Subject] = "subject-1",
            [WellKnownProperties.LastHandledEventSequenceNumber] = 42UL
        },
        ChangeAt(42));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_stream_the_decrypted_read_model() => Streamed["name"].GetValue<string>().ShouldEqual(DecryptedName);
    [Fact] void should_not_stream_the_subject() => Streamed.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
}
