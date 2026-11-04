// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_an_external_token_lacks_the_mapped_role : given.an_external_token_policy_check
{
    async Task Because() => _allowed = await Authorize("reader", "chronicle-admin-readonly");

    [Fact] void should_deny() => _allowed.ShouldBeFalse();
}
