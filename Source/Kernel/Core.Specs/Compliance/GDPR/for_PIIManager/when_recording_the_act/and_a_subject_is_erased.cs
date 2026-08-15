// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_recording_the_act;

/// <summary>
/// Erasure is a deliberate compliance act, not a diagnostic, so Chronicle records that it happened and how far it
/// reached - and records the completion rather than the intent, because whether it finished is the whole question.
/// </summary>
/// <remarks>
/// The subject appears as a stable one-way binding rather than by name. An operator who knows the subject can
/// compute it and find the act; a log aggregator that outlives the erasure does not end up holding the name of a
/// person whose data the erasure existed to remove.
/// </remarks>
public class and_a_subject_is_erased : given.a_pii_manager
{
    Task Because() => _manager.DeleteEncryptionKeyFor(Identifier);

    [Fact] void should_record_the_act() => _logger.Entries.ShouldNotBeEmpty();
    [Fact] void should_record_it_as_a_compliance_act_rather_than_a_diagnostic() => _logger.Entries[0].Level.ShouldEqual(LogLevel.Information);
    [Fact] void should_record_the_subject_as_a_binding() => _logger.Entries[0].Message.ShouldContain(SubjectBinding);
    [Fact] void should_not_name_the_subject() => _logger.Entries[0].Message.ShouldNotContain(Identifier.Value);
    [Fact] void should_record_the_namespace() => _logger.Entries[0].Message.ShouldContain(EventStoreNamespace.Value);
    [Fact] void should_record_the_event_store_it_was_asked_for() => _logger.Entries[0].Message.ShouldContain(EventStore.Value);
    [Fact] void should_record_the_event_store_the_key_was_copied_into() => _logger.Entries[0].Message.ShouldContain(OtherEventStore.Value);
    [Fact] void should_not_log_any_key_material() => ShouldNotHaveLoggedAnyKeyMaterial();
}
