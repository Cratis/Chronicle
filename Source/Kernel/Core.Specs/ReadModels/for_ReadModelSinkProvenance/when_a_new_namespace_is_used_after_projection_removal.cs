// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_a_new_namespace_is_used_after_projection_removal : given.a_provenance_inventory
{
    Exception? _error;
    bool _marked;
    bool _complete;

    async Task Because()
    {
        await _provenance.Preserve(Store);
        _projections = [];
        _provenance = new(_storage);
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, "new-namespace", _model));
        var state = await Marker("new-namespace").ReadState();
        _marked = state.Restricted;
        _complete = state.IsComplete;
    }

    [Fact] void should_allow_the_first_read_in_the_new_namespace() => _error.ShouldBeNull();
    [Fact] void should_not_mark_the_new_database() => _marked.ShouldBeFalse();
    [Fact] void should_complete_the_new_database_inventory() => _complete.ShouldBeTrue();
}
