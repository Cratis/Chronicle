// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.ProtectedValues.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter;

public class when_reporting_a_personal_identifier : Specification
{
    const string Identifier = "person-6093@example.invalid";
    CapturingLogger<UnrecoverableProtectedValueReporter> _logger;
    UnrecoverableProtectedValueReporter _reporter;
    EncryptionKeyUnrecoverable _failure;
    int _count;

    void Establish()
    {
        _logger = new();
        _reporter = new(_logger, TimeProvider.System, () => _count++);
        _failure = new(3, new InvalidOperationException($"Cannot release {Identifier}"));
    }

    void Because() => _reporter.Report(new("store", "namespace", Identifier, "contact.email", "Release"), _failure);

    [Fact] void should_log_one_error() => _logger.Entries.Select(_ => _.Level).ShouldContainOnly(LogLevel.Error);
    [Fact] void should_count_the_failure() => _count.ShouldEqual(1);
    [Fact] void should_identify_the_store_and_namespace() => _logger.Entries.Single().Message.ShouldContain("store/namespace");
    [Fact] void should_identify_the_property_and_phase() => _logger.Entries.Single().Message.ShouldContain("at contact.email during Release");
    [Fact] void should_record_the_exception_type() => _logger.Entries.Single().State.Single(_ => _.Key == "ExceptionType").Value.ShouldEqual(nameof(InvalidOperationException));
    [Fact] void should_record_the_attempted_revisions() => _logger.Entries.Single().State.Single(_ => _.Key == "AttemptedRevisions").Value.ShouldEqual(3);
    [Fact] void should_not_name_the_identifier_in_the_message() => _logger.Entries.Single().Message.ShouldNotContain(Identifier);
    [Fact] void should_not_name_the_identifier_in_structured_state() => _logger.Entries.Single().State.Any(_ => _.Value?.ToString()?.Contains(Identifier, StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_attach_the_identifier_bearing_exception() => _logger.Entries.Single().Exception.ShouldBeNull();
}
