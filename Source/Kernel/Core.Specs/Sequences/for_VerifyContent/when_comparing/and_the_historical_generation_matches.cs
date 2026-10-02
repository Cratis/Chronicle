// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_historical_generation_matches : given.a_stored_event
{
    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_compare_the_requested_generation_not_the_latest_clr_content() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
