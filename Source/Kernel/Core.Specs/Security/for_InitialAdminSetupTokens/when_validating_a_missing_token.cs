// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_InitialAdminSetupTokens;

public class when_validating_a_missing_token : given.setup_tokens
{
    void Because() => _valid = _tokens.IsValid(_userId, null);
    [Fact] void should_reject_the_token() => _valid.ShouldBeFalse();
}
