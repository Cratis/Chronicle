// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_a_scoped_check_precedes_the_extension : given.a_scoped_caller_probe
{
    [Theory]
    [InlineData(DiscoveryOrder.None)]
    [InlineData(DiscoveryOrder.BeforeExtension)]
    [InlineData(DiscoveryOrder.AfterExtension)]
    public Task should_use_each_request_scopes_own_check_without_resolving_it_from_root(DiscoveryOrder discovery) =>
        VerifyCallerIsolation(registerBefore: true, discovery);
}
