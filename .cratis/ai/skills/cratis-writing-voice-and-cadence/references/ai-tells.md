<!-- cratis-ai-managed: skills/cratis-writing-voice-and-cadence/references/ai-tells.md -->
# AI-associated writing patterns

This is the full catalog behind the **cratis-writing-voice-and-cadence** skill: patterns that
make writing read as machine-produced, how a script can find each one, and what a person
would usually write instead. Sources were checked in September 2026. Bracketed IDs such as
HV-10 point to the [Sources](#sources) table at the end of this file.

Read the skill's non-detector policy before using any of this. Every check below finds a
passage for a person to reread. None of them, alone or combined, tells you who wrote a text.
Readers do poorly at that task (HV-01, HV-03), the cues they rely on are unreliable (HV-02),
and the studies that do find differences between human and model text find them in aggregate
over many texts, within the genres and models they tested (HV-05, HV-06). Detector vendors
publish prevalence figures, but those are the vendor's own classifications of posts its
searches found, not verified authorship (HV-21, HV-22).

## How much weight each entry carries

Most of this catalog is editorial. The "Basis" line on each entry says which of three kinds
it is.

"Study" means a verified study found the pattern elevated in model text, in aggregate, in a
specific genre. It still can't classify one piece. "Guide" means a published editors' guide
lists it as a possible sign, and that guide says its signs are context-dependent
observations that prove nothing on their own (HV-23). "Editorial" means no verified study backs it as an AI
signal. Those entries are here because they recur in generated copy and usually stand in for
missing substance, so judge them on that alone.

## Running the checks

- Keep the original text of each piece, with file, line and character offsets. Match the
  lexical, syntactic and tonal patterns against a prose view that leaves out front matter,
  fenced and inline code, URLs, block quotes and quoted strings, because code and quotations
  aren't prose the writer chose. Map every match back to its original offset.
- Some checks need what the prose view leaves out, so they read the original. L4 looks for a
  link, citation or named source in the original sentence. T3 classifies the original first
  sentence, backtick spans and calls included. L1, L5 and T5 read the original sentence to
  decide whether it has a number, identifier or code. N5 compares the complete before and
  after versions.
- Treat straight (`'`) and curly (`’`) apostrophes alike. Patterns that depend on a
  contraction or possessive use `['’]`. A check that normalizes apostrophes does it in a
  comparison copy and keeps the original text and offsets.
- Patterns are JavaScript regular expressions. Use the `i` flag unless the entry says
  otherwise, `g` to count, `m` for line-anchored patterns, and `u` where noted.
- Report each match with its sentence, file and line. Don't sum matches into a score, don't
  label a piece "AI", and don't rewrite automatically.
- The thresholds are starting defaults nobody has calibrated. Before relying on one, run it
  over material you know people wrote and look at what it flags.
- A batch means ten comparable pieces: same channel, same content type. With fewer than five,
  report per-piece results and skip the batch checks.
- A material limitation is never a cadence problem. If a check flags a sentence that states a
  real limit, the fix is to move or rephrase it, never to drop it.

## Lexical

### L1. Inflated significance

Words that assert importance without showing any: *pivotal*, *groundbreaking*,
*transformative*, *game-changing*, *a testament to*, *plays a key role*.

```text
\b(?:pivotal|groundbreaking|transformative|revolutioni[sz](?:e|es|ed|ing)|game[- ]chang(?:er|ers|ing)|(?:a |stands as a )?testament to|plays? an? (?:key|vital|crucial|pivotal|central) role)\b
```

Flag two or more in one piece, or one in a sentence with no number, identifier or named
thing nearby. Replace the word with what it was pointing at: the behavior, the measurement,
the decision, and how far it reaches. Basis: guide (HV-25), with the HV-23 caveat.

### L2. Abstract scene-setting

Openings that set a stage nobody asked for: *in today's fast-paced world*, *the evolving
landscape*, *tapestry*, *the realm of*, *paradigm shift*.

```text
\b(?:(?:ever-?)?(?:evolving|changing|shifting) landscape|in today['’]?s (?:fast-paced|digital|modern|ever-changing) world|tapestry|(?:in|into) the realm of|paradigm shift)\b
```

Flag any match in the first two sentences, or the same phrase in two or more pieces of a
batch. Start instead with the engineering question or the symptom somebody saw. Basis:
guide for "evolving landscape" (HV-25); editorial for the rest.

Fixtures: the pattern matches "In today's digital world" and "In today’s digital world",
and does not match "Today’s release fixes the importer".

### L3. Polished filler verbs and adjectives

*Delve*, *underscore* and *showcase* rose sharply in one large corpus of biomedical abstracts
after 2023 (HV-10); the same analysis estimated that at least 13.5% of 2024 abstracts were
processed with a language model (HV-09). Neither result was measured on developer writing or
social posts. *Robust*, *seamless*, *leverage*, *unlock*, *elevate*, *empower* and
*streamline* are unvalidated clichés in the same family.

```text
\b(?:delv(?:e|es|ed|ing)|underscor(?:e|es|ed|ing)|showcas(?:e|es|ed|ing)|robust(?:ly|ness)?|seamless(?:ly)?|leverag(?:e|es|ed|ing)|unlock(?:s|ed|ing)?|elevat(?:e|es|ed|ing)|empower(?:s|ed|ing)?|streamlin(?:e|es|ed|ing))\b
```

Count per 1,000 words and show every match in context. Starting default: flag three or more
per 1,000 words, or one stem that recurs across three or more pieces in a batch. Use the
direct verb and its object: "runs this query", "fails on replay". Leave a term alone when it
carries its technical meaning, as in robust statistics or a lever in a physical system.
Basis: study for the first three (HV-10); editorial for the rest.

### L4. Unattributed consensus

*Experts say*, *studies show*, *developers want*, *everyone knows*.

```text
\b(?:experts|studies|research|developers|engineers|everyone|many (?:people|teams)|most (?:people|teams|developers))\s+(?:say|says|show|shows|agree|know|want|believe|suggest|suggests)\b
```

Flag any match whose sentence has no link, citation or named source. Name the study, the
survey question and its population, or present the point as the author's opinion. Basis:
editorial.

### L5. Empty assurances

*Easy*, *simple*, *secure*, *production-ready*, *best-in-class*, *blazing fast*.

```text
\b(?:easy|easily|simple|simply|effortless(?:ly)?|secure|production[- ]ready|best[- ]in[- ]class|enterprise[- ]grade|blazing(?:ly)?[- ]fast)\b
```

Flag when the sentence has no prerequisite, version, threat model or comparison. Describe the
path that was actually verified and where it stops working. Any claim of security or
readiness also has to clear the owning product's claim review; rewording doesn't. Basis:
editorial; PostHog's voice guide makes the same argument for specifics and candor about
limits (DP-05).

## Syntactic

### S1. Corrective tail and reflexive contrast

A sentence that ends by correcting itself ("…, not a fresh count."), the "not only X but Y"
frame, the "It's not X, it's Y" reflex, and "rather than" doing the same job.

```text
,\s+not\s+[^,.;:!?]{1,40}[.!?]
\bnot\s+(?:only|just|merely)\b[^.!?]{1,80}\bbut\b
\b(?:it|this|that)(?:'s|’s|\s+is)\s+not\b[^.!?,;:—–]{1,60}[,;:—–.]\s*(?:it|this|that)(?:'s|’s|\s+is)\b
\brather\s+than\b
```

Starting default: flag two or more in a piece under 300 words, four or more per 1,000 words
in longer ones, or the same construction closing a paragraph in five or more pieces of a
batch. State what the thing does first. Keep a contrast when the reader would otherwise
confuse the two things. Basis: editorial.

### S2. Balanced semicolon and em-dash pivot

Two clauses of equal weight on either side of a semicolon, and a dash that swings into a
short closing clause.

```text
\b\w+;\s+[a-z]
(?:\s?[—–]\s?|\s--\s)[^—–.!?\n]{1,40}[.!?](?=\s|$)
```

Presence means nothing; people use both. Compare density and position across the batch.
Flag three or more in one short piece, or the same construction in the last sentence of a
paragraph in more than half the batch. Replace it with the connective that says what the
relationship is: because, so, unless, which means. Basis: editorial.

### S3. Participial tails and noun stacks

A sentence that trails a ", making it…" clause, and runs of nominalizations: "the
implementation of the normalization of the configuration".

```text
,\s+(?!(?:including|according|during|following|regarding|depending|assuming|using|excluding|starting)\b)\w{3,}ing\b
\b\w{3,}(?:tion|sion|ment|ity|ance|ence|ness)s?\b
```

Flag a piece where more than a third of sentences end in a participial tail. Count the second
pattern per sentence and flag sentences with four or more. Put the actor and the action in a
finite verb and keep the technical terms that matter. Basis: a corpus study found aggregate
grammatical differences between human and model text (HV-05), limited to its genres and
models; the specific constructions here are an editorial reading.

### S4. Reflexive triplets and mirrored clauses

Three parallel items where the thought had one or two, and runs of sentences built to the
same template.

```text
\b[\w'-]+(?:\s+[\w'-]+){0,2},\s+[\w'-]+(?:\s+[\w'-]+){0,2},?\s+(?:and|or)\s+[\w'-]+
```

The pattern is noisy by design. Flag three or more triplets in a piece under 500 words, or
one in each of three adjacent paragraphs. For mirrored clauses, split into sentences and flag
three consecutive sentences that start with the same word, or whose word counts sit within
two of each other. Keep every item that is independently useful and cut the ceremonial third.
Basis: editorial.

### S5. Punchline fragment

A two- or three-word sentence landing a slogan: "They run." "Every time."

No regex. Split paragraphs into sentences on `(?<=[.!?])\s+`, then flag sentences of one to
three words that end a paragraph or follow a sentence of twelve words or more. Skip headings,
list items and code. Flag two or more in a piece, or a paragraph-final fragment in three or
more pieces of a batch. Fold the point into the sentence it punctuates, or cut it if that
sentence already made it. Basis: editorial.

### S6. Self-answered questions, signposts and pointer sentences

"Why bother? Because…", "Here's the thing", "That's the X. Here's the Y.", "Let's dive in",
and "That is what X is for" standing in for an explanation.

```text
\?\s+(?:Because|Simple|Easy|Here(?:'s|’s| is)|The answer|It turns out|Turns out)\b
\b(?:Here(?:'s|’s| is) (?:the (?:thing|kicker|catch|twist|deal|point)|why|how|what)|Let(?:'s|’s| us) (?:dive|break (?:it|this) down|unpack))\b
\bThat(?:'s|’s| is) the [\w-]+\.\s+Here(?:'s|’s| is)\b
\bthat(?:'s|’s| is) (?:exactly )?what [^.!?]{1,40}\b(?:is|are) for\b
```

The third pattern is case-sensitive. One self-answered question can be fine. Flag two or more
per piece, or the same signpost in three or more pieces of a batch. Ask a question only when
it is still open; otherwise give the answer. Basis: editorial.

## Structural

These are mostly batch checks. A single piece can't be a template on its own.

### T1. One shape for every piece

For each piece, record paragraph count, word or character count, and the word count of each
paragraph. Compute the coefficient of variation of paragraph length (standard deviation
divided by mean).

Report each piece's figures as a description, not a flag. Don't flag a piece for where it
falls in a ranking: ranking any batch produces a bottom group, even when the writing is
varied and suits its purpose.

Flag only concentration across comparable pieces, and report it with its denominator, for
example "7 of 10 pieces have four paragraphs". The starting defaults are uncalibrated:

- one paragraph count covers seven or more of ten comparable pieces (70% of the batch);
- seven or more of ten comparable pieces fall within 10% of the batch's median word count
  while their subjects clearly differ. Flag this one for manual review.

With fewer than five comparable pieces, report the figures and draw no batch conclusion. Let
the argument decide the shape. Don't pick a new target shape. Basis: study for reduced
stylistic variation in the genres tested (HV-06); the thresholds are editorial.

### T2. Flat sentence length

Compute the standard deviation of sentence length in words for each piece and report it as a
description, not a flag. Exclude short reference entries and step lists, which are
legitimately even. Flag the batch only when flat pieces concentrate. The uncalibrated
starting default is a standard deviation under five words in seven or more of ten comparable
pieces, reported with its denominator. There is no correct number. With fewer than five
comparable pieces, draw no batch conclusion. Basis: as T1.

### T3. One opening for every piece

Classify each piece's first sentence, in this order:

- number: the first token contains a digit
- question: the sentence ends with `?`
- identifier: contains a backtick span, a `Name()` call or a CamelCase word
- direct address: starts with `You`, `Your` or `If you`, or with an imperative verb (manual)
- abstract assertion: matches `^(?:A|An|The)\s+\w+(?:\s+\w+)?\s+(?:is|are|can|needs?|has|should|will)\b`
- event or problem: anything else, confirmed by a reader

Flag the batch when one class covers six or more of ten pieces, or when three or more pieces
share their first two words. Open with a documented event, task or decision. A concrete
opening doesn't mean an invented scene: Dudycz's article on fixing event-sourcing bugs opens
on bad data every system accumulates, then a support incident (DA-03). Don't overcorrect
into density either. In historical headline experiments, added concreteness helped headlines
that were too vague and hurt ones that were already very concrete (HC-03), and linguistic
features predicted the winning variant only 54% of the time (HC-05). Basis: editorial, with
the headline studies as a limit on over-tuning.

### T4. Bold-label bullets, takeaway blocks and emoji bullets

```text
^\s*(?:[-*+]|\d+\.)\s+\*\*[^*\n]+\*\*
^\s*(?:#{1,6}\s*|\*\*)?(?:Key takeaways?|Takeaways?|TL;?DR|The bottom line|Why (?:this|it) matters)\b
^\s*(?:[-*+]\s+)?\p{Extended_Pictographic}
```

Use `m` for all three and `u` for the third. Flag three or more consecutive bold-label
bullets where each label is followed by a sentence of prose, any takeaway block in a piece
under 800 words, and emoji-prefixed bullets as a separate count. Reference lists, glossaries
and option tables are legitimate uses and are exempt. Use a list for a real procedure or
comparison and paragraphs for an argument. Basis: editorial.

### T5. Summary or maxim close

Apply to the first line of the last paragraph:

```text
^(?:In (?:summary|conclusion|short)|To (?:sum up|summarize|conclude)|Overall|Ultimately|All in all|The (?:lesson|takeaway|bottom line)(?: here)? is)\b
```

Also flag a last sentence of twelve words or fewer that has no digit, identifier or code and
shares two or more content words with the first paragraph: that is usually a restatement
dressed as a maxim. Flag the batch when summary or maxim closes appear in five or more of ten.
End on a result, a real limit, the next step, or a question the author wants answered.
Basis: editorial.

### T6. Labeled caveat at the end

A limitation parked in its own closing paragraph: "One honest limit:".

```text
^(?:(?:One|A|The)\s+(?:(?:honest|important|real|big|key)\s+)?(?:limit|limitation|caveat|note|warning)|Caveat|Limitation|Note)s?\b[^:\n]{0,20}:
```

Flag any match in the last paragraph, and the batch when three or more pieces end this way.
Move the limit into the sentence it qualifies: "On macOS and Linux, the installer…". Never
delete it to improve the ending. Basis: editorial.

## Tonal

### N1. Generic optimism and absolute certainty

```text
\b(?:always|never|guaranteed?|every time|zero[- ](?:risk|downtime|config(?:uration)?)|thrilled|excited to|delighted|incredible|amazing|world[- ]class|next[- ]level|cutting[- ]edge|state[- ]of[- ]the[- ]art)\b|100%
```

Flag clusters of two or more in a paragraph and every absolute that isn't backed by a source.
"Never" and "always" are often real limits; check N5 before touching one. State what was
observed, what is uncertain, and which cases aren't supported. Basis: editorial.

### N2. Synthetic first-person experience

Confessions, lessons learned the hard way, and customer anecdotes.

```text
\b(?:I|we)\s+(?:was|were)\s+wrong\b|\b(?:I|we)\s+learned\s+(?:this\s+)?the\s+hard\s+way\b|\b(?:a|one of our)\s+(?:customers?|clients?|users?)\s+(?:told|asked|said|called|emailed)\b|\b(?:last|this)\s+(?:week|month|year),?\s+(?:I|we)\b
```

This isn't a language judgment. Every match goes to the named author, who confirms it
happened and points to something inspectable: the issue, the commit, the message. If they
can't, the passage is removed or labeled as a hypothetical. Basis: LinkedIn's stated policy
that posts should represent the member's own voice and perspectives (HV-18). The rest is an
ethical rule and makes no evidence claim.

### N3. Engagement bait

```text
\b(?:comment\s+["“]?\w+["”]?\s+(?:below|if)|agree\?|repost\s+if|tag\s+(?:someone|a\s+(?:friend|colleague))|who\s+else\b[^?\n]*\?|let\s+me\s+know\s+in\s+the\s+comments|thoughts\?)
```

Flag every match and review the intent. A closing question can be genuine; a request to
react isn't. Invite a specific counterexample, or ask something the author needs answered.
Basis: editorial.

### N4. Review language in the copy and writer-protecting hedges

Evidence, verification and approval wording that belongs in the review record, and stacked
qualifiers that protect the writer without informing the reader.

```text
\b(?:verified (?:against|by|in)|as (?:approved|reviewed)|(?:the )?evidence (?:shows|suggests|indicates)|per (?:the )?review|this has been (?:checked|validated))\b
\b(?:may|might|could|potentially|possibly|arguably|somewhat|in some cases|to some extent|it appears|it seems)\b
```

Flag every match of the first pattern. Count the second per sentence and flag two or more in
one sentence. Move review residue to the review record. Keep a qualifier when it is true and
would change what the reader does. Basis: editorial.

### N5. Meaning lost in the edit

This check compares a passage before and after rewriting. Run it on every rewrite, including
passages nothing flagged.

```text
\b(?:not|no|never|only|unless|until|without|except|may|might|must|cannot|can['’]t|won['’]t|don['’]t|doesn['’]t|isn['’]t|aren['’]t)\b
```

Run it on the complete original of both versions, never the prose view: the identifiers and
quoted values it compares sit in code spans and quotation marks. Extract from each version:
identifiers (backtick spans, CamelCase words, dotted names, paths), quoted values, numbers
(`\d[\d.,]*%?`), and the negation, limiter and modal words above. Normalize curly apostrophes
to straight ones in the extracted words, so a change of apostrophe style alone isn't
reported. Flag any item present before and missing after, and any number or identifier that
appears only after. A person then reads each flagged pair for meaning; counts can't show that
"there is no need to" has become "without needing to". Basis: editorial.

Run these fixtures before relying on the check. Each pair must give the result shown.

| Before | After | Expected |
| --- | --- | --- |
| The installer doesn't support Windows. | The installer supports Windows. | Flag: `doesn't` missing after |
| The installer doesn’t support Windows. | The installer supports Windows. | Flag: `doesn’t` missing after |
| The installer doesn’t support Windows. | The installer doesn't support Windows. | No flag |
| The installer doesn’t support Windows yet. | The installer doesn’t support Windows. | No flag |
| Retries stop after 3 attempts. | Retries stop after 5 attempts. | Flag: `3` missing, `5` only after |

## Sources

Only findings that passed verification are listed. The wording in the second column is the
verified claim, narrowed where the original summary overstated it.

| ID | What the source supports | Type | Source |
| --- | --- | --- | --- |
| HV-01 | In six experiments with 4,600 participants, people could not reliably detect AI-generated self-presentations in professional, hospitality and dating contexts. | Peer-reviewed, 2023 | [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC10089155/) |
| HV-02 | The same study identifies first-person pronouns and contractions as flawed heuristics for judging human authorship. Applies to the studied self-presentations. | Peer-reviewed, 2023 | [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC10089155/) |
| HV-03 | In a convenience sample, 140 instructors and 145 students picked the ChatGPT essay from a pair 70% and 60% of the time; experience, subject expertise and confidence did not reliably predict better performance. | Peer-reviewed, 2024 | [Springer](https://link.springer.com/article/10.1007/s40979-024-00158-3) |
| HV-05 | A study of parallel human and LLM corpora found systematic lexical, grammatical and rhetorical differences, in aggregate. | Peer-reviewed, 2025 | [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC11874169/) |
| HV-06 | In the models and genres studied, LLMs struggled to match human stylistic variation. | Peer-reviewed, 2025 | [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC11874169/) |
| HV-09 | From excess vocabulary across more than 15 million PubMed abstracts, at least 13.5% of 2024 abstracts were estimated to be processed with LLMs. An inferred lower bound, not a count, and not about social media. | Corpus study, 2025 | [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC12219543/) |
| HV-10 | The same study reports strong excess 2024 usage of "delves", "underscores" and "showcasing". An aggregate signal, not an individual verdict. | Corpus study, 2025 | [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC12219543/) |
| HV-11 | In an experiment with 680 US participants, some AI assistance increased engagement and content volume while lowering perceived quality and authenticity of discussion. | Preprint, not peer reviewed, 2025 | [arXiv](https://arxiv.org/abs/2506.14295) |
| HV-12 | A review of 47 journalism studies found no consistent trust penalty from AI provenance cues; effects depended on topic, baseline trust, source cues and signaled human oversight. Not about developer posts. | Systematic review, 2026 | [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC13183635/) |
| HV-18 | LinkedIn: "It's ok to use AI to help you write, but your posts and comments need to represent your voice and your perspectives." | Platform statement, 2026 | [LinkedIn](https://news.linkedin.com/2026/keeping-conversations-real-on-linkedin) |
| HV-19 | LinkedIn: content that appears AI-generated and lacks clear perspective is less likely to be widely distributed beyond the author's immediate network. LinkedIn's own claim, not independently measured. | Platform statement, 2026 | [LinkedIn](https://news.linkedin.com/2026/keeping-conversations-real-on-linkedin) |
| HV-21 | A detector vendor classified 4,061 of 5,000 sampled public LinkedIn posts of 100+ words as "Likely AI". A vendor classification, not verified authorship or a platform-wide rate. | Vendor, observational, 2026 | [Originality.ai](https://originality.ai/blog/ai-content-published-linkedin) |
| HV-22 | The vendor limits that estimate to posts found through its topic-and-date searches. | Vendor, 2026 | [Originality.ai](https://originality.ai/blog/ai-content-published-linkedin) |
| HV-23 | Wikipedia's editors' guide says its indicators are context-dependent, descriptive and not proof that a text was AI-generated. | Editors' guide, living | [Wikipedia](https://en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing) |
| HV-25 | The same guide lists inflated claims of broad significance, including "evolving landscape", as possible indicators. | Editors' guide, living | [Wikipedia](https://en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing) |
| HC-03 | Across 8,977 historical Upworthy headline experiments, more concreteness raised click-through for too-vague headlines and lowered it for too-concrete ones. Clicks, not reading or trust. | Registered report, 2025 | [Nature](https://www.nature.com/articles/s41598-024-81575-9) |
| HC-05 | Linguistic features predicted the higher-CTR Upworthy variant 54% of the time across all pairs. | Registered report, 2023 | [PLOS ONE](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0281682) |
| HC-16 | Hacker News guidelines: "Don't post generated text or AI-edited text. HN is for conversation between humans." The line sits in the comment guidelines. | Platform rule, living | [HN guidelines](https://news.ycombinator.com/newsguidelines.html) |
| DP-05 | PostHog's voice guidance favors concrete nouns, directness and honesty about limitations over polish. One company's judgment. | Practitioner guide, living | [PostHog handbook](https://posthog.com/handbook/brand/tone) |
| DA-03 | Dudycz's article on fixing event-sourcing bugs opens with bad data from integrations, users and shipped changes before describing a support incident. | Case example, 2026 | [event-driven.io](https://event-driven.io/en/fixing-bugs-in-event-sourcing-is-hard/) |

Platform rules and living guides change. Re-read them before citing them in published work.
