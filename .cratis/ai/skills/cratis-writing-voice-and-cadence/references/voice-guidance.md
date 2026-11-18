<!-- cratis-ai-managed: skills/cratis-writing-voice-and-cadence/references/voice-guidance.md -->
# Voice and cadence guidance

## Contents

- Structural sameness is the more damaging half
- Fix with substance
- What a person leaves behind
- Hedging that protects the writer
- Who is speaking, and to whom
- Channel policies on AI-assisted writing

Detail for the revision workflow in `SKILL.md`: batch structure, substance,
speaker and channel questions. The stop conditions, claim-preservation rules and
first-person rules stay in `SKILL.md`; read those first.

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

Where a sentence has nothing to add, cut it. Concreteness has a ceiling too. In past
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
