// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_an_input_property_is_not_declared : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        { "type": "object", "properties": { "localSecret": {{Pii}} } }
        """;

    protected override ExpandoObject State() => CreateState(("localSecret", PersonalValue), ("undeclared", PersonalValue), ("__initialized", true), ("__lastHandledEventSequenceNumber", 42L));

    Task Because() => RoundTrip();

    [Fact] void should_not_restore_a_value_with_unknown_protection() => ((IDictionary<string, object?>)_stored).ContainsKey("undeclared").ShouldBeFalse();
    [Fact] void should_not_store_unknown_plaintext_after_erasure() => ContainsPersonalValue(_storedAfterErasure).ShouldBeFalse();
    [Fact] void should_release_json_without_undeclared_application_state() => _jsonReleaseError.ShouldBeNull();
    [Fact] void should_preserve_the_kernel_initialization_flag() => Value(_stored, "__initialized").ShouldEqual(true);
    [Fact] void should_preserve_the_kernel_watermark() => Value(_stored, "__lastHandledEventSequenceNumber").ShouldEqual(42L);
}
