// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_an_ordinary_operation_has_no_context_provider : given.an_operation_policy
{
    void Establish()
    {
        var command = new ReplayObserver("ordinary", "default", Secret, string.Empty);
        _operation = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues());
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_operation_in_audit_mode() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_not_record_a_denial_for_an_unenrolled_operation() => _logger.Entries.ShouldBeEmpty();
    [Fact] void should_not_consult_an_administrator_check() => _authorization.ReceivedCalls().ShouldBeEmpty();
}
