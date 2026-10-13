<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/evolution-example.md -->
# Example: evolving persisted events

A complete design-mode model showing the three evolution routes from `evolution.md`: a new
generation (compatible change), an identity-aware rename that keeps the stored name as an `id`
pin, and a changed meaning that becomes a new event. Several generations select ESM v4.

```screenplay
// cratis-screenplay-streams-and-consistency: event evolution, complete design-mode example (V1, warnings as errors).
// - MembershipRenewed: a compatible change (new property with a defined default for old facts),
//   declared as generation 2 in full beside generation 1, in the same slice.
// - MembershipPaused: the persisted event formerly stored as "MembershipSuspended"; renamed
//   through the identity-aware path, which keeps the old stored name as an `id` pin.
// - MembershipEnded vs MembershipTerminatedForBreach: a change of meaning is a NEW event, not
//   a generation.
// Screenplay does not express migrations/upcasters (Documentation/screenplay/events.md); the
// target must convert generation 1 facts. Multiple generations select ESM v4.

concept MembershipId : Uuid

module Membership
  feature Renewals
    slice StateChange RenewMembership
      description "Renewal records the period length; generation 1 facts were always 12 months"
      command RenewMembership
        membershipId MembershipId identifier
        months       Int
        validate
          months > 0  message "A renewal covers at least one month"
        produces MembershipRenewed
          for membershipId
          months = months
      event MembershipRenewed generation 1
      event MembershipRenewed generation 2
        months Int
      specification RenewingForSixMonths
        when RenewMembership
          membershipId = "5b0f4c3e-2d1a-4e6b-8f7c-9a1b2c3d4e05"
          months       = 6
        then MembershipRenewed
          for "5b0f4c3e-2d1a-4e6b-8f7c-9a1b2c3d4e05"
          months = 6

    slice StateChange PauseMembership
      command PauseMembership
        membershipId MembershipId identifier
        produces MembershipPaused
          for membershipId
      event MembershipPaused
        id "MembershipSuspended"

    slice StateChange EndMembership
      description "Ending by the member and termination for breach are different facts"
      command EndMembership
        membershipId MembershipId identifier
        forBreach    Bool
        produces when forBreach == false
          MembershipEnded
            for membershipId
        produces when forBreach == true
          MembershipTerminatedForBreach
            for membershipId
      event MembershipEnded
      event MembershipTerminatedForBreach
      specification EndingByChoice
        when EndMembership
          membershipId = "5b0f4c3e-2d1a-4e6b-8f7c-9a1b2c3d4e05"
          forBreach    = false
        then MembershipEnded
          for "5b0f4c3e-2d1a-4e6b-8f7c-9a1b2c3d4e05"
```
