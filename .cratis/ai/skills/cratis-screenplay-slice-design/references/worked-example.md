<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/worked-example.md -->
# Worked example: parcel lockers (design mode)

One complete legacy design-mode control for `cratis-screenplay-slice-design`, without
`command reads protected`. Its reads and read paths retain PLAY0271/PLAY0268, so no
specification runs here. List queries and clock fixtures bind on an admitting compiler;
they are not the remaining blocker. Personas and screens are informational.
Eligible command reads can opt into v25; inspect every projection/key/removal before
migration and keep the separate rendering result (Stage 4.138.0 renders protected reads, STAGE-ESM-044 otherwise; the v25 pair itself is admitted). Read
[protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md).
The unenforced descriptions below refer to this legacy control, not all command reads.
The specifications still seed every read-model source (`LockerInstalled`, `LockerRequested`,
complete givens) so they are the right form once binding is possible.
Lineage matrix: `field-lineage.md`. State-transition table and rule coverage: `slicing.md`.
Per-command refusal inventory: `command-inventory.md`.

This historical compiled example keeps per-event reasoning in StateView
`description` strings. For authoring checked, keep descriptions as summaries
and record lineage reasoning in fenced Markdown `documentation` (`field-lineage.md`).
The historical source below stays unchanged; it is not the current placement recipe.

The excerpt's Stage availability note is historical (Stage 4.24.x, Arc 22.25.0).
Stage 4.73.1's scaffold pins an Arc/Chronicle set (see the Stage skill's `references/rendered-application.md`), so protected-read APIs are available
to gap-fill (the compiled description below saying they are "not available in Stage-rendered apps" is superseded); this neither admits these `.play` rules nor proves target protection.
The original compiled excerpt is retained unchanged. Its `expect PLAY0516` is the story-order
information (`slicing.md` "Declare in story order"): the `LockerBoard` view consumes
`LockerAssigned` from the feature declared after it. A new model declares the producing feature
first and carries no such information.

```screenplay expect PLAY0516 test=unbound
domain Parcelpoint.Lockers

concept LockerId : Uuid
concept RequestId : Uuid
concept LockerNumber : String
  validate
    not empty  message "A locker needs a number"
concept Litres : Int
  validate
    min 1  message "A locker must hold at least one litre"
concept RequestState : Enum
  waiting
  assigned

policy IsAuthenticated
  require authenticated
policy IsAttendant
  require role "Attendant"

persona Attendant
  description "Keeps the locker register and decides which request gets which locker"
  policy IsAuthenticated
  policy IsAttendant
persona Customer
  description "A customer who asks for a locker and follows the answer"
  policy IsAuthenticated

module Lockers
  description "Lockers, requests and assignments for one pickup point"
  authorize IsAuthenticated
  feature Installation
    slice StateChange InstallLocker
      description "The attendant adds a locker to the register"
      command InstallLocker
        lockerId LockerId identifier
        number LockerNumber
        volume   Litres
        authorize IsAttendant
        produces event LockerInstalled
          number LockerNumber = number
          volume Litres = volume
      constraint UniqueLockerNumber
        unique number on LockerInstalled
        message "That locker number is already installed"
      constraint LockerInstalledOnce
        unique event LockerInstalled
        message "The locker is already installed"
      specification InstallingALocker
        given caller
          authenticated
          role "Attendant"
        when InstallLocker
          lockerId = "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          number = "B-07"
          volume   = 120
        then LockerInstalled
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          number = "B-07"
          volume   = 120
      specification RefusingACustomerInstallingALocker
        given caller
          authenticated
          role "Customer"
        when InstallLocker
          lockerId = "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          number = "B-07"
          volume   = 120
        then denied

    slice StateView LockerBoard
      description "The attendant's overview of every locker and whether it is taken. LockerInstalled sets number, volume and assigned=false: the locker enters the board. LockerAssigned sets assigned=true: the board shows which lockers are taken."
      readmodel LockerSummary
        lockerId   LockerId
        number   LockerNumber
        volume     Litres
        assigned Bool
      query LockerById => LockerSummary optional
        by lockerId LockerId
      query ListLockers => LockerSummary[]
      projection LockerBoard => LockerSummary
        from LockerInstalled
          lockerId   = $eventSourceId
          number   = number
          volume     = volume
          assigned = false
        from LockerAssigned
          assigned = true
      screen LockerBoard
        title "Lockers"
        data LockerSummary[] via query ListLockers
        table LockerSummary
          column number   label "Locker"
          column volume     label "Volume (l)"
          column assigned label "Taken"
        action InstallLocker
        action AssignLocker
      specification RefusingAnAnonymousLockerLookup
        given caller
        then query LockerById
          arguments
            lockerId = "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
        then denied
      specification RefusingAnAnonymousLockerList
        given caller
        then query ListLockers
        then denied

  feature Requests
    slice StateChange RequestLocker
      description "A customer asks for a locker of roughly the size they need"
      command RequestLocker
        requestId RequestId identifier
        preferredVolume Litres
        produces event LockerRequested
          preferredVolume Litres = preferredVolume
          requestedAt DateTime = $context.occurred
      specification RequestingALocker
        given caller
          authenticated
        given clock "2026-03-02T10:00:00Z"
        when RequestLocker
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          preferredVolume = 100
        then LockerRequested
          for "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          preferredVolume = 100
          requestedAt   = "2026-03-02T10:00:00Z"

      specification RefusingAnAnonymousRequest
        given caller
        when RequestLocker
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          preferredVolume = 100
        then denied

    slice StateChange AssignLocker
      description "The attendant gives one waiting request one free locker. Constraints enforce one holder per locker and one locker per request at append time. The state rules (the locker is installed and free, the request exists and is still waiting) are written as reads plus require: stated intent, NOT enforced in the model today (PLAY0268/0271 at binding are expected in design mode; Screenplay#129). Target enforcement: Arc [ProtectedDecision] with DecisionRead<T> (Arc 22.39.0 or later; not available in Stage-rendered apps, where this stays a capability gap), or a Chronicle DCB concurrency scope on the locker stream; the constraints stay as the backstop."
      command AssignLocker
        lockerId  LockerId identifier
        requestId RequestId
        reads LockerSummary as locker by lockerId
        reads RequestStatus as request by requestId
        authorize IsAttendant
        validate
          require locker.assigned == false
            message "The locker is not free"
          require request.state == "waiting"
            message "The request is not waiting for a locker"
        produces event LockerAssigned
          requestId RequestId = requestId
      constraint OneRequestPerLocker
        unique event LockerAssigned
        message "The locker is already assigned"
      constraint OneLockerPerRequest
        unique requestId on LockerAssigned
        message "The request already has a locker"
      specification AssigningALocker
        given caller
          authenticated
          role "Attendant"
        given LockerInstalled
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          number = "B-07"
          volume = 120
        given LockerRequested
          for "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          preferredVolume = 100
          requestedAt   = "2026-03-02T10:00:00Z"
        when AssignLocker
          lockerId        = "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
        then LockerAssigned
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
      specification RefusingACustomerAssigningALocker
        given caller
          authenticated
          role "Customer"
        when AssignLocker
          lockerId  = "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
        then denied
      specification RejectingASecondAssignmentOfALocker
        given caller
          authenticated
          role "Attendant"
        given LockerInstalled
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          number = "B-07"
          volume = 120
        given LockerRequested
          for "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          preferredVolume = 100
          requestedAt   = "2026-03-02T10:00:00Z"
        given LockerRequested
          for "5a4b3c2d-1e0f-4a9b-8c7d-6e5f4a3b2c03"
          preferredVolume = 90
          requestedAt   = "2026-03-02T10:05:00Z"
        given LockerAssigned
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
        when AssignLocker
          lockerId        = "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "5a4b3c2d-1e0f-4a9b-8c7d-6e5f4a3b2c03"
        then error "The locker is not free"
      specification AppendingASecondAssignmentOfALocker
        given LockerInstalled
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          number = "B-07"
          volume = 120
        given LockerRequested
          for "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          preferredVolume = 100
          requestedAt   = "2026-03-02T10:00:00Z"
        given LockerRequested
          for "5a4b3c2d-1e0f-4a9b-8c7d-6e5f4a3b2c03"
          preferredVolume = 90
          requestedAt   = "2026-03-02T10:05:00Z"
        given LockerAssigned
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
        when append LockerAssigned
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "5a4b3c2d-1e0f-4a9b-8c7d-6e5f4a3b2c03"
        then error "The locker is already assigned"

    slice StateView RequestProgress
      description "A customer checks whether their request has been given a locker. LockerRequested sets requestId, preferredVolume and state=waiting: the request enters the view. LockerAssigned sets state=assigned and lockerId: the customer learns which locker to use. Open question: limiting this to the caller's own request needs the customer recorded on LockerRequested and a caller-scoped query."
      readmodel RequestStatus
        requestId RequestId
        preferredVolume Litres
        state         RequestState
        lockerId        LockerId optional
      query RequestById => RequestStatus optional
        by requestId RequestId
      projection RequestProgress => RequestStatus
        from LockerRequested
          requestId = $eventSourceId
          preferredVolume = preferredVolume
          state         = "waiting"
        from LockerAssigned key requestId
          state  = "assigned"
          lockerId = $eventSourceId
      screen MyRequest
        title "My locker request"
        data RequestStatus via query RequestById
        action RequestLocker
      specification FollowingARequestToAssignment
        given caller
          authenticated
        given LockerInstalled
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          number = "B-07"
          volume = 120
        given LockerRequested
          for "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          preferredVolume = 100
          requestedAt   = "2026-03-02T10:00:00Z"
        given LockerAssigned
          for "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
          requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
        then query RequestById
          arguments
            requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
          result
            state  = "assigned"
            lockerId = "6d1f0a52-4b8e-4c33-9d0e-1a2b3c4d5e01"
      specification RefusingAnAnonymousRequestLookup
        given caller
        then query RequestById
          arguments
            requestId = "0c9e7b1d-2f3a-4e5b-8c6d-7e8f9a0b1c02"
        then denied
```
