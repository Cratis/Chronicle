// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_reporting_erasure;

public class with_plaintext_and_recorded_erasure : given.content_for_erasure_reporting
{
    async Task Because()
    {
        await Erase();
        _stored["profile"]["name"] = "legacy plaintext";
        _stored["scalars"][0] = string.Empty;
        _stored["count"] = 0;
        _stored["flag"] = false;
        await Release();
    }

    [Fact] void should_not_report_genuine_zero() => _result.ErasedPaths.Contains("count").ShouldBeFalse();
    [Fact] void should_not_report_genuine_false() => _result.ErasedPaths.Contains("flag").ShouldBeFalse();
    [Fact] void should_preserve_legacy_plaintext() => _result.Value["profile"]["name"].GetValue<string>().ShouldEqual("legacy plaintext");
    [Fact] void should_not_report_plaintext() => _result.ErasedPaths.Contains("profile.name").ShouldBeFalse();
    [Fact] void should_not_report_an_unencrypted_empty_string() => _result.ErasedPaths.Contains("scalars[0]").ShouldBeFalse();
}
