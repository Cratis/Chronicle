// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_SinkProvenanceExemptions;

public class when_resolving_non_database_sinks
{
    [Fact]
    void should_exempt_only_null_and_event_publication_sinks()
    {
        SinkProvenanceExemptions.IsNonDatabase(SinkTypeId.None).ShouldBeTrue();
        SinkProvenanceExemptions.IsNonDatabase(WellKnownSinkTypes.EventSequence).ShouldBeTrue();
        SinkProvenanceExemptions.IsNonDatabase(WellKnownSinkTypes.InMemory).ShouldBeFalse();
        SinkProvenanceExemptions.IsNonDatabase(WellKnownSinkTypes.MongoDB).ShouldBeFalse();
        SinkProvenanceExemptions.IsNonDatabase(WellKnownSinkTypes.SQL).ShouldBeFalse();
        SinkProvenanceExemptions.IsNonDatabase("unknown").ShouldBeFalse();
    }

    [Fact]
    async Task should_not_require_a_database_marker_for_null_or_event_publication_sinks()
    {
        ISinkFactory preview = new NonDatabaseSinkFactory(SinkTypeId.None);
        ISinkFactory publication = new NonDatabaseSinkFactory(WellKnownSinkTypes.EventSequence);
        (await preview.GetProvenance("store", "namespace", SinkName.Default).IsQuarantined()).ShouldBeFalse();
        (await publication.GetProvenance("store", "namespace", SinkName.Default).IsQuarantined()).ShouldBeFalse();
    }

    [Fact] void should_not_admit_event_publications_to_public_read_model_reads() => SinkProvenanceExemptions.AllowsPublicReads(WellKnownSinkTypes.EventSequence).ShouldBeFalse();

    sealed class NonDatabaseSinkFactory(SinkTypeId type) : ISinkFactory
    {
        public SinkTypeId TypeId => type;

        public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) => new NullSink();
    }
}
