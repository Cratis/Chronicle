// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_DerivedDatabaseName.when_bringing_a_name_within_budget;

/// <summary>
/// This is the leak. PostgreSQL truncates an over-long identifier silently, so two event stores - or one event
/// store and two namespaces - whose names agreed up to 63 bytes resolved to a single physical database and
/// shared an event log, with no error anywhere to say so (#4137).
/// </summary>
public class and_two_names_share_a_truncation_prefix : Specification
{
    static readonly string _prefix = $"chronicle+es+{new string('a', 60)}";

    string _first;
    string _second;

    void Because()
    {
        _first = DerivedDatabaseName.WithinBudget($"{_prefix}+TenantOne", DerivedDatabaseName.PostgreSqlMaxBytes);
        _second = DerivedDatabaseName.WithinBudget($"{_prefix}+TenantTwo", DerivedDatabaseName.PostgreSqlMaxBytes);
    }

    [Fact] void should_keep_them_apart() => _first.ShouldNotEqual(_second);
}
