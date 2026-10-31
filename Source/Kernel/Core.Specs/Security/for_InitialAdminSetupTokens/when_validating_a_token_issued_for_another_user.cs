// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_InitialAdminSetupTokens;

public class when_validating_a_token_issued_for_another_user : given.setup_tokens
{
    void Establish() => _token = _tokens.Issue(Guid.NewGuid());
    void Because() => _valid = _tokens.IsValid(_userId, _token);
    [Fact] void should_reject_the_token() => _valid.ShouldBeFalse();
}
