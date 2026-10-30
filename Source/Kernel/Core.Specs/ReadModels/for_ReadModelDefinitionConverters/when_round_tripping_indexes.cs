// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Services.ReadModels;
using ProtoBuf;

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitionConverters;

public class when_round_tripping_indexes : Specification
{
    ReadModelDefinition _original;
    ReadModelDefinition _roundTripped;
    ReadModelDefinition _legacy;
    given.LegacyIndexDefinition _older;
    Contracts.ReadModels.ReadModelDefinition _contract;

    void Establish()
    {
        _original = new("model", "models", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() }, [new("name"), new("created") { Properties = [new("created", true)] }, new("name") { Properties = [new("name"), new("details.total", true)] }]);
    }

    async Task Because()
    {
        _contract = Serializer.DeepClone(_original.ToContract());
        _roundTripped = _contract.ToChronicle(Contracts.ReadModels.ReadModelOwner.Client, Contracts.ReadModels.ReadModelSource.Code);
        await using var stream = new MemoryStream();
        Serializer.Serialize(stream, _contract.Indexes[2]);
        stream.Position = 0;
        _older = Serializer.Deserialize<given.LegacyIndexDefinition>(stream);
        var legacy = _original.ToContract();
        legacy.Indexes = [new() { PropertyPath = "legacy" }];
        _legacy = legacy.ToChronicle(Contracts.ReadModels.ReadModelOwner.Client, Contracts.ReadModels.ReadModelSource.Code);
    }

    [Fact] void should_round_trip_every_property_and_direction_over_the_wire() => _roundTripped.Indexes.ShouldEqual(_original.Indexes);
    [Fact] void should_keep_the_leading_path_for_older_kernels() => _contract.Indexes.Select(index => index.PropertyPath).SequenceEqual(["name", "created", "name"]).ShouldBeTrue();
    [Fact] void should_let_an_older_kernel_ignore_additive_fields() => _older.PropertyPath.ShouldEqual("name");
    [Fact] void should_read_a_legacy_contract_as_ascending() => _legacy.Indexes.SequenceEqual([new IndexDefinition("legacy")]).ShouldBeTrue();
}
