// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Chronicle.Registrations.for_RegistrationBackoff;

public class when_classifying_namespace_lifecycle_failures : Specification
{
    Exception[] _failures;
    bool[] _retryable;

    void Establish() => _failures =
    [
        new CommandFailed(new() { ExceptionMessages = ["NamespaceRecoveryRequired"] }),
        new TargetInvocationException(new AggregateException(new InvalidOperationException(), new CommandFailed(new() { ExceptionMessages = ["NamespaceRecoveryRequired"] }))),
        new CommandFailed(new() { ExceptionMessages = ["NamespaceDeleting"] }),
        new CommandFailed(new() { ExceptionMessages = ["SomeNamespaceRecoveryRequiredMessage"] }),
        new InvalidOperationException("NamespaceRecoveryRequired"),
        new CommandFailed(null)
    ];

    void Because() => _retryable = _failures.Select(RegistrationBackoff.IsRetryable).ToArray();

    [Fact] void should_stop_on_the_literal_recovery_required_code() => _retryable[0].ShouldBeFalse();
    [Fact] void should_find_the_code_inside_wrapped_aggregate_failures() => _retryable[1].ShouldBeFalse();
    [Fact] void should_retry_a_deleting_namespace() => _retryable[2].ShouldBeTrue();
    [Fact] void should_not_match_a_substring_in_a_structured_field() => _retryable[3].ShouldBeTrue();
    [Fact] void should_not_parse_exception_messages() => _retryable[4].ShouldBeTrue();
    [Fact] void should_preserve_retry_behavior_for_a_missing_result() => _retryable[5].ShouldBeTrue();
}
