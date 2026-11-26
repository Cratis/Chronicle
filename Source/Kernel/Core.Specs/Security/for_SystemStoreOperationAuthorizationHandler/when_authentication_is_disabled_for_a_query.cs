// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_authentication_is_disabled_for_a_query : given.a_query_operation_policy
{
    void Establish()
    {
        _authenticationEnabled = false;
        _enforce = true;
        _enforceDestructive = true;
        Query(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = "System" });
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_preserve_disabled_authentication_behavior() => _context.HasSucceeded.ShouldBeTrue();
}
