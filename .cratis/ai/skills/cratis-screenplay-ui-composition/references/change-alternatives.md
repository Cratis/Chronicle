<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/change-alternatives.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Choose an action list by the item a change carries

Requires Screenplay 4.251.0 (`screenplay --version`); older compilers refuse
alternatives on `on change` with `PLAY0560`. `on change` accepts the same block-form
`when`/`otherwise` alternatives as click, double click and select, and `item` is the
item that changed, with the changed property already holding its new value. The first
matching `when` runs once; with no match the `otherwise` list runs, or nothing runs when
there is none; a change with no item runs nothing, including the `otherwise` list.
Compiler admission is not proof that a Stage renderer executes the gesture.

An opaque `where` on `on change` stays valid and is not deprecated by `PLAY0564`, which
applies only to click, double click and select. Do not mix `where` with alternatives.

This complete model compiles on the standalone compiler from 4.251.0. The fence is an
excerpt only because the standalone verification pin predates that release.

```screenplay excerpt
module Conference
  feature Review
    slice StateView TalkBoard
      event TalkSubmitted
        talkId String
        status String
      readmodel TalkSummary
        talkId String
        status String
      projection Talks => TalkSummary
        from TalkSubmitted
          talkId = $eventSourceId
          status = status
      query AllTalks => TalkSummary[]
      command AcceptTalk
        talkId String
      screen ReviewBoard
        data TalkSummary[] via query AllTalks
        component Cratis.Blueprint.Default.kanbanBoard talkBoard
          property cards from query AllTalks
          property cardId = "talkId"
          property cardColumn = "status"
          on change
            when item.status == "accepted"
              execute AcceptTalk
                with talkId from item.talkId
            otherwise
              notify warning "A talk cannot move back to Submitted"
```

The opaque form keeps compiling on the same board, in place of the `when` list:

```screenplay excerpt
          on change
            where item.status == "accepted"
            execute AcceptTalk
              with talkId from item.talkId
```
