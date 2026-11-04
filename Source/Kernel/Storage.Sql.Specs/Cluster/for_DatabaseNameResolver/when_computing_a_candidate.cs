// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_DatabaseNameResolver;

public class when_computing_a_candidate : Specification
{
    DatabaseNameResolver _resolver;
    string _fits;
    string _tooLong;

    void Establish() => _resolver = new("Server=SQL1;Initial Catalog=cluster;");

    async Task Because()
    {
        _fits = await _resolver.Candidate(DatabaseIdentity.Store("orders"));
        _tooLong = await _resolver.Candidate(DatabaseIdentity.Store(new string('s', 150)));
    }

    [Fact] void should_use_the_logical_name_when_it_fits() => _fits.ShouldEqual("cluster+es+orders");
    [Fact] void should_hash_the_name_when_it_does_not_fit() => _tooLong.ShouldEqual(DatabaseName.Hashed($"cluster+es+{new string('s', 150)}", Cratis.Arc.EntityFrameworkCore.DatabaseType.SqlServer));
}
