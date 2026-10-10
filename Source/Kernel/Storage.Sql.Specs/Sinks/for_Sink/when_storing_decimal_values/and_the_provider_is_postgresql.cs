// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

[Collection(PostgreSqlCollection.Name)]
public class and_the_provider_is_postgresql(PostgreSqlFixture fixture) : given.a_decimal_sink
{
    protected override ISinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };

    Task Because() => Store();

    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)_read["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)_read["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
