// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_inventory_has_no_evidence : given.a_provenance_inventory
{
    Exception? _error;
    bool _marked;

    async Task Because()
    {
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        _marked = await Marker(EventStoreNamespaceName.Default).IsQuarantined();
    }

    [Fact] void should_allow_the_existing_ordinary_database() => _error.ShouldBeNull();
    [Fact] void should_not_persist_a_mark_for_a_clean_inventory() => _marked.ShouldBeFalse();
}
