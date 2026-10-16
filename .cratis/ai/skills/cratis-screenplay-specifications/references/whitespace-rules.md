<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/whitespace-rules.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Whitespace-only rule rejection

## Contents

- Product sources
- Complete model

Text `not empty` rejects null, empty text and Unicode `White_Space`-only text
for concept and command property rules. The reference execution rejects them for
**every ESM version**, without changing canonical bytes. Existing
specifications expecting whitespace acceptance now fail. Keep the rule and
correct its expected outcome, not its input to hide the rejection.

U+0085 and U+00A0 are whitespace; U+FEFF is not. Values are not trimmed:
`" a "` passes and remains unchanged. JavaScript `trim()` is not the contract.
Collections need only one element (even an empty or blank element); identity
text (event-source and stream-id parts) still treats whitespace as a value.
Null command/event fixture values are not admitted in source specifications;
the null check has evaluator-source evidence, not a fabricated passing fixture.

## Product sources

Cratis/Screenplay:
- `Source/DotNET/Screenplay/Semantics/Execution/SemanticEvaluator.cs`.
- `Documentation/screenplay/commands.md`, `concepts.md` in the same folder.
- `Source/DotNET/Screenplay.CanonicalCorpus/Corpus/NotEmptyWhitespace/source.play`
  supplies the complete model below; only the explanatory Unicode comments are added.
- `Source/DotNET/Screenplay/Semantics/Execution/for_SemanticEvaluator/when_validating_not_empty/with_empty_text.cs`,
  `with_non_whitespace_text.cs` and `with_a_collection.cs` in the same folder
  check concept/property rejection, U+FEFF acceptance and collection boundaries
  across versions. Decision 0071 documents unchanged identity text (`:37-41,79`).

## Complete model

The visually blank literals in `RejectingNextLine...` are U+0085; those in
`AcceptingByteOrderMark...` are U+FEFF. Preserve their exact code points.

```screenplay
concept Name : String
  validate
    not empty message "Name is required"
module Names
  feature Registration
    slice StateChange RegisterConcept
      command RegisterConcept
        id String identifier
        name Name
        produces NameRegistered
          for id
          name = name
      event NameRegistered
        name Name
      specification RejectingWhitespaceOnAConcept
        when RegisterConcept
          id = "name-1"
          name = "   "
        then error "Name is required"
      specification RejectingNextLineOnAConcept
        when RegisterConcept
          id = "name-1"
          // U+0085 NEXT LINE
          name = ""
        then error "Name is required"
      specification AcceptingNonWhitespaceOnAConcept
        when RegisterConcept
          id = "name-1"
          name = " a "
        then NameRegistered
          for "name-1"
          name = " a "
      specification AcceptingByteOrderMarkOnAConcept
        when RegisterConcept
          id = "name-1"
          // U+FEFF ZERO WIDTH NO-BREAK SPACE
          name = "﻿"
        then NameRegistered
          for "name-1"
          name = "﻿"
    slice StateChange RegisterProperty
      command RegisterProperty
        id String identifier
        name String
        validate
          name not empty message "Name is required"
        produces TextRegistered
          for id
          name = name
      event TextRegistered
        name String
      specification RejectingWhitespaceOnAProperty
        when RegisterProperty
          id = "name-1"
          name = "   "
        then error "Name is required"
      specification RejectingNextLineOnAProperty
        when RegisterProperty
          id = "name-1"
          // U+0085 NEXT LINE
          name = ""
        then error "Name is required"
      specification AcceptingByteOrderMarkOnAProperty
        when RegisterProperty
          id = "name-1"
          // U+FEFF ZERO WIDTH NO-BREAK SPACE
          name = "﻿"
        then TextRegistered
          for "name-1"
          name = "﻿"
```

Save the fenced model as `whitespace-rules.play`, then run
`screenplay test whitespace-rules.play --format json` with the standalone pin.
All seven specifications must pass; the corpus gate executes the complete fence.
A passing reference result does not prove that an older CLI-bundled compiler or
Stage/Arc targets have adopted the Unicode correction. Their own specifications
must establish parity.

Requires Screenplay 4.151.0 or later (`screenplay --version`) for the Unicode `not empty` correction.
