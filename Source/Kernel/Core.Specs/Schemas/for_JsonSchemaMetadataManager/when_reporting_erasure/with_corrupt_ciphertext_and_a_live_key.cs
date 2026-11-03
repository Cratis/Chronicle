// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_reporting_erasure;

public class with_corrupt_ciphertext_and_a_live_key : given.content_for_erasure_reporting
{
    async Task Because()
    {
        await _keys.RecordErasureFor("store", "tenant", "subject");
        var bytes = Convert.FromBase64String(_stored["profile"]["name"].GetValue<string>());
        bytes[^1] ^= 1;
        _stored["profile"]["name"] = Convert.ToBase64String(bytes);
        await Release();
    }

    [Fact] void should_keep_the_display_fallback() => _result.Value["profile"]["name"].GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_not_turn_a_decryption_failure_into_erasure() => _result.ErasedPaths.ShouldBeEmpty();
    [Fact] void should_still_release_other_properties() => _result.Value["contacts"][0]["email"].GetValue<string>().ShouldEqual("one");
}
