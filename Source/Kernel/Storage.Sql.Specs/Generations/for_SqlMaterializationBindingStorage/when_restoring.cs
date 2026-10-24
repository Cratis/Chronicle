// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_SqlMaterializationBindingStorage;

public class when_restoring : Specification
{
    Exception _error;
    void Because()
    {
        var storage = new SqlMaterializationBindingStorage();
        var key = new MaterializationKey(StoreIncarnation.Unspecified, "ns", "projection", SinkTypeId.None, SinkConfigurationId.None);
        _error = Catch.Exception(() => storage.TryRestore("store", key, MaterializationGeneration.First, ServingEpoch.Initial, new(2), TimeSpan.FromHours(1), new(Guid.NewGuid())).GetAwaiter().GetResult());
    }
    [Fact] void should_refuse_without_io() => _error.ShouldBeOfExactType<MaterializationBindingsNotSupported>();
}
