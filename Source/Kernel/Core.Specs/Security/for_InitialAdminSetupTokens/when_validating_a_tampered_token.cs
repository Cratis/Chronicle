// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_InitialAdminSetupTokens;

public class when_validating_a_tampered_token : given.setup_tokens
{
    void Establish() => _token = "changed-" + _tokens.Issue(_userId).Value;
    void Because() => _valid = _tokens.IsValid(_userId, _token);
    [Fact] void should_reject_the_token() => _valid.ShouldBeFalse();
}
