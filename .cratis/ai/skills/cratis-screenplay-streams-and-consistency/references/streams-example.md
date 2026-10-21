<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/streams-example.md -->
# Example: streams, constraints and a recorded rule

A complete design-mode model for course enrolment. It shows one business identity per stream,
cross-stream references as payload, three kinds of uniqueness constraint, a state-dependent
rule recorded as **NOT enforced in the model today**, a declared `concurrency` scope, and an
active-state view with removal. Read it with `consistency-and-concurrency.md` (sections 1-4).

The model compiles (V1, warnings as errors) but is not binding-ready by design:
`reads` and the `concurrency` block are PLAY0271, and `require` over a view is PLAY0268.
The `pii` concept failed binding on that compiler and still fails on the pinned CLI,
but binds as v13 on standalone 4.142.0. Keep those lines: they state real intent.

```screenplay expect PLAY0516 test=unbound
// cratis-screenplay-streams-and-consistency: complete design-mode example (V1 with --warnings-as-errors).
// Shows: one business identity per stream, cross-stream references as payload, uniqueness
// constraints (single, composite with release, unique event), a recorded state-dependent rule
// that Screenplay cannot enforce, a declared concurrency scope, and an active-state view with removal.
// Historical/CLI binding probes refuse compliance; standalone 4.142.0 binds this pii concept as v13 (since 4.141.0), the
// concurrency block and `reads` are PLAY0271, `require` over a view is PLAY0268. Keep them: they state
// real intent. The capacity rule is NOT enforced in the model today (see the slice description).

domain Acme.Academy

concept MemberId : Uuid                    // event source id of one member stream
concept CourseId : Uuid                    // event source id of one course stream
concept EnrolmentId : Uuid                 // event source id of one enrolment stream
concept Email : String pii
  pii reason "Identifies a person"
  validate
    not empty  message "An email is required"

policy IsAuthenticated
  require authenticated
policy IsCoordinator
  require role "Coordinator"
persona Coordinator
  description "Schedules courses and manages enrolments. Cannot: change member contact details"
  policy IsAuthenticated
  policy IsCoordinator

module Membership
  description "Who our members are"
  authorize IsAuthenticated
  feature Members
    slice StateChange RegisterMember
      description "A person becomes a member; the email address is unique across members"
      command RegisterMember
        memberId MemberId identifier
        email    Email
        name     String
        authorize IsCoordinator
        produces event MemberRegistered
          email Email = email
          name String = name
      constraint UniqueMemberEmail
        unique email on MemberRegistered
        ignore casing
      specification RegisteringAMember
        given caller
          authenticated
          role "Coordinator"
        when RegisterMember
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          email    = "kari@example.com"
          name     = "Kari"
        then MemberRegistered
          for "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          email = "kari@example.com"
          name  = "Kari"
      specification RefusingARegistrationWithoutTheCoordinatorRole
        given caller
          authenticated
        when RegisterMember
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          email    = "kari@example.com"
          name     = "Kari"
        then denied
      specification RefusingAnEmailAnotherMemberHolds
        given caller
          authenticated
          role "Coordinator"
        given MemberRegistered
          for "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          email = "kari@example.com"
          name  = "Kari"
        when RegisterMember
          memberId = "7b2d3c9f-1c2e-4e66-8b4f-3a7b8d2e1f22"
          email    = "kari@example.com"
          name     = "Kari Two"
        then error "Constraint 'UniqueMemberEmail' is violated: another event source already holds the constrained value."

module Courses
  description "Courses and who attends them"
  authorize IsAuthenticated
  feature Enrolment
    authorize IsCoordinator
    slice StateChange OpenCourse
      command OpenCourse
        courseId CourseId identifier
        seats    Int
        produces event CourseOpened
          seats Int = seats
      specification OpeningACourse
        given caller
          authenticated
          role "Coordinator"
        when OpenCourse
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats    = 12
        then CourseOpened
          for "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats = 12
      specification RefusingOpeningACourseWithoutTheCoordinatorRole
        given caller
          authenticated
        when OpenCourse
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats    = 12
        then denied

    slice StateView CourseAvailability
      description "Open seats per course, built from enrolment and withdrawal facts. A lagging view: usable for display and as stated intent, not as a protected decision. It routes enrolment-stream facts to the course by a payload property (fan-in). A Chronicle protected decision read admits only a projection keyed directly by the event source id and refuses event-property routing, so this view cannot be guarded, and a concurrency scope on the course source would not see enrolment-stream appends. Consequence: two concurrent enrolments can both pass the capacity check and overbook the course. Redesign awaiting agreement (see consistency-and-concurrency.md section 3): record seat reservations as decisions on the course source (a seat-taken fact there, with enrolment reacting to it), or give each seat its own identity and claim it with a uniqueness constraint."
      readmodel CourseAvailability
        courseId  CourseId
        openSeats Int
      query CourseAvailabilityById => CourseAvailability optional
        by courseId CourseId
      projection CourseAvailabilityProjection => CourseAvailability
        from CourseOpened
          courseId = $eventSourceId
          openSeats = seats
        from MemberEnrolled key courseId
          decrement openSeats
        from EnrolmentWithdrawn key courseId
          increment openSeats
      specification CountingOpenSeats
        given caller
          authenticated
          role "Coordinator"
        given CourseOpened
          for "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats = 12
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then query CourseAvailabilityById
          arguments
            courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          result
            openSeats = 11
      specification RefusingAvailabilityToAnOrdinaryMember
        given caller
          authenticated
        then query CourseAvailabilityById
          arguments
            courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        then denied

    slice StateChange EnrolMember
      description "Atomic here: one active enrolment per member and course (composite unique constraint, released by withdrawal), and one MemberEnrolled per enrolment stream, so a retry with the same enrolment id after a lost reply cannot enrol twice (the composite constraint alone lets a source re-claim its own value). NOT enforced in the model today: 'enrolments never exceed the course capacity'. The rule depends on other streams' state: `reads` is PLAY0271 and `require` over a view is PLAY0268 at binding (Screenplay#129). The enrolment-to-course fan-in cannot be protected as modeled (see the CourseAvailability slice): concurrent enrolments can overbook. Target enforcement is an open decision (consistency-and-concurrency.md section 3); the model stays as stated intent, recorded in STATE.md."
      command EnrolMember
        enrolmentId EnrolmentId identifier
        courseId    CourseId
        memberId    MemberId
        concurrency
          eventSource
        reads CourseAvailability as course by courseId   // stated intent, unprotected (PLAY0271 at binding)
        validate
          require course.openSeats > 0
            message "The course is full"                 // NOT enforced: the view lags (PLAY0268 at binding)
        produces event MemberEnrolled
          courseId CourseId = courseId          // cross-stream references are payload
          memberId MemberId = memberId
      constraint OneActiveEnrolmentPerMemberAndCourse
        unique courseId, memberId on MemberEnrolled
        released by EnrolmentWithdrawn
      constraint EnrolOnce
        unique event MemberEnrolled
      specification EnrollingAMember
        given caller
          authenticated
          role "Coordinator"
        given CourseOpened
          for "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats = 12
        when EnrolMember
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
      specification RefusingARetriedEnrolment
        given caller
          authenticated
          role "Coordinator"
        given CourseOpened
          for "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats = 12
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        when EnrolMember
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then error "Constraint 'EnrolOnce' is violated: the event source already has the constrained event."

      specification RefusingAnEnrolmentWithoutTheCoordinatorRole
        given caller
          authenticated
        when EnrolMember
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then denied
      specification RefusingASecondActiveEnrolmentUnderANewId
        given caller
          authenticated
          role "Coordinator"
        given CourseOpened
          for "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats = 12
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        when EnrolMember
          enrolmentId = "1e4c0f3b-7d5a-4a2f-9c8b-6f3e2d4c5b12"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then error "Constraint 'OneActiveEnrolmentPerMemberAndCourse' is violated: another event source already holds the constrained value."
      specification ReEnrollingAfterWithdrawal
        given caller
          authenticated
          role "Coordinator"
        given CourseOpened
          for "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          seats = 12
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        given EnrolmentWithdrawn
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        when EnrolMember
          enrolmentId = "1e4c0f3b-7d5a-4a2f-9c8b-6f3e2d4c5b12"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then MemberEnrolled
          for "1e4c0f3b-7d5a-4a2f-9c8b-6f3e2d4c5b12"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"

    slice StateChange WithdrawEnrolment
      description "NOT enforced in the model today: a withdrawal must name an existing active enrolment of the same course. `courseId` is only a routing reference supplied by the caller, so without this check a wrong course would restore a seat elsewhere, and a withdrawal of an enrolment that does not exist would invent a free seat. Stated as `reads` + `require` over ActiveEnrolmentEntry (PLAY0271/PLAY0268 at binding). Target enforcement: a protected decision read of the enrolment (it is keyed directly by the enrolment source id, so it qualifies). Requirements to test in the target: withdrawing an unknown enrolment is rejected; withdrawing with another course is rejected; a correct withdrawal restores one seat."
      command WithdrawEnrolment
        enrolmentId EnrolmentId identifier
        courseId    CourseId
        reads ActiveEnrolmentEntry as enrolment by enrolmentId   // stated intent, unprotected
        validate
          require enrolment.courseId == courseId
            message "The enrolment does not belong to that course"   // NOT enforced today
        produces event EnrolmentWithdrawn
          courseId CourseId = courseId
      constraint WithdrawOnce
        unique event EnrolmentWithdrawn
      specification WithdrawingAnEnrolment
        given caller
          authenticated
          role "Coordinator"
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        when WithdrawEnrolment
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        then EnrolmentWithdrawn
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
      specification RefusingAWithdrawalWithoutTheCoordinatorRole
        given caller
          authenticated
        when WithdrawEnrolment
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        then denied
      specification RefusingASecondWithdrawal
        given caller
          authenticated
          role "Coordinator"
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        given EnrolmentWithdrawn
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        when WithdrawEnrolment
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId    = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        then error "The enrolment does not belong to that course"
      specification AppendingASecondWithdrawal
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        given EnrolmentWithdrawn
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        when append EnrolmentWithdrawn
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        then error "Constraint 'WithdrawOnce' is violated: the event source already has the constrained event."

    slice StateView ActiveEnrolment
      description "An active enrolment is present until withdrawal; the facts stay in history."
      readmodel ActiveEnrolmentEntry
        enrolmentId EnrolmentId
        courseId    CourseId
        memberId    MemberId
      query ActiveEnrolmentById => ActiveEnrolmentEntry optional
        by enrolmentId EnrolmentId
      projection ActiveEnrolments => ActiveEnrolmentEntry
        from MemberEnrolled
          enrolmentId = $eventSourceId
          courseId = courseId
          memberId = memberId
        remove with EnrolmentWithdrawn
      specification ShowingAnActiveEnrolment
        given caller
          authenticated
          role "Coordinator"
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then query ActiveEnrolmentById
          arguments
            enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          result
            courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
            memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
      specification RefusingEnrolmentDetailToAnOrdinaryMember
        given caller
          authenticated
        then query ActiveEnrolmentById
          arguments
            enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
        then denied
      specification RemovingAWithdrawnEnrolment
        given MemberEnrolled
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        when append EnrolmentWithdrawn
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          courseId = "a4e7c1d2-3b5f-4a6e-9c8d-7f1e2d3c4b02"
        then no readmodel ActiveEnrolmentEntry for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
```
