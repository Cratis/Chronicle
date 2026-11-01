// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_the_postgresql_name_is_one_byte_over : Specification
{
    string _result;

    void Because() => _result = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, new string('x', 64));

    [Fact] void should_fit_exactly() => Encoding.UTF8.GetByteCount(_result).ShouldEqual(63);
    [Fact] void should_keep_the_readable_prefix_and_hash() => _result.ShouldEqual(new string('x', 46) + "~7ce100971f64e700");
}
