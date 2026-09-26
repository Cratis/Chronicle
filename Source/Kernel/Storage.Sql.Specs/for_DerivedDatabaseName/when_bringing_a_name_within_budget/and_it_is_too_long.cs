// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Storage.Sql.for_DerivedDatabaseName.when_bringing_a_name_within_budget;

public class and_it_is_too_long : Specification
{
    static readonly string _name = $"chronicle+es+{new string('a', 60)}+Default";

    string _result;

    void Because() => _result = DerivedDatabaseName.WithinBudget(_name, DerivedDatabaseName.PostgreSqlMaxBytes);

    [Fact] void should_fit_within_the_budget() => (Encoding.UTF8.GetByteCount(_result) <= DerivedDatabaseName.PostgreSqlMaxBytes).ShouldBeTrue();
    [Fact] void should_keep_a_recognizable_prefix() => _result.StartsWith("chronicle+es+", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_be_the_original() => _result.ShouldNotEqual(_name);
}
