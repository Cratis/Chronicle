// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.given;

public class a_listing_overlapping_a_local_write : Specification
{
    protected SharedProjectionDefinitionsStorage _shared;
    protected HeldListingStorage _inner;
    protected CachingProjectionDefinitionsStorage _storage;
    protected ProjectionId _id;
    protected ProjectionDefinition _original;
    protected ProjectionDefinition _changed;
    protected Task _listing;

    async Task Establish()
    {
        _shared = new();
        _inner = new(_shared);
        _storage = new(_inner);
        _id = "some-projection";
        _original = a_caching_projection_definitions_storage.DefinitionFor(_id);
        _changed = _original with { ReadModel = "another-read-model" };
        await _storage.Save(_original);
        _inner.HoldNextListing();
        _listing = _storage.GetAll();
        await _inner.SnapshotTaken.Task;
    }
}
