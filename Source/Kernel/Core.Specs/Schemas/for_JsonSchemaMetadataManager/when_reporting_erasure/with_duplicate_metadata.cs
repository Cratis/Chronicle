// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_reporting_erasure;

public class with_duplicate_metadata : given.content_for_erasure_reporting
{
    async Task Because()
    {
        await Erase();
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"profile":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""},{"metadataType":"PII","details":""}]}}}}}
            """);
        _stored = new System.Text.Json.Nodes.JsonObject { ["profile"] = _stored["profile"].DeepClone() };
        await Release();
    }

    [Fact] void should_report_each_path_only_once() => _result.ErasedPaths.ShouldEqual<IEnumerable<string>>(["profile.name"]);
}
