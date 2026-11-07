// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_the_defaults_are_read : Specification
{
    [Fact] void should_use_the_destructive_operations_default() => new Authentication().EnforceDestructiveOperations.ShouldEqual(Authentication.DefaultEnforceDestructiveOperations);
    [Fact] void should_default_destructive_operations_to_audit_mode() => Authentication.DefaultEnforceDestructiveOperations.ShouldBeFalse();
    [Fact] void should_use_the_administrator_policy_default() => new Authentication().EnforceAdministratorPolicyForOperations.ShouldEqual(Authentication.DefaultEnforceAdministratorPolicyForOperations);
    [Fact] void should_default_the_administrator_policy_for_operations_to_audit_mode() => Authentication.DefaultEnforceAdministratorPolicyForOperations.ShouldBeFalse();
}
