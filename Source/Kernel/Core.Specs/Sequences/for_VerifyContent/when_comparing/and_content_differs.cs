// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_content_differs : given.a_stored_event
{
    void Establish() => _command = _command with { Content = "{\"value\":43}" };

    async Task Because() => _result = await Verify();

    [Fact] void should_report_different() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
