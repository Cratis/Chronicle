<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/automation-translate-example.md -->
# Automation and Translate example (ESM v6)

Executable Automation and Translate slices. Binds as `executableReady` (ESM v34, for `then invokes`) on the standalone tool and on a CLI whose bundled compiler is Screenplay 4.246.0 or later; see [executable-subset.md](executable-subset.md) for what renders. Shows a reaction `produces` cascade, `invokes`, `where`, clock `at`, an application trigger, a capture with translate and a transition `when`, and v6 specifications. Each specification states only its own slice (Screenplay decision 0115); the automation states the command it invokes with `then invokes` (ESM v34).

The `Renew` command is ungated so that the reaction can invoke it: this is a syntax demonstration, not a safe production design. Reactions invoke with no caller, so a real model needs a trusted path (see [cratis-screenplay-automations-and-translations](../../cratis-screenplay-automations-and-translations/SKILL.md)).

```screenplay
// Automation and Translate: ESM v6 forms.
concept MemberId : Uuid

trigger RenewalFileArrived
  memberId MemberId
  months   Int

module Membership
  feature Subscriptions
    slice StateChange Subscribe
      command Subscribe
        memberId MemberId identifier
        produces event MemberSubscribed
          subscribedAt DateTime = $context.occurred
      screen Subscribe
        action Subscribe for screen
      specification SubscribingAMember
        given clock "2026-10-05T09:00:00Z"
        when Subscribe
          memberId = "5b6c1f7e-2d3a-4e8b-9c0d-1a2b3c4d5e6f"
        then MemberSubscribed                             // its own slice only: Onboarding states WelcomeSent
          for "5b6c1f7e-2d3a-4e8b-9c0d-1a2b3c4d5e6f"
          subscribedAt = "2026-10-05T09:00:00Z"

    slice StateChange Renew
      command Renew                                       // ungated for the demo only: reactions invoke with no caller
        memberId MemberId identifier
        months   Int
        validate
          months > 0  message "Renew for at least one month"
        produces event MembershipRenewed
          months Int = months
      specification RejectingAZeroRenewal
        when Renew
          memberId = "5b6c1f7e-2d3a-4e8b-9c0d-1a2b3c4d5e6f"
          months   = 0
        then error "Renew for at least one month"

    slice Automation Onboarding
      reaction Welcomer
        when MemberSubscribed
          produces WelcomeSent                            // no `for`: lands on the trigger's source
            sentAt = $context.occurred
      reaction RenewalImporter
        when RenewalFileArrived
          memberId
          months
          invokes Renew
            memberId = memberId
            months   = months
        where months > 0
      reaction WeeklyReport
        at 07:30 on Monday
          produces ReportIssued
            for "weekly"                                  // clock trigger needs an explicit `for`
            issuedAt = $context.occurred
      event WelcomeSent
        sentAt DateTime
      event ReportIssued
        issuedAt DateTime
      specification WelcomingAfterAnAppend
        given clock "2026-10-05T09:00:00Z"
        when append MemberSubscribed                      // `then` lists only what followed
          for "5b6c1f7e-2d3a-4e8b-9c0d-1a2b3c4d5e6f"
          subscribedAt = "2026-10-05T09:00:00Z"
        then WelcomeSent
          for "5b6c1f7e-2d3a-4e8b-9c0d-1a2b3c4d5e6f"
          sentAt = "2026-10-05T09:00:00Z"
      specification RenewingFromAFile
        when trigger RenewalFileArrived
          memberId = "5b6c1f7e-2d3a-4e8b-9c0d-1a2b3c4d5e6f"
          months   = 12
        then invokes Renew                                // ESM v34: the hand-over; Renew's slice proves MembershipRenewed
          memberId = "5b6c1f7e-2d3a-4e8b-9c0d-1a2b3c4d5e6f"
          months   = 12
      specification IssuingTheWeeklyReport
        given clock "2026-10-05T07:00:00Z"
        when clock "2026-10-05T07:30:00Z"
        then ReportIssued
          for "weekly"
          issuedAt = "2026-10-05T07:30:00Z"

    slice Translate LegacyMembers
      capture LegacyMemberCapture
        key id
        map
          status = status translate
            "aktiv"   => active
            "avsluttet" => cancelled
        append LegacyMembershipCancelled
          when status from "active" to "cancelled"
            cancelledAt = $context.occurred
      event LegacyMembershipCancelled
        cancelledAt DateTime
      specification SeeingALegacyCancellation
        given clock "2026-10-05T12:00:00Z"
        given capture LegacyMemberCapture
          id     = "m-42"
          status = "aktiv"
        when capture LegacyMemberCapture
          id     = "m-42"
          status = "avsluttet"
        then LegacyMembershipCancelled
          for "m-42"
          cancelledAt = "2026-10-05T12:00:00Z"
```
