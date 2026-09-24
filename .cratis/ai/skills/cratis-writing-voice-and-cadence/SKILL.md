---
name: cratis-writing-voice-and-cadence
description: Find and remove the sentence constructions and structural sameness that make written material read as machine-generated, across documentation, release notes, pull request descriptions, posts and reviews, without changing any technical claim. Use when reviewing or rewriting existing prose for voice. Do not use for technical accuracy review, for original drafting, or as a reason to touch approved copy without authorization.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-writing-voice-and-cadence/SKILL.md -->

# Writing voice and cadence

Material that is factually correct can still be unreadable in a way nobody can name. The
usual diagnosis is "it sounds like AI wrote it", and the usual response is to rewrite by feel,
which produces a second machine voice rather than a human one.

The failure is measurable, and it has two halves. The smaller half is a set of sentence
constructions used far past the density a person would use them. The larger half is
structural sameness across a whole body of material: a reader meeting the second piece
recognizes the format before reading a word of it.

## The constructions

None of these is wrong. One of any of them is a good sentence. What turns them into a tell is
recurrence.

| Construction | What it looks like |
| --- | --- |
| **Corrective tail** | "…, not a fresh count." The sentence ends by correcting itself |
| **Balanced semicolon** | Two clauses of equal weight either side of a semicolon |
| **"rather than"** | The same correction carried by a different connective |
| **Em-dash pivot** | The dash used to swing into a closing clause |
| **"That is what X is for"** | A pointer sentence standing in for an explanation |
| **Punchline fragment** | A two- or three-word sentence landing a slogan: "They run." |
| **Signpost pair** | "That's the X. Here's the Y." Announcing the structure instead of having one |
| **Labeled caveat** | "One honest limit:" A limitation parked in its own closing paragraph |
| **Aphoristic close** | A tidy maxim as the last line, restating what the piece already said |
| **Reflexive triplet** | Three parallel items where the thought had one or two |

To measure rather than guess, count them. Searching for `, not a`, `; ` between two clauses,
`rather than`, and an em-dash followed by a short closing clause takes a few minutes across a
directory and turns an argument about taste into a number. Reading a draft aloud catches all
of them faster than any tool.

## Structural sameness is the more damaging half

Look at ten pieces together, not one at a time:

- **Every piece the same length and shape.** If most of them are three or four paragraphs
  with the same paragraph weights, that is a template.
  The fix for a template is not a better template.
  Do not adopt a target shape: vary the paragraph count, and let paragraphs differ visibly
  in weight.
- **Every sentence the same length.** When sentence length barely varies within a piece, the
  result is a monotone regardless of the words. Let it swing between four words and twenty-five.
- **Every piece opening the same way.** An abstract noun phrase making a declarative
  assertion — "A screen that needs…", "A registration test can…" — is one move. Alternate it
  with a question, a direct address, a concrete moment, or a number.

## Hedging that protects the writer

The other common complaint is "wishy-washy": copy so qualified that it never commits to
anything. It usually comes from review language leaking into the material: evidence,
verification and approval wording that belongs in the review record, and qualifiers that
protect the writer without informing the reader.

The rule below protects every hedge that is true and would change what the reader does. It
does not protect review residue. Move the evidence to the review record, state the capability
plainly, and put a real limit inside the sentence it limits ("on macOS and Linux, …") instead
of in a separate disclaimer.

## Address somebody

- **Say "you".** Material that tells a reader what to do should address that reader.
- **A company account is a team talking.** Write "we", the way the people who built it would
  say it, not the company name in the third person.
- **Attributed writing should sound attributed.** Prose published under a person's name and
  written entirely in the impersonal third person reads as documentation wearing a byline.
  Opinion, judgement and preference belong to the named author and are theirs to give.
- **First person is not a licence to invent.** Opinion is allowed; invented experience,
  anecdotes that did not happen, and claimed observations the author did not make are not.
  Where a named author owns the material, it stays blocked until that author reviews it.
- **A question is allowed when the piece has earned it.** A question that follows from the
  argument is not bait; one bolted on to harvest a response is.

## Never trade a claim for a smoother sentence

This is the part that makes a voice pass dangerous, and it is the reason to do one carefully
rather than quickly.

The risk is not an obvious rewrite. It is a hedge dissolving into nicer phrasing. "There is
no need to" becomes "without needing to", and a limit quietly widens. "Not its meaning"
becomes "and leaves its meaning alone", and a distinction softens. Both read better. Both
changed what the text asserts.

Before accepting any rewrite, confirm that:

- every identifier and every number present before is still present, and none was invented;
- every hedging word — not, never, only, may, unless, until — survives in substance;
- every stated limitation survives, including one that reads as an awkward caveat, because it
  is a caveat and it is deliberate;
- the closing limitation or evidence statement is not thinned, relocated into a subordinate
  clause, or dropped because it spoiled the ending.

A mechanical check over identifiers, numbers and hedge counts catches most of this before a
human reads a word, and is worth writing once for any corpus large enough to need a pass.

## Work in tranches that can be read

Rewriting is not a batch operation. Any body of material large enough to have a detectable
voice is too large to rewrite in one pass with judgement, and a fast pass replaces one
detectable format with another. Rewrite a tranche, read it, and only then continue.

Where material carries a recorded approval, a rewrite invalidates it. Recording a fresh
approval over copy nobody has read makes the record assert something false, after which every
downstream check agrees with it. Size the tranche to what its owner will actually read.

## Stop conditions

Stop when a rewrite would change what the material claims, when the named author has not
accepted a perspective being introduced on their behalf, or when the material is under an
approval that the rewrite would invalidate without authorization to refresh it.
