// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_command_targets_a_physical_system_store : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        var command = new ReplayObserver("System+namespace", "default", "observer", string.Empty);
        _operation = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: Security.given.caller_services.For(_authorization).HttpContext!.RequestServices);
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_not_receive_the_ordinary_store_exemption() => _context.HasSucceeded.ShouldBeFalse();
}
