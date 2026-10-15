// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_DecimalColumnValues;

public class when_rounding_midpoints : Specification
{
    decimal _positive;
    decimal _negative;

    void Because()
    {
        _positive = DecimalColumnValues.ForSqlServer(0.0000000000000000005m);
        _negative = DecimalColumnValues.ForSqlServer(-0.0000000000000000005m);
    }

    [Fact] void should_round_positive_midpoints_away_from_zero() => _positive.ShouldEqual(0.000000000000000001m);
    [Fact] void should_round_negative_midpoints_away_from_zero() => _negative.ShouldEqual(-0.000000000000000001m);
}
