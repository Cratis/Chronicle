// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_DecimalColumnValues;

public class when_a_value_exceeds_sql_server_scale : Specification
{
    decimal _result;

    void Because() => _result = DecimalColumnValues.ForSqlServer(1m / 3m);

    [Fact] void should_round_to_the_column_scale() => _result.ShouldEqual(0.333333333333333333m);
}
