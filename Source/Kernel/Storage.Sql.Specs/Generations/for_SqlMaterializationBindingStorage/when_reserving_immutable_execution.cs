// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_SqlMaterializationBindingStorage;

public class when_reserving_immutable_execution : Specification
{
    Exception _error;
    void Because()
    {
        var storage = new SqlMaterializationBindingStorage();
        var key = new MaterializationKey(StoreIncarnation.Unspecified, "ns", "projection", SinkTypeId.None, SinkConfigurationId.None);
        var execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{}");
        _error = Catch.Exception(() => storage.ReserveNext("store", key, execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch).GetAwaiter().GetResult());
    }
    [Fact] void should_refuse_before_any_io() => _error.ShouldBeOfExactType<MaterializationBindingsNotSupported>();
}
