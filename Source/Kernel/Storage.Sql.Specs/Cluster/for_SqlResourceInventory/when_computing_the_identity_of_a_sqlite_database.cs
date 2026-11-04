// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Connections;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceInventory;

public class when_computing_the_identity_of_a_sqlite_database : Specification
{
    static readonly IReadOnlyDictionary<string, string> _none = new Dictionary<string, string>();

    string _lower;
    string _upper;
    string _composed;
    string _decomposed;
    string _relative;
    string _absolute;

    void Because()
    {
        _lower = SqlResourceInventory.IdentityOf(Target("/data/chronicle/orders.db"), _none);
        _upper = SqlResourceInventory.IdentityOf(Target("/DATA/Chronicle/ORDERS.db"), _none);
        _composed = SqlResourceInventory.IdentityOf(Target("/data/caf\u00e9.db"), _none);
        _decomposed = SqlResourceInventory.IdentityOf(Target("/data/cafe\u0301.db"), _none);
        _relative = SqlResourceInventory.IdentityOf(Target("data/orders.db"), _none);
        _absolute = SqlResourceInventory.IdentityOf(Target(Path.GetFullPath("data/orders.db")), _none);
    }

    [Fact] void should_ignore_case() => _upper.ShouldEqual(_lower);
    [Fact] void should_ignore_the_form_of_unicode_normalization() => _decomposed.ShouldEqual(_composed);
    [Fact] void should_use_the_full_path() => _relative.ShouldEqual(_absolute);

    static SqlTarget Target(string path) => new($"Data Source={path}", string.Empty, null);
}
