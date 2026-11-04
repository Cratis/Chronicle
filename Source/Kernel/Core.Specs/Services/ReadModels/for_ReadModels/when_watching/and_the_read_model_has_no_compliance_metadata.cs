// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_the_read_model_has_no_compliance_metadata : a_watch_with_the_real_compliance_chain
{
    protected override bool HasPersonalData => false;

    async Task Because() => await Push(new JsonObject { ["id"] = Key, ["name"] = "plain-name" }, ChangeAt(42));

    [Fact] void should_keep_the_sequence_number_on_the_streamed_document() => Streamed[WellKnownProperties.LastHandledEventSequenceNumber].GetValue<ulong>().ShouldEqual(42UL);
    [Fact] void should_not_stream_the_subject() => Streamed.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
    [Fact] void should_not_stream_the_subjects() => Streamed.ContainsKey(WellKnownProperties.Subjects).ShouldBeFalse();
}
