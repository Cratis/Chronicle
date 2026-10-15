// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_decimal_digits_differ_in_sql : given.a_storage_round_trip
{
    async Task Establish()
    {
        await Store(DecimalSchema, """{"value":0.1234567890123456789012345678}""", sql: true);
        _command = _command with { Content = """{"value":0.1234567890123456789012345679}""" };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_detect_the_last_decimal_digit() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
