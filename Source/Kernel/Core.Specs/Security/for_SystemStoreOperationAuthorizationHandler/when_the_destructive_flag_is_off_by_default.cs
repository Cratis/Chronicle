// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_the_destructive_flag_is_off_by_default : Specification
{
    [Fact] void should_default_to_not_enforcing() => new Authentication().EnforceDestructiveOperations.ShouldEqual(Authentication.DefaultEnforceDestructiveOperations);
    [Fact] void should_not_enforce_by_default() => Authentication.DefaultEnforceDestructiveOperations.ShouldBeFalse();
}
