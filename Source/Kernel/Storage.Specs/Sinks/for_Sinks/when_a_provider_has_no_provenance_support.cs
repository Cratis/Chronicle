// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_a_provider_has_no_provenance_support : given.a_sink_factory
{
    Exception? _provisioningError;
    Exception? _lookupError;
    bool _restricted;
    UnknownSinkFactory _unknown;

    void Establish()
    {
        _unknown = new(_model.Sink.Type, _sink);
        _sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_unknown]));
    }

    async Task Because()
    {
        _restricted = await _sinks.GetProvenance(_model).IsQuarantined();
        _provisioningError = await Catch.Exception(() => _sinks.GetFor(_model));
        _lookupError = await Catch.Exception(() => _sinks.GetWithoutProvisioning(_model));
    }

    [Fact] void should_treat_unknown_database_provenance_as_restricted() => _restricted.ShouldBeTrue();
    [Fact] void should_refuse_provisioning() => _provisioningError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_refuse_unprovisioned_reads() => _lookupError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_create_a_payload_sink() => _unknown.CreateCount.ShouldEqual(0);
    [Fact] void should_not_read_or_provision_payloads() => _sink.ReceivedCalls().Any().ShouldBeFalse();

    sealed class UnknownSinkFactory(SinkTypeId type, ISink sink) : ISinkFactory
    {
        public SinkTypeId TypeId => type;

        public int CreateCount { get; private set; }

        public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
        {
            CreateCount++;
            return sink;
        }
    }
}
