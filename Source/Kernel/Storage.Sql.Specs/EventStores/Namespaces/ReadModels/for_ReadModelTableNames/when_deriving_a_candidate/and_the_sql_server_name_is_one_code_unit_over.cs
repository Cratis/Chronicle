// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_the_sql_server_name_is_one_code_unit_over : Specification
{
    string _result;

    void Because() => _result = ReadModelTableNames.Candidate(DatabaseType.SqlServer, new string('x', 129));

    [Fact] void should_fit_exactly() => _result.Length.ShouldEqual(128);
    [Fact] void should_keep_the_readable_prefix() => _result[..111].ShouldEqual(new string('x', 111));
    [Fact] void should_separate_the_hash() => _result[111].ShouldEqual('~');
    [Fact] void should_have_a_lowercase_hex_digest() => _result[112..].All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f').ShouldBeTrue();
}
