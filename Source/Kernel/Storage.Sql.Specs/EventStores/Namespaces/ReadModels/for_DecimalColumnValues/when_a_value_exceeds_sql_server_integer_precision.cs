// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_DecimalColumnValues;

public class when_a_value_exceeds_sql_server_integer_precision : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => DecimalColumnValues.ForSqlServer(100000000000000000000m));

    [Fact] void should_refuse_a_value_that_would_overflow_the_column() => _exception.ShouldBeOfExactType<DecimalValueExceedsColumnPrecision>();
}
