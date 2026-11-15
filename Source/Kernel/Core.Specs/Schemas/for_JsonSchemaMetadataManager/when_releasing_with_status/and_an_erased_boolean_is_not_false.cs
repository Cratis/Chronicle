// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_an_erased_boolean_is_not_false : given.a_manager
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"flag":{"type":"boolean","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _source = new() { ["flag"] = true };
        await ProtectAndErase();
    }

    async Task Because() => _result = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected);

    [Fact] void should_report_erased() => _result.Statuses["flag"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_retain_ciphertext_rather_than_false() => _result.Content["flag"]!.GetValue<string>().ShouldEqual(_protected["flag"]!.GetValue<string>());
}
