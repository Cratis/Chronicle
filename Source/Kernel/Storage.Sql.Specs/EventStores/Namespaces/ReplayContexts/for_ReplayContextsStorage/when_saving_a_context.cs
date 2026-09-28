// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayContexts.for_ReplayContextsStorage;

public class when_saving_a_context : given.a_migrated_namespace_database
{
    ReplayContextsStorage _storage;
    ReplayContext _context;
    ReplayContext _retrieved;

    void Establish()
    {
        _storage = new ReplayContextsStorage(_eventStore, _namespace, _database);
        _context = new(
            new ReadModelType(new ReadModelIdentifier("some-read-model"), new ReadModelGeneration(3)),
            "some-read-model",
            "some-read-model-20260101120000",
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    }

    async Task Because()
    {
        await _storage.Save(_context);
        (await _storage.TryGet(_context.Type.Identifier)).TryPickT0(out _retrieved, out _);
    }

    [Fact] void should_get_the_saved_context_back() => _retrieved.ShouldEqual(_context);
}
