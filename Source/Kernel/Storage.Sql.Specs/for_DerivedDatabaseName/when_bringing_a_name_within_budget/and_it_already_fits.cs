// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_DerivedDatabaseName.when_bringing_a_name_within_budget;

public class and_it_already_fits : Specification
{
    const string Name = "chronicle+es+Testing+Default";

    string _result;

    void Because() => _result = DerivedDatabaseName.WithinBudget(Name, DerivedDatabaseName.PostgreSqlMaxBytes);

    [Fact] void should_leave_it_alone() => _result.ShouldEqual(Name);
}
