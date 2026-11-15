// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_reporting_erasure;

public class with_missing_keys_without_erasure : given.content_for_erasure_reporting
{
    async Task Because()
    {
        _materialMissing = true;
        await Release();
    }

    [Fact] void should_not_infer_erasure_from_unreadable_content() => _result.ErasedPaths.ShouldBeEmpty();
    [Fact] void should_preserve_pii_fallbacks() => _result.Value["profile"]["name"].GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_confidentiality_fallbacks() => _result.Value["secret"].GetValue<string>().ShouldEqual(string.Empty);
}
