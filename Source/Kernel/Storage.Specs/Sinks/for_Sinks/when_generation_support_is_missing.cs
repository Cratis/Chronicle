// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_generation_support_is_missing : Specification
{
    LegacyFactory _legacy;
    Exception? _error;

    void Establish() => _legacy = new();
    void Because() => _error = Catch.Exception(() => ((ISinkFactory)_legacy).CreateFor("store", "namespace", GenerationContexts.Create(WellKnownSinkTypes.EventSequence)));

    [Fact] void should_refuse_with_a_typed_capability_error() => _error.ShouldBeOfExactType<MaterializationBindingsNotSupported>();
    [Fact] void should_never_fall_back_to_legacy_allocation() => _legacy.Created.ShouldBeFalse();

    sealed class LegacyFactory : ISinkFactory
    {
        public bool Created { get; private set; }
        public SinkTypeId TypeId => WellKnownSinkTypes.EventSequence;
        public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
        {
            Created = true;
            return Substitute.For<ISink>();
        }
    }
}
