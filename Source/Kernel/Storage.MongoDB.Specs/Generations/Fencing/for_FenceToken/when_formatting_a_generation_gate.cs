// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FenceToken;

public class when_formatting_a_generation_gate : Specification
{
    readonly MaterializationKey _key = new(new(Guid.Parse("fa5dc415-e617-48a0-9975-fd1580aa321a")), "one", "projection", "mongodb", SinkConfigurationId.None);

    [Fact] void should_match_the_binding_gate_identity() =>
        new FenceToken(_key.Value, 23, 7).GateId.ShouldEqual(new GenerationGateId(_key, new(23)).Value);

    [Fact] void should_preserve_the_sink_key_and_unsigned_generation_range() =>
        new FenceToken("store/namespace/read-model", ulong.MaxValue, 0).GateId.ShouldEqual("store/namespace/read-model#18446744073709551615");

    [Fact] void should_preserve_collection_names() => FenceCollections.Gates.ShouldEqual("fence-gates");
    [Fact] void should_preserve_epoch_field_names() => FenceCollections.Epoch.ShouldEqual("epoch");
    [Fact] void should_preserve_admission_field_names() => FenceCollections.Admitted.ShouldEqual("admitted");
}
