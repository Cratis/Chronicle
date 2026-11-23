// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStringCache;

public class when_reusing_normalization : given.a_cache
{
    string _first;
    string _result;

    void Establish() => _first = Normalize(_input);

    void Because() => _result = Normalize(SqlConnectionStringInput.From(Configured, "tenant", new PostgreSqlPooling()));

    [Fact] void should_reuse_the_string_for_equal_input_values() => ReferenceEquals(_first, _result).ShouldBeTrue();
}
