// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_a_projection_is_removed_after_inventory_and_the_silo_restarts : given.a_provenance_inventory
{
    Exception _error;
    bool _quarantined;

    async Task Because()
    {
        await _provenance.Preserve(Store);
        _projections = [];
        _provenance = new(_storage);
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, Concepts.EventStoreNamespaceName.Default, _model));
        _quarantined = await Marker(Concepts.EventStoreNamespaceName.Default).IsQuarantined();
    }

    [Fact] void should_keep_the_retained_read_model_eligible() => _error.ShouldBeNull();
    [Fact] void should_not_turn_removal_into_restricted_evidence() => _quarantined.ShouldBeFalse();
}
