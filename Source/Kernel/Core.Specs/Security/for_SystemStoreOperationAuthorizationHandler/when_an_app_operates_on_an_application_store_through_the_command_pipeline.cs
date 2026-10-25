// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_an_app_operates_on_an_application_store_through_the_command_pipeline : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        var command = new ReplayObserver("application", "default", "observer", string.Empty);
        _operation = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues());
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_operation_without_administrator_privilege() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_not_log_an_administrator_denial() => _logger.Entries.ShouldBeEmpty();
}
