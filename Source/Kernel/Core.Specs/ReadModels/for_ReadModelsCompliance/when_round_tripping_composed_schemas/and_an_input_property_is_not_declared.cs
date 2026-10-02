// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_an_input_property_is_not_declared : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        { "type": "object", "properties": { "localSecret": {{Pii}} } }
        """;

    protected override ExpandoObject State() => CreateState(("localSecret", PersonalValue), ("undeclared", PersonalValue), ("__initialized", true), ("__lastHandledEventSequenceNumber", 42L));

    Task Because() => RoundTrip();

    [Fact] void should_preserve_undeclared_state_as_on_main() => Value(_stored, "undeclared").ShouldEqual(PersonalValue);
    [Fact] void should_preserve_undeclared_state_after_erasure() => Value(_storedAfterErasure, "undeclared").ShouldEqual(PersonalValue);
    [Fact] void should_not_restore_declared_personal_state_after_erasure() => Value(_storedAfterErasure, "localSecret").ShouldEqual(string.Empty);
    [Fact] void should_keep_the_existing_json_release_schema_drift_contract() => _jsonReleaseError.ShouldBeOfExactType<SchemaPropertyNotFoundInSchema>();
    [Fact] void should_preserve_the_kernel_initialization_flag() => Value(_stored, "__initialized").ShouldEqual(true);
    [Fact] void should_preserve_the_kernel_watermark() => Value(_stored, "__lastHandledEventSequenceNumber").ShouldEqual(42L);
}
