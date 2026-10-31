// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_decimals_differ_below_mongodb_precision : given.a_mongodb_round_trip
{
    async Task Establish()
    {
        await StoreInMongoDB(DecimalSchema, """{"value":0.1234567890123456789012345678}""");
        _command = _command with { Content = """{"value":0.1234567890123456789012345679}""" };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_distinguish_the_exact_decimal_values() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
