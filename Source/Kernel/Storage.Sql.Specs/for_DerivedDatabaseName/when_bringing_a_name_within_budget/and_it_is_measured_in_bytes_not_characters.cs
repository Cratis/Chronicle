// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Storage.Sql.for_DerivedDatabaseName.when_bringing_a_name_within_budget;

/// <summary>
/// The server counts bytes. A namespace named outside ASCII costs more than its Length suggests, so measuring
/// characters would let a name through that the server then truncates anyway.
/// </summary>
public class and_it_is_measured_in_bytes_not_characters : Specification
{
    static readonly string _name = $"chronicle+es+{new string('ø', 40)}";

    string _result;

    void Because() => _result = DerivedDatabaseName.WithinBudget(_name, DerivedDatabaseName.PostgreSqlMaxBytes);

    [Fact] void should_have_been_within_the_character_count() => _name.Length.ShouldBeLessThan(DerivedDatabaseName.PostgreSqlMaxBytes);
    [Fact] void should_still_have_been_shortened() => _result.ShouldNotEqual(_name);
    [Fact] void should_fit_within_the_budget() => (Encoding.UTF8.GetByteCount(_result) <= DerivedDatabaseName.PostgreSqlMaxBytes).ShouldBeTrue();
}
