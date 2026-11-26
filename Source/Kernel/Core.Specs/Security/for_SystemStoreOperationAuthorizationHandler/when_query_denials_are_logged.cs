// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_query_denials_are_logged : given.a_query_operation_policy
{
    string _auditIdentity;
    string _refusalIdentity;

    async Task Because()
    {
        await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "System", ["token"] = Secret });
        _auditIdentity = _query.Name.Value;
        _enforceDestructive = true;
        await Allows(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = "System", ["token"] = Secret });
        _refusalIdentity = _query.Name.Value;
    }

    [Fact] void should_log_the_registered_query_identity_for_audit() => _logger.Entries[0].Message.ShouldContain(_auditIdentity);
    [Fact] void should_log_the_registered_query_identity_for_refusal() => _logger.Entries[1].Message.ShouldContain(_refusalIdentity);
    [Fact] void should_audit_at_warning_level() => _logger.Entries[0].Level.ShouldEqual(LogLevel.Warning);
    [Fact] void should_refuse_at_warning_level() => _logger.Entries[1].Level.ShouldEqual(LogLevel.Warning);
    [Fact] void should_not_log_argument_or_credential_values() => _logger.Entries.ShouldEachConformTo(_ => !_.Message.Contains(Secret));
    [Fact] void should_not_misidentify_the_query_as_its_context() => _logger.Entries.ShouldEachConformTo(_ => !_.Message.Contains("QueryContext"));
}
