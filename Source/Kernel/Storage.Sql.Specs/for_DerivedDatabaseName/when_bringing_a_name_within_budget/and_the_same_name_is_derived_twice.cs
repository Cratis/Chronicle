// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_DerivedDatabaseName.when_bringing_a_name_within_budget;

/// <summary>
/// The physical name has to survive a restart, so the hash cannot come from string.GetHashCode() - that is
/// randomized per process, and a database nobody can find again is worse than one that is merely long.
/// </summary>
public class and_the_same_name_is_derived_twice : Specification
{
    static readonly string _name = $"chronicle+es+{new string('a', 60)}+Default";

    string _first;
    string _second;

    void Because()
    {
        _first = DerivedDatabaseName.WithinBudget(_name, DerivedDatabaseName.PostgreSqlMaxBytes);
        _second = DerivedDatabaseName.WithinBudget(_name, DerivedDatabaseName.PostgreSqlMaxBytes);
    }

    [Fact] void should_be_stable() => _first.ShouldEqual(_second);
    [Fact] void should_match_a_known_value() => _first.ShouldEqual("chronicle+es+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa+b2d4c2b6");
}
