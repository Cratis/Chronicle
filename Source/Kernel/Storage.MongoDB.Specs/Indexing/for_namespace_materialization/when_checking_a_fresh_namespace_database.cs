// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_namespace_materialization;

/// <summary>
/// A namespace database that has never had anything written to it must report as having no data - connecting to
/// it, or even asking MongoDB for a collection reference, does not itself create anything on the server. Listing
/// collection names is a read, so checking it must not be the very thing that materializes the database.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_checking_a_fresh_namespace_database(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    bool _hasAnyCollections;

    async Task Because() => _hasAnyCollections = await _database.HasAnyCollections();

    [Fact] void should_report_no_collections() => _hasAnyCollections.ShouldBeFalse();
}
