// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_DecimalColumnValues;

public class when_a_value_exceeds_sql_server_scale : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => DecimalColumnValues.ForSqlServer(0.0000000000000000001m));

    [Fact] void should_refuse_silent_rounding() => _exception.ShouldBeOfExactType<DecimalValueExceedsColumnPrecision>();
}
