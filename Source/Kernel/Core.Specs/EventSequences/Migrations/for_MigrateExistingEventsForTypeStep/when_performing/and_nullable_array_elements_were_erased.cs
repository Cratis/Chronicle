// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_nullable_array_elements_were_erased : given.protected_events
{
    protected override string SourceSchemaJson => """
        {"type":"object","properties":{
          "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
          "numbers":{"type":"array","items":{"type":["integer","null"],"format":"int32?","compliance":[{"metadataType":"PII","details":""}]}}
        }}
        """;
    protected override string SourceContentJson => """{"name":"Jane","numbers":[42,7]}""";
    protected override string MigrationSuffix => string.Empty;

    async Task Establish()
    {
        await _keys.RecordErasureFor(Store, Namespace, "erased-owner");
        await _keys.DeleteFor(Store, Namespace, "erased-owner");
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_the_migration() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_replace_both_events() => _stored.Values.All(generations => generations.ContainsKey(2)).ShouldBeTrue();
    [Fact] void should_preserve_erased_element_positions() => ((object?[])((IDictionary<string, object?>)_plaintextEvents[0])["numbers"]!).ShouldContainOnly(null, null);
    [Fact] void should_release_active_array_elements() => ((object?[])((IDictionary<string, object?>)_plaintextEvents[1])["numbers"]!).ShouldContainOnly(42, 7);
}
