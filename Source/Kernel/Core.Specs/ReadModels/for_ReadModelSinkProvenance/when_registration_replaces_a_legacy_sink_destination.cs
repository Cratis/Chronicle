// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_registration_replaces_a_legacy_sink_destination : given.a_provenance_inventory
{
    readonly SinkConfigurationId _replacement = Guid.NewGuid();
    Exception? _error;
    bool _oldRestricted;
    bool _newRestricted;

    async Task Because()
    {
        _projections = [_projection with { EventSequenceId = "system" }];
        await _provenance.Preserve(Store);
        _projections = [];
        _model = _model with { Sink = _model.Sink with { Configuration = _replacement } };
        _models = [_model];
        _provenance = new(_storage);
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        _oldRestricted = await Marker(EventStoreNamespaceName.Default).IsQuarantined();
        _newRestricted = await Marker(EventStoreNamespaceName.Default, _replacement).IsQuarantined();
    }

    [Fact] void should_allow_the_independent_replacement_database() => _error.ShouldBeNull();
    [Fact] void should_not_replay_orphan_evidence_into_the_replacement() => _newRestricted.ShouldBeFalse();
    [Fact] void should_retain_the_original_database_restriction() => _oldRestricted.ShouldBeTrue();
}
