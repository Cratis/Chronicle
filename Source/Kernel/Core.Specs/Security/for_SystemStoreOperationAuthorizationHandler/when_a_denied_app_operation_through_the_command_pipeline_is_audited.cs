// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_denied_app_operation_through_the_command_pipeline_is_audited : given.an_operation_policy
{
    void Establish() => _operation = new CommandContext(CorrelationId.New(), _operation.GetType(), _operation, [], new CommandContextValues());

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_existing_operation() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_name_the_command_rather_than_the_context() => _logger.Entries.Single().Message.ShouldContain("ReplayObserver");
    [Fact] void should_not_name_the_command_context() => _logger.Entries.Single().Message.ShouldNotContain(nameof(CommandContext));
}
