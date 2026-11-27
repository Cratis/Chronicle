// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class when_rejecting_downgraded_typed_envelopes : given.a_manager
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    async Task should_reject_damaged_framing_before_classifying_erasure(int version, bool erase)
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _source = new JsonObject { ["value"] = new string('x', 65536) };
        if (erase) await ProtectAndErase();
        else _protected = await _manager.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _source);
        var bytes = Convert.FromBase64String(_protected["value"].GetValue<string>());
        bytes[4] = (byte)version;
        _protected["value"] = Convert.ToBase64String(bytes);
        var failure = await Catch.Exception(() => _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected));
        failure.ShouldBeOfExactType<CryptographicException>();
        (await _manager.TryRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected)).ShouldBeNull();
    }
}
