// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_the_change_has_no_actual_sequence_number : a_watch_with_the_real_compliance_chain
{
    protected override bool HasPersonalData => false;

    async Task Because() => await Push(new JsonObject { ["id"] = Key, ["name"] = "plain-name" }, ChangeAt(ulong.MaxValue));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_not_stamp_a_sentinel_watermark() => Streamed.ContainsKey(WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeFalse();
}
