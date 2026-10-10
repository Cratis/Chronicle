// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_DecimalColumnValues;

public class when_a_value_fits_sql_server_precision : Specification
{
    decimal _result;

    void Because() => _result = DecimalColumnValues.FromSqlServer(DecimalColumnValues.ForSqlServer(193.580000000000000000m));

    [Fact] void should_remove_only_redundant_scale() => decimal.GetBits(_result).ShouldEqual(decimal.GetBits(193.58m));
}
