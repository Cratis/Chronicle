// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// The whole release path end to end, for a lifecycle that ends in more than one way. An invitation is over when it
/// is accepted, revoked or expires, and each of those releases the address it held — so the next invitation to that
/// address is allowed whichever way the previous one ended.
/// </summary>
/// <remarks>
/// The declaration used to keep only the last removal event, so an address released by anything but the final
/// declaration stayed claimed forever: the terminal event appended successfully, the constraint went on blocking, and
/// nothing anywhere reported a problem. An outstanding invitation is claimed too, so a constraint that released on
/// everything would not read as a pass either.
/// </remarks>
public class when_a_value_is_released_by_each_of_several_terminal_events : Specification, IDisposable
{
    static readonly InvitedEmailAddress _acceptedAddress = new("accepted@cratis.io");
    static readonly InvitedEmailAddress _revokedAddress = new("revoked@cratis.io");
    static readonly InvitedEmailAddress _expiredAddress = new("expired@cratis.io");
    static readonly InvitedEmailAddress _outstandingAddress = new("outstanding@cratis.io");

    EventScenario _scenario;
    AppendResult _afterAcceptance;
    AppendResult _afterRevocation;
    AppendResult _afterExpiry;
    AppendResult _whileOutstanding;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        await Invite(_acceptedAddress, new InvitationAccepted());
        await Invite(_revokedAddress, new InvitationRevoked());
        await Invite(_expiredAddress, new InvitationExpired());
        await Invite(_outstandingAddress, terminalEvent: null);

        _afterAcceptance = await Claim(_acceptedAddress);
        _afterRevocation = await Claim(_revokedAddress);
        _afterExpiry = await Claim(_expiredAddress);
        _whileOutstanding = await Claim(_outstandingAddress);
    }

    [Fact] void should_allow_the_address_again_after_the_invitation_was_accepted() => _afterAcceptance.ShouldBeSuccessful();
    [Fact] void should_allow_the_address_again_after_the_invitation_was_revoked() => _afterRevocation.ShouldBeSuccessful();
    [Fact] void should_allow_the_address_again_after_the_invitation_expired() => _afterExpiry.ShouldBeSuccessful();
    [Fact] void should_still_hold_the_address_of_an_outstanding_invitation() => _whileOutstanding.ShouldHaveConstraintViolation(UniqueInvitedEmailAddress.Name);

    public void Dispose() => _scenario.Dispose();

    async Task Invite(InvitedEmailAddress address, object? terminalEvent)
    {
        var invitation = EventSourceId.New();
        await _scenario.Given.ForEventSource(invitation).Events(new InvitationSent(address));

        if (terminalEvent is not null)
        {
            await _scenario.Given.ForEventSource(invitation).Events(terminalEvent);
        }
    }

    Task<AppendResult> Claim(InvitedEmailAddress address) =>
        _scenario.When.ForEventSource(EventSourceId.New()).Events(new InvitationSent(address));
}
