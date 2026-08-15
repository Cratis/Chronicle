// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_recording_the_act;

/// <summary>
/// This is the act that makes crypto-shredding reversible, so it is the one that most needs a record. An
/// authorization that let a subject be protected again while Chronicle said nothing at all would leave an incident
/// review with no way to establish that it happened, let alone when.
/// </summary>
/// <remarks>
/// Logged symmetrically with the erasure: recording the un-erase while leaving the erase silent would read as an
/// oversight rather than a decision.
/// </remarks>
public class and_a_new_encryption_key_is_authorized : given.a_pii_manager
{
    Task Because() => _manager.AllowNewEncryptionKeyFor(Identifier);

    [Fact] void should_record_the_act() => _logger.Entries.ShouldNotBeEmpty();
    [Fact] void should_record_it_as_a_compliance_act_rather_than_a_diagnostic() => _logger.Entries[0].Level.ShouldEqual(LogLevel.Information);
    [Fact] void should_record_the_subject_as_a_binding() => _logger.Entries[0].Message.ShouldContain(SubjectBinding);
    [Fact] void should_not_name_the_subject() => _logger.Entries[0].Message.ShouldNotContain(Identifier.Value);
    [Fact] void should_record_the_event_stores_it_reached() => _logger.Entries[0].Message.ShouldContain(OtherEventStore.Value);
    [Fact] void should_say_the_erased_key_does_not_come_back() => _logger.Entries[0].Message.ShouldContain("does not come back");
    [Fact] void should_not_log_any_key_material() => ShouldNotHaveLoggedAnyKeyMaterial();
}
