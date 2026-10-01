// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// <c language="csharp">[Unique]</c> under one name on two event types is one constraint: at most one event from either per event source.
/// Declared separately, each definition checked only its own event type so the other was accepted, and a genuine
/// violation threw while its message was being resolved because the name matched two definitions.
/// </summary>
public class when_event_types_share_a_unique_attribute_name : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _phone;
    AppendResult _phoneAgain;
    AppendResult _email;
    AppendResult _emailForAnotherSource;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(PhoneClaimed), typeof(EmailClaimed)]);
        artifacts.UniqueEventTypeConstraints.Returns([typeof(PhoneClaimed), typeof(EmailClaimed)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because()
    {
        var id = EventSourceId.New();
        _phone = await _scenario.When.ForEventSource(id).Events(new PhoneClaimed("555-0100"));
        _phoneAgain = await _scenario.When.ForEventSource(id).Events(new PhoneClaimed("555-0100"));
        _email = await _scenario.When.ForEventSource(id).Events(new EmailClaimed("a@example.com"));
        _emailForAnotherSource = await _scenario.When.ForEventSource(EventSourceId.New()).Events(new EmailClaimed("a@example.com"));
    }

    [Fact] void should_accept_the_first_claim() => _phone.ShouldBeSuccessful();
    [Fact] void should_reject_the_same_event_type_again() => _phoneAgain.ShouldHaveConstraintViolation(ContactClaim.Name);
    [Fact] void should_resolve_the_message_of_that_violation() => _phoneAgain.ConstraintViolations.Single().Message.Value.ShouldEqual("A contact has already been claimed");
    [Fact] void should_reject_the_other_event_type_for_the_same_event_source() => _email.ShouldHaveConstraintViolation(ContactClaim.Name);
    [Fact] void should_accept_the_other_event_type_for_another_event_source() => _emailForAnotherSource.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
