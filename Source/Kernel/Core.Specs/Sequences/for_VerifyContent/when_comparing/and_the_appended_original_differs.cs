// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_appended_original_differs : given.a_generation_history
{
    void Establish() => _command = _command with { Content = """{"value":43}""" };

    async Task Because() => _result = await Verify();

    [Fact] void should_report_the_direct_difference() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
