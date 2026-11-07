// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNameCasing.when_resolving;

/// <summary>
/// A name the server already has in the same casing is used as it is.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class an_exact_match(MongoDBFixture fixture) : given.a_server_with_a_database(fixture)
{
    string _resolved;

    void Because() => _resolved = DatabaseNameCasing.Resolve(_client, ExistingName);

    [Fact] void should_resolve_to_the_name_as_it_is() => _resolved.ShouldEqual(ExistingName);
}
