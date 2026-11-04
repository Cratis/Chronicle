// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_the_read_model_has_pii_without_a_declared_watermark : a_watch_with_the_real_compliance_chain
{
    async Task Because() => await Push(new JsonObject { ["id"] = Key, ["name"] = EncryptedName }, ChangeAt(42));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_stream_the_decrypted_read_model() => Streamed["name"].GetValue<string>().ShouldEqual(DecryptedName);
    [Fact] void should_keep_the_sequence_number_on_the_streamed_document() => Streamed[WellKnownProperties.LastHandledEventSequenceNumber].GetValue<ulong>().ShouldEqual(42UL);
}
