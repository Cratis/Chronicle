// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_checking_flat_key_support : given.named_sinks
{
    void Establish() => _defaultFactory.SupportsFlatCompositeKeys.Returns(true);

    [Fact]
    void should_resolve_the_effective_named_factory_not_the_declared_type()
    {
        _sinks.SupportsFlatCompositeKeys(ReadModel(new SinkName("reporting"))).ShouldBeFalse();
        _sinks.SupportsFlatCompositeKeys(ReadModel(SinkName.Default)).ShouldBeTrue();
        _defaultFactory.DidNotReceive().CreateFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>());
        _namedFactory.DidNotReceive().CreateFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>(), Arg.Any<SinkName>());
    }

    [Fact]
    void should_leave_existing_factories_unsupported_by_default() => ((ISinkFactory)new ExistingFactory()).SupportsFlatCompositeKeys.ShouldBeFalse();

    sealed class ExistingFactory : ISinkFactory
    {
        public SinkTypeId TypeId => "SQL";

        public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) => throw new InvalidOperationException("Capability checks must not create a sink.");
    }
}
