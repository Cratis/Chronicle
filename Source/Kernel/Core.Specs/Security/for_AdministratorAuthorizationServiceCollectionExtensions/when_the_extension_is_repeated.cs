// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_the_extension_is_repeated : given.a_scoped_caller_probe
{
    [Fact]
    public Task should_keep_one_handler_and_the_callers_check() => VerifyCallerIsolation(true, DiscoveryOrder.AfterExtension, repeated: true);
}
