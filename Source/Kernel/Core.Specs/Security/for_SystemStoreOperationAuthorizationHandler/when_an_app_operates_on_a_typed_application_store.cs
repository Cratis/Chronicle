// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_an_app_operates_on_a_typed_application_store : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        _operation = new { EventStore = (EventStoreName)"application" };
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_operation() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_not_log_an_administrator_denial() => _logger.Entries.ShouldBeEmpty();
}
