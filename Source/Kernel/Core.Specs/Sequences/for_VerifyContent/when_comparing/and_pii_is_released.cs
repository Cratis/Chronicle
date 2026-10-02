// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_pii_is_released : given.a_protected_event
{
    async Task Because() => _result = await _command.Handle(_storage, _manager);

    [Fact] void should_recognize_genuinely_empty_content() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
