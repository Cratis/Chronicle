---
name: cratis-writing-voice-and-cadence
description: Review and revise existing prose so it reads as written by a person, by finding overused constructions and stock phrasing in one piece and structural sameness across a batch, then fixing both with specifics instead of injected variety, without changing any technical claim. Flags are editing prompts and never a verdict on who wrote something. Use when reviewing or rewriting documentation, release notes, pull request descriptions, posts or reviews for voice. Do not use for technical accuracy review, original drafting, authorship detection, or as a reason to touch approved copy without authorization.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-writing-voice-and-cadence/SKILL.md -->

# Writing voice and cadence

Material that is factually correct can still read as if nobody wrote it. The usual diagnosis
is "it sounds like AI wrote it", and the usual response is to rewrite by feel. That tends to
produce a second machine voice: new stock phrases in place of the old ones, a joke dropped in,
a sentence chopped short for rhythm.

This skill works from what can be counted and what can be checked. Three things go wrong:
a handful of constructions recur often enough to be worth a second look, a body of work
falls into one shape, and underneath both, something only the author knew is missing.
It revises existing material. Drafting belongs to the skill for the content type:
**cratis-social-feed-post** for feed posts, **cratis-release-notes** for release notes,
**cratis-technical-examples** for code samples, **cratis-documentation-writing** for pages.

The full catalog of patterns is in [references/ai-tells.md](references/ai-tells.md), with a
word list or regex and a batch threshold for each, and the studies behind them. IDs such as
HV-02 below point to that file's Sources table.

## This is an editing aid, not a detector

Read this before running any check.

People are poor at telling AI text from human text. In six experiments with 4,600
participants, readers could not reliably pick out AI-generated self-presentations, and the
cues they leaned on, first-person pronouns and contractions among them, were flawed (HV-01,
HV-02). In a separate paired-essay study, instructors identified the ChatGPT essay 70% of the
time; subject expertise did not reliably improve performance (HV-03). Studies do
find differences between human and model text, but in aggregate over many texts and within
the genres and models they tested (HV-05, HV-06). None of that says who wrote a particular
paragraph.

So these rules hold for every use of the skill:

- A flag means "read this passage again". It never means "AI wrote this".
- Never use a flag, a count or a detector score to accuse a person, grade them, or block
  their work. Don't combine flags into an "AI score".
- A contributor writing in a second or third language has their own rhythm. Fix what is
  unclear to the reader. Don't push them toward an arbitrary house style because a pattern
  list matched.
- Contractions, "I", humor, short sentences and typos prove nothing about who wrote a text
  (HV-02). Adding them so copy passes as human is the same mistake in reverse.
- A construction that carries a real distinction stays, however often the catalog lists it.

## The constructions

None of these is wrong. A single corrective tail can be the clearest sentence in the piece.
What turns a construction into a tell is recurrence: the same move three times in a short
post, or in the same position in every post of a series.

| Construction | What it looks like |
| --- | --- |
| **Corrective tail** | "…, not a fresh count." The sentence ends by correcting itself. "It's not X, it's Y" is the same reflex |
| **Balanced semicolon** | Two clauses of equal weight either side of a semicolon |
| **"rather than"** | The same correction carried by a different connective |
| **Em-dash pivot** | The dash used to swing into a closing clause |
| **"That is what X is for"** | A pointer sentence standing in for an explanation |
| **Punchline fragment** | A two- or three-word sentence landing a slogan: "They run." |
| **Signpost pair** | "That's the X. Here's the Y." Announcing the structure instead of having one |
| **Labeled caveat** | "One honest limit:" A limitation parked in its own closing paragraph |
| **Aphoristic close** | A tidy maxim as the last line, restating what the piece already said |
| **Reflexive triplet** | Three parallel items where the thought had one or two |

Vocabulary is a weaker signal than it looks. "Delve", "underscore" and "showcase" rose
sharply in a large corpus of biomedical abstracts (HV-10), and a published editors' guide
lists phrasing like "evolving landscape" as a possible sign (HV-25). The same guide says its
signs are observations that prove nothing on their own (HV-23). Use the word lists to find a
vague word and put the specific one in its place. When "robust" means what an engineer means
by it, leave it.

## Review a single piece

1. Read it aloud once, start to finish. Most of these constructions are audible long before
   they are countable.
2. Run the single-piece checks from the catalog: the word lists, the construction patterns,
   list shape, opening and close. Record each match with its sentence, so whoever reads the
   report can judge it in context.
3. For each flag, ask what the sentence is standing in for. A significance word usually
   covers a missing measurement. A reflexive "X, not Y" often means nobody said what X does.
   A caveat in the last paragraph usually belongs inside an earlier sentence.
4. Mark the passages that need something only the author has: a reason, a version, a
   result. Those go back to the author as questions.

## Structural sameness is the more damaging half

A reader meeting the second piece in a series can recognize the format before reading a word
of it, and a per-piece review can't see that. Put ten comparable pieces side by side, same
channel and same content type, and compare openings, shapes, lengths and closes.

Openings come first. Classify each first sentence as an event, a problem, a question, direct
address, a number, an identifier, or an abstract assertion ("A registration test can…"). Six
of ten opening the same way, or three that share their first two words, is an uncalibrated
trigger to inspect the batch for a template.

Then paragraph shapes: how many paragraphs, and how much each weighs. When most pieces run
three or four paragraphs of about the same size, the format is doing the writing. Length
across the batch tells the same story. Pieces on very different subjects shouldn't all land
within a few words of each other.

Last, closes. Label each last paragraph as a result, a next step, an open question, a limit,
a summary or a maxim. A summary or maxim in most of the batch is a template. So is the same
labeled caveat at the bottom of every piece.

Within a piece, sentence length that barely moves reads as a monotone. Compare it with the
rest of your own corpus; there is no correct spread, and short reference entries or numbered
steps are allowed to be even.

The fix for a template is not a better template. Swapping every abstract opening for a
question gives you ten question openings. Flag concentration in the batch, never one outlier,
and don't adopt a target shape. With fewer than five comparable pieces, say so and skip the
batch conclusions.

## Fix with substance

When a piece reads as generated, what is usually missing is a fact. Varying sentence length
on purpose or shuffling paragraph counts treats the symptom and grows a new pattern. Language
models struggled to match the range of human stylistic variation in the genres studied
(HV-06). Let differences in subject and purpose determine the shape, and add whatever makes
this piece different from the last one:

- the specific artifact: the command, the event type, the file, the error text, the
  before-and-after;
- the named thing in place of the category: "the retry loop in the importer" for "complex
  workflows";
- a reason the author owns, in words they would say out loud, supplied by them;
- the limit, placed in the sentence it limits: "On macOS and Linux, the installer…".

Where a sentence has nothing to add, cut it. Concreteness has a ceiling too. In historical
headline experiments, extra concreteness helped headlines that were too vague and hurt ones
that were already very concrete (HC-03). Packing identifiers into an opening won't rescue it.

If the source material doesn't contain the substance, don't supply it yourself. Ask the
author, or leave the passage plain and accurate.

## What a person leaves behind

These are signs that someone who knows the work was involved. Look for them in review and
note which ones are missing. Never fabricate one to make copy read better; a fabricated
detail is worse than an absent one.

- a named artifact the reader can open or run
- the exact version or environment, where it changes the outcome
- an engineering task a reader would recognize from their own week
- an observation kept apart from the inference drawn from it
- a credible counterexample, or the simpler alternative the author considered
- a material limit sitting in the sentence it qualifies
- a reason for a judgment, owned by the author
- a next step the reader can reproduce
- variation that follows from the subject

PostHog's published voice guide argues for much the same list: concrete nouns, directness,
candor about limitations (DP-05). That is one company's judgment, with no measured effect.

## Hedging that protects the writer

The other common complaint is "wishy-washy": copy so qualified that it never commits to
anything. It usually comes from review language leaking into the material. Evidence,
verification and approval wording belongs in the review record, and so do qualifiers that
protect the writer without informing the reader. The claim rule below protects every hedge
that is true and would change what the reader does. It does not protect review residue: move
the evidence to the review record and state the capability plainly.

## Who is speaking, and to whom

None of these choices makes text sound human by itself. Each depends on the piece.

Instructions usually read best addressed to the person following them: "run", "you'll see".
Reference pages and explanations often don't need "you" at all, and forcing it in makes them
chatty. Use it where someone is being told what to do.

A company account is a team talking when it reports what the team did: "we shipped", not the
company name in the third person. When the subject is how the product behaves, the product
can be the subject.

Writing under a person's name can carry opinion, judgment and preference, and those belong to
the named author. Prose under a byline in the impersonal third person reads like
documentation with a name on it. How personal it gets is the author's call.

A question earns its place when the argument raised it and the author wants the answer. One
bolted on to collect replies is bait.

## First person is not a license to invent

Never invent experience, anecdotes, opinions, customers, incidents, benchmarks or quotes. That
applies to a named founder, a company account, and the vague "one team we worked with". A
hypothetical example is labeled as one.

Copy in the first person needs the named author's own input: the detail, the opinion and the
reason come from them. It also needs their sign-off on the final wording, and until it has
both it stays a draft. A meaningful edit to first-person copy goes back to them as well.
Spotting the gap is part of the review ("this paragraph needs the real reason you picked
PostgreSQL"). Filling it in on the author's behalf is out of bounds.

## Channel policies on AI-assisted writing

LinkedIn has stated its position (HV-18, HV-19):

> It's ok to use AI to help you write, but your posts and comments need to represent your
> voice and your perspectives.

> When content appears to be generated by AI and lacks clear perspective, it is less likely
> to be widely distributed beyond a person's immediate network.

This is LinkedIn's own account of its system, and it hasn't been independently verified.
Read the two sentences together. Help with language is allowed when the post is the member's
own view, and what LinkedIn says it limits is content that looks generated *and* has no clear
perspective. Neither sentence says AI help alone costs reach. Elsewhere the evidence on how
readers react to AI assistance is mixed: a preprint, not peer reviewed, found some assistance
raised engagement while lowering perceived authenticity (HV-11), and a review of 47
journalism studies found no consistent trust penalty (HV-12). For a reviewer, "does this say
something the author actually thinks?" matters more than any word list.

Hacker News draws a harder line. The comment section of its guidelines says: "Don't post
generated text or AI-edited text. HN is for conversation between humans." (HC-16)

The rule is written for comments. Cratis applies it, as its own stricter policy, to
everything it posts there: nothing Cratis posts to Hacker News is written or edited by AI,
whether title, text or comment. Don't run a voice pass on HN copy. You can help the person
check facts and links.

## Never trade a claim for a smoother sentence

This is the part that makes a voice pass dangerous, and the reason to do one carefully
instead of quickly.

The risk is not an obvious rewrite. It is a hedge dissolving into nicer phrasing. "There is
no need to" becomes "without needing to", and a limit quietly widens. "Not its meaning"
becomes "and leaves its meaning alone", and a distinction softens. Both read better. Both
changed what the text asserts.

Before accepting any rewrite, confirm that:

- every identifier and every number present before is still present, and none was invented;
- every hedging word (not, never, only, may, unless, until) survives in substance;
- every stated limitation survives, including one that reads as an awkward caveat, because it
  is a caveat and it is deliberate;
- the closing limitation or evidence statement is not thinned, relocated into a subordinate
  clause, or dropped because it spoiled the ending.

A mechanical before-and-after check over identifiers, numbers and hedge words catches most of
this before anyone reads a word. The catalog describes one. A person still reads every
flagged pair, because a count can't tell a widened limit from a harmless rephrase.

## Work in tranches that can be read

Rewriting is not a batch operation. Any body of material large enough to have a detectable
voice is too large to rewrite in one pass with judgment, and a fast pass replaces one
detectable format with another. Rewrite a tranche, read it, and only then continue.

Where material carries a recorded approval, a rewrite invalidates it. Recording a fresh
approval over copy nobody has read makes the record assert something false, after which every
downstream check agrees with it. Size the tranche to what its owner will actually read.

## Stop conditions

Stop and hand back when:

- a rewrite would change what the material claims;
- the named author hasn't supplied or accepted a perspective, detail or opinion being put in
  their mouth;
- the material is under an approval the rewrite would invalidate, and nobody has authorized a
  refresh;
- the request is to judge whether a person used AI, or to gate their work on a flag count;
- the copy is for Hacker News.

Reviewing or revising copy never includes publishing, scheduling, posting or replying. An
agent hands the text back; a person decides what goes out.
