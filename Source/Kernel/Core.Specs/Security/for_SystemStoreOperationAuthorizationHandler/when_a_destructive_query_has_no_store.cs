// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_destructive_query_has_no_store : given.a_query_operation_policy
{
    void Establish()
    {
        _enforceDestructive = true;
        Query(nameof(given.operation_queries.Destructive));
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_refuse_the_operation() => _context.HasSucceeded.ShouldBeFalse();
}
