// cratis-ai-managed: hooks/scripts/cratis-release-notes-reviewed.cjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Exact marked programs from Cratis/Workflows at 40125b4fcae388e62ed9471edfffd0e991422f0a.
"use strict";
if (process.argv[2] === "release-notes") {
"use strict";
// cratis:program release-notes
const fs = require("node:fs");

const ALLOWED = ["Added", "Changed", "Fixed", "Removed", "Security", "Deprecated"];
const ALLOWED_LIST = ALLOWED.map(name => `## ${name}`).join(", ");
const REVIEWER_HEADINGS = ["test plan", "testing", "tests", "verification", "verified", "validation",
    "quality", "review", "notes", "notes for reviewers", "limitations", "known follow-up", "acceptance",
    "checked locally", "out of scope", "not verified", "not tested", "draft"];
// Result headings with a qualifier: "Checks (local)", "Checks (local, mirroring dotnet-build.yml)", "Local
// checks", "CI results". A bare `### CI` stays allowed: it can group CI changes inside a section.
const REVIEWER_HEADING = /^(?:(?:checks?|ci|tests?|local)\s*\(.*\)|(?:local|ci)\s+(?:checks?|results?|status|runs?|gates?)|(?:checks?|gates?)\s+run)$/;
const isReviewerHeading = name => REVIEWER_HEADINGS.includes(name) || REVIEWER_HEADING.test(name);
const NARRATIVE_HEADINGS = ["overview", "description", "what", "why", "how", "context", "details"];
const CHANGE_HEADINGS = ["changes", "what changed"];
const FORBIDDEN_HEADINGS = [...REVIEWER_HEADINGS, ...NARRATIVE_HEADINGS, ...CHANGE_HEADINGS];
const isForbiddenHeading = name => FORBIDDEN_HEADINGS.includes(name) || REVIEWER_HEADING.test(name);
const PLACEHOLDERS = [
    "Describe a new user-facing capability",
    "Describe a user-facing behavior change",
    "Describe a user-facing fix",
    "Optional summary of the PR",
    "Short statement of what was added",
    "Short statement of what changed",
    "Short statement of what was fixed",
    "Short statement of what was removed",
    "Short statement of the security fix",
    "Short statement of what is deprecated",
];
const VERBATIM = "This pull request description is published verbatim as the release notes; editing the description re-runs this check.";
const PR_COMMENT = "Move test plans, verification, review notes and provenance to a pull request comment.";

// Code is shown, not published as prose: the same fences and inline spans release-action ignores.
const FENCED_CODE = /^[ \t]*(`{3,}|~{3,})[\s\S]*?^[ \t]*\1[ \t]*$/gm;
const INLINE_CODE = /(`+)[^\n]*?\1/g;
const HTML_COMMENT = /<!--[\s\S]*?-->/g;
const KEYWORD = /\b(close[sd]?|fix(?:e[sd])?|resolve[sd]?|refs?|references)(?:[ \t]*:[ \t]*|[ \t]+)((?:[\w.-]+\/[\w.-]+)?#(\d+))/gi;
const KEYWORD_URL = /\b(close[sd]?|fix(?:e[sd])?|resolve[sd]?|refs?|references)(?:[ \t]*:[ \t]*|[ \t]+)(https?:\/\/(?:www\.)?github\.com\/([\w.-]+\/[\w.-]+)\/(?:issues|pull)\/(\d+))/gi;
// The pattern release-action closes issues with.
// It sees everything but fenced and inline code: HTML comments are not skipped.
const CLOSES_ISSUE = /(?<![\w/-])\(#(\d+)\)/g;
// What may follow the delivering `(#n)` and still count as the end of the bullet: more issue references
// (`(part of #n)`, `(see #n)`, `(Cratis/Repo#n)` too), closing emphasis, a trailing `<br>` and sentence
// punctuation (a `:` may introduce nested bullets).
const ONLY_ISSUES_AFTER = /^(?:\s*(?:\((?:(?:part of|see) )?(?:[\w.-]+\/[\w.-]+)?#\d+\)|\*\*|__|\*|_|<br\s*\/?>|[.,;:!?]))*\s*$/i;
// `(#56, #57)` closes nothing: release-action only recognises a `(#n)` of its own.
const GROUPED_ISSUES = /\(#\d+(?:\s*[, ]\s*#\d+)+\)/g;
// Labels that only ever introduce process notes, bullet or not.
const PROCESS_LABEL = /^\s*(?:(?:[-*+]|\d{1,9}[.)])\s+)?(?:\*\*|__|\*|_)?(?:test plan|notes? (?:for|to) reviewers?|reviewer notes?|known follow-up)(?:\s*:|(?:\*\*|__|\*|_)\s*:)/i;
// "Reviewed by ..." opens a note only outside a bullet or prose section: "- Reviewed by status is now shown
// on the dashboard (#4)" is a product bullet, and "Reviewed by Claude" is caught by the provenance rules.
const REVIEWED_BY = /^\s*(?:(?:[-*+]|\d{1,9}[.)])\s+)?(?:\*\*|__|\*|_)?reviewed by\b/i;
// Labels that can also open a product bullet ("- Validation: rules now apply to commands (#3)", "- Review:
// requests are now sent when a pull request is ready (#4)"): they are notes when standalone, when not a
// bullet under a section, or when what follows reports a test, CI or review result.
const RESULT_LABEL = /^\s*(?<bullet>(?:[-*+]|\d{1,9}[.)])\s+)?(?:\*\*|__|\*|_)?(?:validation|testing|verification|tests?|test results?|review|reviewed|tested|verified|local|locally|checked locally|local checks|checks|ci|ci results|gates?)(?:\s*:|(?:\*\*|__|\*|_)\s*:)(?<rest>.*)$/i;
// A result is reported, not described: the result word ends the clause ("all green", "12 specs passed"),
// so "failed commands now return a 400" and "green-field templates" stay product wording.
const END = String.raw`\s*(?=[.,;!:(]|$)`;
const RESULT_WORDING = new RegExp([
    String.raw`\b\d+\s+(?:\w+\s+)?(?:passed|passing|failed|failing)\b`,
    String.raw`^\s*(?:all\s+(?:\w+\s+){0,2}?)?(?:passed|passing|pass|green|failed|failing|ok|okay|done|locally|yes|no|clean|approved|lgtm|good|fine|none|pending)${END}`,
    String.raw`\ball\s+(?:\w+\s+){0,2}?(?:pass|passed|passing|green)${END}`,
    String.raw`\b(?:tests?|specs?|builds?|gates?|checks?)\s+(?:pass|passed)${END}`,
    String.raw`\breviewers?\s+(?:\w+\s+){0,2}approved\b|\blgtm\b|\bno (?:blocking |new )?(?:findings|concerns)\b`,
].join("|"), "i");
// A claim opens its own clause ("CI green, all tests passed."), so "The release is published when all tests
// pass" stays a product statement.
const CLAIM_START = String.raw`(?:^|[;,:.!]\s*)(?:(?:[-*+>]|\d{1,9}[.)])\s+)*(?:\*\*|__|\*|_)?\s*(?:done\s*[-;:,]\s*)?`;
const RESULT_CLAIM = new RegExp(CLAIM_START + "(?:" + [
    String.raw`all\s+(?:(?:the|unit|integration)\s+)*tests?\s+(?:now\s+)?(?:pass|passed|passing)(?:${END}|\s+(?:locally|in\s+ci|again)\b)`,
    String.raw`ci\s+(?:is\s+|was\s+|has\s+been\s+)?(?:green|passing|passed)(?:${END}|\s+(?:locally|again|and)\b)`,
].join("|") + ")", "i");
// Notes about the pull request rather than the change, recognised by their phrasing, wherever they stand: a
// bullet, the summary, the lead paragraph or a line of their own. Each pattern names how the note is
// worded, not its topic, so "Events are now verified against their schema" and "Toasts are stacked on top
// of each other" stay product wording.
// A clause opens a line (after list markers, a quote and emphasis) or follows sentence punctuation. Not a
// comma: "Forms are submitted, validated in the browser and saved" is product wording.
const CLAUSE = String.raw`(?:^\s*(?:(?:[-*+>]|\d{1,9}[.)])\s+)*(?:\*\*|__|\*|_)?|[;:.!(\u2014]\s*(?:\*\*|__|\*|_)?)`;
const CHECKED = String.raw`(?:verified|tested|checked|validated|confirmed|reproduced|exercised|smoke[- ]tested)`;
const CHECKED_BY = String.raw`(?:(?:this|it|everything(?: else)?|the rest|the (?:change|changes|fix|feature|branch|pr|pull request))\s+(?:was|were|has been|is)\s+)?(?:also\s+|only\s+|manually\s+|locally\s+)?${CHECKED}`;
// "locally", "manually" and "visually" end the note or lead into how ("Tested locally with ..."), so
// "- Validated locally stored tokens before use" stays product wording.
const HOW_CHECKED = String.raw`(?:(?:locally|manually|visually|by hand|end[- ]to[- ]end)(?=\s*(?:$|[.,;:)(*_\u2014-]|\s(?:with|and|on|in|by|against|using|via|before|after|first|too|only|as well|across|for|through)\b))|in (?:ci|storybook|the browser|a browser|a sandbox|the sandbox)\b|on (?:my|a local)\b|by (?:building|running|rebuilding|deploying|opening|installing|executing|starting|comparing)\b)`;
const PULL_REQUEST_REF = String.raw`(?:#\d+|[\w.-]+\/[\w.-]+#\d+|https:\/\/github\.com\/\S+\/pull\/\d+)`;
// The first match decides the message, so provenance comes before how a change was checked.
const PROCESS_NOTES = [
    // Provenance: which branch or build it was checked against ("Verified against Arc.TypeScript main (v0.34.0)").
    { kind: "provenance", pattern: new RegExp(CLAUSE + CHECKED_BY + String.raw`\s+(?:[\w-]+\s+){0,2}?(?:against|on|with|from)\s+(?:the\s+)?(?:[\w@/.-]+\s+){0,2}?(?:(?:main|master|HEAD)(?:\s+branch)?(?=\s*(?:\(|[.,;:)]|$))|the default branch\b)`, "i") },
    // ... or which kind of environment, where the note opens the clause ("Tested against a real kernel").
    { kind: "provenance", pattern: new RegExp(`${CLAUSE}${CHECKED_BY}\\s+(?:[\\w-]+\\s+){0,3}?(?:against|on|with)\\s+(?:a|the)\\s+(?:real|local|live|running)\\s+\\w`, "i") },
    // How or where it was checked: "Verified by building the sample", "verified in Storybook", "Tested locally".
    { kind: "verification", pattern: new RegExp(`${CLAUSE}${CHECKED_BY}\\s+(?:[\\w-]+\\s+){0,2}?${HOW_CHECKED}`, "i") },
    // What was not checked; the lookahead keeps "- Not verified events are rejected" product wording.
    { kind: "verification", pattern: new RegExp(CLAUSE + String.raw`(?:(?:this|it)\s+(?:is|was|has)\s+)?(?:not|never)\s+(?:yet\s+)?(?:been\s+)?(?:verified|tested|validated|exercised)(?=\s*(?:$|[.,;:)*_]|\s(?:against|on|with|in|here|yet|locally|end|by|manually|visually)\b))`, "i") },
    // Scope notes: "Storybook only;", "Docs-only.", "Tests only (#3)".
    { kind: "verification", pattern: new RegExp(CLAUSE + String.raw`(?:storybook|docs?|documentation|specs?|tests?|ci|tooling)[- ]only(?:\s+(?:change|changes|pr|pull request|update))?\s*(?:[;.,:)]|\(#|$)`, "i") },
    // Result counts: "specs 492/492", "Integration 110/110", "9 of 9 runs", "518 specs passed", "12 passed, 0 failed".
    { kind: "verification", pattern: /\b(?:specs?|tests?|runs?|checks?|jobs?|integration|unit|e2e|scenarios?)\s+(\d[\d,]*)\s*\/\s*\1\b|\b(\d[\d,]*)\s*\/\s*\2\s+(?:specs?|tests?|runs?|checks?|passed|passing|green)\b|\b\d[\d,]*\s+(?:tests?|specs?|scenarios?)\s+(?:passed|passing|green|failed|failing)\b|\b\d+\s+passed,\s*\d+\s+(?:failed|skipped)\b/i },
    // "3 of 3 runs" is a count in prose ("The dashboard shows 3 of 3 runs completed for each observer"): a result
    // only when it starts a clause or is followed by verification wording.
    { kind: "verification", pattern: new RegExp(CLAUSE + String.raw`(\d+)\s+of\s+\1\s+(?:runs|specs|tests|checks|scenarios)\b`, "i") },
    { kind: "verification", pattern: /\b(\d+)\s+of\s+\1\s+(?:runs|specs|tests|checks|scenarios)\s+(?:(?:all|have|had)\s+)?(?:passed|passing|green|succeeded|successful|failed|failing|ok)\b/i },
    // Gate results: "Tier 1 PASS", "0 warnings, 0 errors".
    { kind: "verification", pattern: /\btier[ -]?[123]\b[^.\n]{0,20}?\b(?:pass(?:ed|es)?|green|ok|clean)\b|\b\d+\s+warnings?,\s*\d+\s+errors?\b/i },
    // Reviewer instructions: stacking, retargeting, merge and deploy order, draft status.
    { kind: "instruction", pattern: new RegExp(String.raw`\bstacked\s+(?:on|onto)\s+(?:top of\s+)?(?:${PULL_REQUEST_REF}|(?:the\s+(?:other\s+)?|another\s+|this\s+)?(?:pr|pull request|branch)\b)|${CLAUSE}retarget(?:ed|ing)?\s+(?:it\s+|this\s+(?:pr\s+|pull request\s+)?)?(?:to|onto)\s+(?:main|master|the default branch)\b|\b(?:depends on|blocked by|merge (?:it\s+|this\s+)?(?:after|before)|land (?:it\s+|this\s+)?(?:after|before))\s+${PULL_REQUEST_REF}`, "i") },
    // "This PR is ..." always speaks to reviewers; "this branch" only when it says what the branch needs, so
    // "The CLI now warns when this branch is behind main" stays product wording.
    { kind: "instruction", pattern: /\bthis\s+(?:pr|pull request)\s+(?:is|was|should|must|needs|depends|requires|has to|can only|will need|replaces|supersedes|builds on|targets)\b/i },
    { kind: "instruction", pattern: new RegExp(CLAUSE + String.raw`this\s+branch\s+(?:should|must|needs|depends|requires|has to|can only|will need|replaces|supersedes|builds on|targets|is (?:stacked|based|built|part))\b`, "i") },
    // "Do not merge" is an instruction when it opens a clause ("Do not merge until the kernel ships.") or says what not
    // to merge yet; "Arrays in read models do not merge" and "should be deployed before" about other subjects are
    // product wording.
    { kind: "instruction", pattern: new RegExp(String.raw`\b(?:before|after|once)\s+(?:merging|deploying|releasing)\s+this\s+(?:pr|pull request|branch|change)\b|${CLAUSE}(?:do not|don['\u2019]t)\s+merge(?=\s*(?:$|[.,;:!)]))|\b(?:do not|don['\u2019]t)\s+merge\s+(?:this|it|yet|until|before|after)\b|\b(?:this|it)\s+(?:pr\s+|pull request\s+)?should be (?:deployed|merged|released) (?:separately|together|first|after|before|alongside)\b`, "i") },
    { kind: "instruction", pattern: /^\s*(?:\*\*|__)?(?:draft|wip|dnm|reviewers?|for reviewers|note to reviewers)(?:\*\*|__)?\s*:/i },
];
const PROCESS_FIX = {
    verification: "How or where the change was checked, what was not checked, and test or CI results are for reviewers: move them to a pull request comment. The release notes say only what changed for the reader.",
    provenance: "Which branch, commit or build the change was checked against is for reviewers: move it to a pull request comment. If compatibility matters to the reader, state it as a fact, for example `Requires @cratis/arc 0.34 or later`.",
    instruction: "Stacking, retargeting, merge or deploy order and draft status are for reviewers: move them to a pull request comment. If consumers must upgrade in a set order, write that as an upgrade bullet, for example `Upgrade the Chronicle kernel to 19.25 before this client`.",
};
const PROCESS_RULE = {
    verification: "Review, verification or provenance note",
    provenance: "Review, verification or provenance note",
    instruction: "Reviewer instruction",
};
const BOLD_ONLY = /^\s*(?:(?:[-*+]|\d{1,9}[.)])\s+)?(?:\*\*|__|\*|_)(.+?)(?:\*\*|__|\*|_)\s*:?\s*$/;
const AGENT = "(?:claude|copilot|codex|chatgpt|gpt|cursor|gemini|opus|sonnet|haiku|openai|anthropic|an? ai\\b|llm)";
// Authorship of the description itself starts the line ("Generated with Claude Code"); a bullet about a
// product feature that generates text with a model ("Text generated with OpenAI is now cached") does not.
const AUTHORED = new RegExp(`^\\s*(?:(?:[-*+>]|\\d{1,9}[.)])\\s+)*[^\\w\\s]*\\s*(?:(?:this|the)\\s+(?:pull request|pr|description|change|changes|code|patch|commit)\\s+(?:was|is|has been)\\s+)?(?:\\*\\*|__|\\*|_)?(?:generated|written|drafted|authored|produced)\\s+(?:entirely\\s+|mostly\\s+|partly\\s+)?(?:with|by)\\s+(?:an?\\s+)?\\[?${AGENT}`, "i");
// Provenance is a note when it starts a clause ("Reviewed with a cross-provider review.", "Review: Opus-only
// review"), not when it sits inside a product sentence ("Adds same-provider review routing ..."). Where the
// text is product wording (a bullet under an allowed section, a `## Summary` or a lead paragraph) the note
// must also END the clause, apart from issue references and a result word ("- Cross-provider review
// pending (#5)"), so "- Cross-provider review is now selectable (#5)" passes. Only a stand-alone line
// outside those uses the loose tail below.
const PROVIDER = "(?:opus|anthropic|gpt|claude|openai)-only";
const QUALIFIER = String.raw`(?:(?:an?|the|this|our|quick|brief|short|final|full|independent|second|extra|additional|another|fresh|light)\s+)*`;
const NOTE_START = CLAIM_START.replace("(?:^|", "(?:^\\s*|");
const PROVENANCE_TOPIC = new RegExp(NOTE_START + "(?:" + [
    String.raw`${QUALIFIER}(?:same|cross)-provider\)?\s+review\b`,
    String.raw`review(?:ed|s)?\s+(?:(?:was|is|with|by|using|via|through)\s+)*(?:an?\s+)?(?:(?:same|cross)-provider|${PROVIDER})(?:\s+review)?\b`,
    String.raw`${QUALIFIER}${PROVIDER}\s+(?:\([^)]*\)\s+)?review\b`,
].join("|") + ")", "i");
const NOTE_RESULT = String.raw`\s*(?:(?:is|was|has\s+been)\s+)?(?:pending|passed|passing|done|complete|completed|approved|clean|green|ok|skipped|missing|unavailable|only|requested|required)\b`;
// In product wording the note must end the clause: nothing but a result word, punctuation, closing emphasis
// or `<br>` after it. Elsewhere any punctuation that continues the note ("Cross-provider review: no
// findings") is enough.
// Product wording can say a review "is required", "is requested" or "only" applies ("Cross-provider review is
// required (#4)"); inside a bullet those words describe the feature, so the strict tail leaves them out.
const NOTE_RESULT_STRICT = String.raw`\s*(?:(?:is|was|has\s+been)\s+)?(?:pending|passed|passing|done|complete|completed|approved|clean|green|ok|skipped|missing|unavailable)\b`;
const NOTE_TAIL_STRICT = new RegExp(`^(?:${NOTE_RESULT_STRICT})?(?:[\\s:;,.!*_]|<br\\s*\\/?>)*$`, "i");
// An outright result or provenance claim ("Reviewed with Anthropic models only") keeps every result word:
// only the topic path is narrowed for product wording.
const NOTE_TAIL_CLAIM = new RegExp(`^(?:${NOTE_RESULT})?(?:[\\s:;,.!*_]|<br\\s*\\/?>)*$`, "i");
const NOTE_TAIL_LOOSE = new RegExp(`^(?:${NOTE_RESULT}|\\s*[:;,.!\u2014\u2013-]\\s*\\S|\\s*\\((?!#)|[\\s:;,.!]*$)`, "i");
// A model or agent name may run to a few words ("Claude Code", "GPT-5.5", "Opus 5.5").
const AGENT_NAME = String.raw`${AGENT}\b(?:[\s-]+(?:code|opus|sonnet|haiku|pro|max|mini|[\w.]*\d[\w.]*)\b)*`;
// "Reviewed with Anthropic models only (...)": a clause that opens by saying who or what reviewed it. In
// product wording only the past tense is a claim: "- Review with two models (#5)" names a feature.
const provenanceResult = verb => new RegExp(NOTE_START + "(?:" + [
    String.raw`${verb}\s+(?:with|by|using|via)\s+(?:(?:an?|the|two|three|both|only)\s+)?(?:[\w.-]+\s+){0,2}?(?:models?|agents?|providers?)\b`,
    String.raw`${verb}\s+(?:with|by|using|via)\s+(?:an?\s+)?${AGENT_NAME}`,
    String.raw`(?:the\s+)?review workflow\s+(?:has\s+|had\s+|also\s+)?(?:(?:passed|failed)${END}`
        + String.raw`|passed\s+with\s+(?:no\s+\w+|\w+\s+findings?)|passed\s+(?:with|locally|again|twice|cleanly)\b|returned\s+(?:findings|no\s+\w+|nothing|clean|\d+)\b`
        + String.raw`|ran\s+(?:twice|once|again|clean|cleanly|green|locally|successfully|\d+)\b`
        + String.raw`|found\s+(?:nothing|no\s+\w+|\d+|issues|findings|blockers)\b)`,
].join("|") + ")", "i");
const PROVENANCE_RESULT = provenanceResult(String.raw`review(?:ed)?`);
const PROVENANCE_RESULT_STRICT = provenanceResult("reviewed");
const CO_AUTHORED = /^\s*(?:(?:[-*+>]|\d{1,9}[.)])\s+)*(?:\*\*|__|\*|_)?\s*co-authored-by:/i;
const withoutIssues = text => text.replace(/(?:\s*\((?:part of )?#\d+\))+\s*$/, "");
// A product bullet that ends in an issue reference names a change ("- Cross-provider review (#12)"); it is
// only a note when a result word precedes the reference ("- Cross-provider review pending (#3)").
const NOTE_TAIL_STRICT_RESULT = new RegExp(`^(?:${NOTE_RESULT_STRICT})(?:[\\s:;,.!*_]|<br\\s*\\/?>)*$`, "i");
const strictTail = raw => {
    const tail = withoutIssues(raw);
    return (tail === raw ? NOTE_TAIL_STRICT : NOTE_TAIL_STRICT_RESULT).test(tail);
};
const provenance = (line, strict) => {
    if (CO_AUTHORED.test(line)) {
        return true;
    }
    const result = (strict ? PROVENANCE_RESULT_STRICT : PROVENANCE_RESULT).exec(line);
    if (result && (!strict || NOTE_TAIL_CLAIM.test(withoutIssues(line.slice(result.index + result[0].length))))) {
        return true;
    }
    const topic = PROVENANCE_TOPIC.exec(line);
    if (!topic) {
        return false;
    }
    const raw = line.slice(topic.index + topic[0].length);
    return strict ? strictTail(raw) : NOTE_TAIL_LOOSE.test(withoutIssues(raw));
};
const TRANSCRIPT = /<summary>\s*original prompt\s*<\/summary>|^\s*(?:#{1,6}\s+|\*\*|__)?original prompt(?:\*\*|__)?\s*:?\s*$|\blet copilot coding agent\b|\bcopilot coding agent\s+(?:tips|settings|set things up)|\bfirewall rules blocked me\b/i;
const INLINE_LINK = /\]\(\s*<?([^)\s>]+)/g;
const REFERENCE_LINK = /^ {0,3}\[(?!\^)[^\]]+\]:\s*<?([^\s>]+)/;
const HTML_LINK = /\b(?:href|src)\s*=\s*["']([^"']+)["']/gi;
const ABSOLUTE = /^(?:https?:\/\/|#|mailto:)/i;
const ATX_HEADING = /^ {0,3}(#{1,6})(?:[ \t]+(.*?))?(?:[ \t]+#+)?[ \t]*$/;
const SETEXT_UNDERLINE = /^ {0,3}(=+|-+)[ \t]*$/;
const THEMATIC_BREAK = /^ {0,3}([-*_])(?:[ \t]*\1){2,}[ \t]*$/;
const LIST_ITEM = /^\s*(?:[-*+]|\d{1,9}[.)])[ \t]+\S/;
// A bullet that only says there is nothing (`- None.`, `- **N/A**`) is a filler, not a change.
const FILLER = /^(?:none|n\/a|nothing)$/i;
const isFiller = text => FILLER.test(text
    .replace(/^\s*(?:[-*+]|\d{1,9}[.)])[ \t]+/, "")
    .replace(/<\/?(?:b|i|em|strong|u|s|del|ins)\b[^>]*>/gi, "")
    .replace(/[*_~\s]+/g, " ")
    .replace(/^[\s.,;:!?]+|[\s.,;:!?]+$/g, ""));
// HTML that renders as a heading, or as a bold-only line, on the release page.
const HTML_HEADING = /^\s*<h([1-6])\b[^>]*>(.*?)(?:<\/h\1>)?\s*$/i;
const HTML_BOLD_ONLY = /^\s*(?:(?:[-*+]|\d{1,9}[.)])\s+)?(?:<p[^>]*>\s*)?<(b|strong|i|em)>(.+?)<\/\1>\s*:?\s*(?:<\/p>)?\s*$/i;
// A <summary> may span lines: it is read from the whole text and reported at the line it opens on.
const HTML_SUMMARY = /<summary\b[^>]*>([\s\S]*?)<\/summary>/gi;
const ISSUE_TEXT = /^(?:[\w.-]+\/[\w.-]+)?#\d+$/;
const KEYWORD_TEXT = /^(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?|refs?|references)\b(?:[ \t]*:)?(?:[ \t]+(?:[\w.-]+\/[\w.-]+)?#\d+)?$/i;

const blank = (text, pattern, fill) => text.replace(pattern, match => match.replace(/[^\n]/g, fill));
const plain = html => html.replace(/<[^>]*>/g, "").trim();
// A closing keyword is still one when wrapped in emphasis or a link (`**Closes** #1`, `Closes [#1](url)`),
// so the keyword rules read the text as it renders: emphasis dropped, a link reduced to its issue
// reference or its URL, or, when the link text is itself a keyword, to that text followed by its URL (`[Closes](issue-url)`).
// A keyword as link text is a keyword only where it starts a clause ("[Closes](url)", "Done. [Fixes](url)"), not
// inside a sentence ("Adds a [Fixes](url) page").
const CLAUSE_START = /(?:^|[.;:!(])\s*(?:(?:[-*+>]|\d{1,9}[.)])\s+)*$/;
const linkAs = (text, url, before) => {
    const trimmed = text.trim();
    return ISSUE_TEXT.test(trimmed) ? trimmed
        : KEYWORD_TEXT.test(trimmed) && CLAUSE_START.test(before) ? `${trimmed} ${url}` : url;
};
const rendered = line => line
    .replace(/\*+|~~|(?<!\w)_+|_+(?!\w)/g, "")
    .replace(/<(https?:\/\/[^>\s]+)>/gi, "$1")
    .replace(/<a\b[^>]*\bhref\s*=\s*["']([^"']+)["'][^>]*>(.*?)<\/a>/gi, (whole, url, text, offset, all) => linkAs(plain(text), url, all.slice(0, offset)))
    .replace(/<\/?[a-z][^>]*>/gi, "")
    .replace(/!?\[([^\]]*)\]\(\s*<?([^)\s>]+)>?(?:\s[^)]*)?\)/g, (whole, text, url, offset, all) => linkAs(text, url, all.slice(0, offset)))
    .replace(/\[([^\]]*)\]\[[^\]]*\]/g, "$1")
    .replace(/[\[\]]/g, "");
const normalize = name => name.replace(/^[*_\s]+|[*_\s:]+$/g, "").replace(/\s+/g, " ").toLowerCase();
const quote = line => {
    const trimmed = line.trim();
    return trimmed.length > 120 ? `${trimmed.slice(0, 117)}...` : trimmed;
};

function headingAt(visible, index) {
    const atx = ATX_HEADING.exec(visible[index]);
    if (atx) {
        return { level: atx[1].length, name: (atx[2] || "").trim(), setext: false };
    }
    const html = HTML_HEADING.exec(visible[index]);
    if (html) {
        return { level: Number(html[1]), name: plain(html[2]), setext: false, html: true };
    }
    const next = visible[index + 1];
    const underline = next === undefined ? null : SETEXT_UNDERLINE.exec(next);
    if (!underline || !visible[index].trim() || THEMATIC_BREAK.test(visible[index]) || ATX_HEADING.test(visible[index])) {
        return null;
    }
    // Only a paragraph turns into a heading; under a list item or a quote the underline is a rule.
    let start = index;
    while (start > 0 && visible[start - 1].trim() && !ATX_HEADING.test(visible[start - 1])) {
        start--;
    }
    if (!LIST_ITEM.test(visible[start]) && !/^\s*>/.test(visible[start]) && !/^\s*\|/.test(visible[start])) {
        return { level: underline[1][0] === "=" ? 1 : 2, name: visible[index].trim(), setext: true };
    }
    return null;
}

function headingFix(heading) {
    const name = normalize(heading.name);
    const setext = heading.setext
        ? " A line directly above `---` or `===` renders as a heading; put a blank line before a `---` rule."
        : "";
    if (isReviewerHeading(name)) {
        return `${PR_COMMENT}${setext}`;
    }
    if (NARRATIVE_HEADINGS.includes(name)) {
        return `Remove the heading: only an optional \`## Summary\` section or one lead paragraph of 1-3 sentences, without a heading, may precede the sections; everything else is a bullet under ${ALLOWED_LIST}.${setext}`;
    }
    if (CHANGE_HEADINGS.includes(name)) {
        return `Write the changes as bullets under ${ALLOWED_LIST}.${setext}`;
    }
    return `Only ${ALLOWED_LIST} are allowed as sections. Breaking changes and upgrade steps are bullets in ## Changed or ## Removed; group bullets with a ### sub-heading inside an allowed section if needed.${setext}`;
}

// The text release-action scans for `(#n)`: fenced code and then inline code removed outright (not blanked,
// so what surrounds a removed span joins up), with the original offset of every character kept.
function closingReferences(text, prose) {
    let scanned = { text, origin: Array.from({ length: text.length }, (_, offset) => offset) };
    for (const pattern of [FENCED_CODE, INLINE_CODE]) {
        let out = "";
        const origin = [];
        let last = 0;
        const keep = end => {
            out += scanned.text.slice(last, end);
            for (let offset = last; offset < end; offset++) {
                origin.push(scanned.origin[offset]);
            }
        };
        for (const match of scanned.text.matchAll(pattern)) {
            keep(match.index);
            last = match.index + match[0].length;
        }
        keep(scanned.text.length);
        scanned = { text: out, origin };
    }
    const starts = [0];
    for (let offset = text.indexOf("\n"); offset >= 0; offset = text.indexOf("\n", offset + 1)) {
        starts.push(offset + 1);
    }
    const position = offset => {
        let line = starts.length - 1;
        while (starts[line] > offset) {
            line--;
        }
        return { line, column: offset - starts[line] };
    };
    const found = [];
    for (const match of scanned.text.matchAll(CLOSES_ISSUE)) {
        const first = position(scanned.origin[match.index]);
        const last = position(scanned.origin[match.index + match[0].length - 1]);
        found.push({ number: match[1], line: first.line, endLine: last.line, endColumn: last.column + 1,
            commented: prose[first.line][first.column] === " " });
    }
    return found;
}

function check({ body, repository, defaultBranch }) {
    const lines = body.replace(/\r\n?/g, "\n").split("\n");
    // Code keeps its extent (so a section holding only an example is not empty) but loses its text;
    // comments vanish entirely, as they do on the release page.
    const text = lines.join("\n");
    let visible = blank(text, FENCED_CODE, "x");
    visible = blank(visible, INLINE_CODE, "x");
    visible = blank(visible, HTML_COMMENT, " ").split("\n");
    // The same without fenced code, which does not belong to the bullet text around it.
    const uncommented = blank(blank(text, FENCED_CODE, " "), INLINE_CODE, "x");
    const prose = blank(uncommented, HTML_COMMENT, " ").split("\n");
    // What release-action reads when it closes issues: it removes fenced and inline code, in that order,
    // and nothing else, so a `(#n)` hidden in an HTML comment still closes its issue. Each match is
    // mapped back to where it sits in the description.
    const closes = closingReferences(text, prose);

    const violations = [];
    const report = (rule, index, fix) => violations.push({ rule, line: index === null ? null : index + 1,
        text: index === null ? null : quote(lines[index]), fix });

    // The text of every <summary>, at the line it opens on.
    const summaryAt = new Map();
    const flat = visible.join("\n");
    for (const match of flat.matchAll(HTML_SUMMARY)) {
        summaryAt.set(flat.slice(0, match.index).split("\n").length - 1, plain(match[1]));
    }

    // Each bullet runs to the line before the next bullet, heading or unindented paragraph.
    const bulletEnd = new Array(prose.length).fill(-1);
    for (let start = 0; start < prose.length; start++) {
        if (!LIST_ITEM.test(prose[start])) {
            continue;
        }
        let end = start;
        for (let next = start + 1; next < prose.length; next++) {
            const candidate = prose[next];
            if (!candidate.trim()) {
                continue;
            }
            if (LIST_ITEM.test(candidate) || ATX_HEADING.test(candidate) || THEMATIC_BREAK.test(candidate)
                || (!/^(?: {2,}|\t)/.test(candidate) && !prose[next - 1].trim())) {
                break;
            }
            end = next;
        }
        for (let line = start; line <= end; line++) {
            bulletEnd[line] = end;
        }
    }

    // GitHub hides everything after a `<!--` that is never closed.
    const unclosed = visible.findIndex(line => line.includes("<!--"));
    if (unclosed >= 0) {
        report("Unclosed HTML comment", unclosed, "Close the comment with `-->`, or delete it: GitHub hides everything after an unclosed `<!--`, so the release page would lose the rest of the description.");
    }

    let section = null;
    let preamble = true;
    let paragraphs = 0;
    let inParagraph = false;
    let bulletsOutside = false;
    let lastOrder = -1;
    let summaries = 0;
    const sections = [];
    const proseOnly = index => {
        if (!section.flagged) {
            section.flagged = true;
            report("Summary is prose", index, `A \`## Summary\` is short prose; put each change in a bullet under ${ALLOWED_LIST}, or delete the summary.`);
        }
    };

    for (let index = 0; index < visible.length; index++) {
        const line = visible[index];
        const heading = headingAt(visible, index);

        if (heading) {
            inParagraph = false;
            const name = normalize(heading.name);
            const order = ALLOWED.findIndex(allowed => allowed.toLowerCase() === name);
            const summary = name === "summary";
            // Inside an open section a `###` named like an allowed section (`### Added`) is a sub-heading; at the
            // top, before or between sections, it is a section written at the wrong level.
            const subHeading = heading.level > 2 && section !== null;
            if (heading.level <= 2 || (order >= 0 && !subHeading) || (summary && preamble)) {
                const leadParagraph = preamble && paragraphs > 0;
                preamble = false;
                if (summary) {
                    if (heading.level !== 2 || heading.html) {
                        report("Section heading level", index, "Sections are level-2 Markdown headings: write `## Summary`.");
                    }
                    if (summaries > 0 || lastOrder >= 0) {
                        report("Section order", index, `\`## Summary\` is optional, appears once and comes first, before ${ALLOWED_LIST}; move it to the top or delete it.`);
                    } else if (leadParagraph) {
                        report("Summary and lead paragraph", index, "Use either a `## Summary` section or one lead paragraph without a heading, not both; merge them into one.");
                    }
                    summaries++;
                    section = { index, content: false, bullets: 0, prose: true, flagged: false };
                    sections.push(section);
                } else if (order >= 0) {
                    if (heading.level !== 2 || heading.html) {
                        report("Section heading level", index, `Sections are level-2 Markdown headings: write \`## ${ALLOWED[order]}\`.`);
                    }
                    if (order <= lastOrder) {
                        report("Section order", index, `Each allowed section appears once, in the order ${ALLOWED_LIST}; merge the bullets into the earlier section.`);
                    }
                    lastOrder = Math.max(lastOrder, order);
                    section = { index, content: false, bullets: 0, prose: false, flagged: false };
                    sections.push(section);
                } else {
                    report("Heading not allowed", index, headingFix(heading));
                    section = null;
                }
            } else if (isForbiddenHeading(name) || name === "summary") {
                report("Heading not allowed", index, headingFix(heading));
            } else if (section && section.prose) {
                proseOnly(index);
            } else if (preamble) {
                report("Sub-heading outside a section", index, `A ### sub-heading only groups bullets inside one of ${ALLOWED_LIST}; move it under the section it belongs to.`);
            }
            // A heading is still a line of the description: what a heading says is published, and a `(#n)`
            // in it closes the issue, so it gets the same content checks as any other line below.
        } else if (!line.trim() || THEMATIC_BREAK.test(line)) {
            inParagraph = false;
        } else {
            if (section) {
                section.content = true;
                if (LIST_ITEM.test(line)) {
                    if (section.prose) {
                        proseOnly(index);
                    } else if (!isFiller(prose.slice(index, bulletEnd[index] + 1).join(" "))) {
                        section.bullets++;
                    }
                }
            } else if (preamble) {
                if (LIST_ITEM.test(line)) {
                    if (!bulletsOutside) {
                        report("Bullets outside a section", index, `Put bullets under one of ${ALLOWED_LIST}; only a lead paragraph without a heading may precede the sections.`);
                    }
                    bulletsOutside = true;
                } else if (!inParagraph) {
                    paragraphs++;
                    if (paragraphs === 2) {
                        report("More than one lead paragraph", index, `Keep at most one lead paragraph of 1-3 sentences that states the theme across the bullets; put each change in a bullet under ${ALLOWED_LIST}.`);
                    }
                }
                inParagraph = true;
            }
        }

        const shown = rendered(line);
        for (const match of shown.matchAll(KEYWORD)) {
            const reference = match[2];
            const number = match[3];
            const bracketed = shown[match.index - 1] === "(" && shown[match.index + match[0].length] === ")";
            const fix = reference.startsWith("#") && bracketed
                ? `Replace \`(${match[0].trim()})\` with \`(#${number})\` if this change delivers it, or \`(part of #${number})\` if it must stay open.`
                : reference.startsWith("#")
                ? `Replace \`${match[0].trim()}\` with \`(#${number})\` at the end of the bullet that delivers it, or \`(part of #${number})\` if it must stay open.`
                : `Replace \`${match[0].trim()}\` with a plain \`${reference}\` reference; another repository's issues are never closed from here.`;
            report("Closing or linking keyword", index, `${fix} Keywords such as Closes, Fixes and Refs are never used in release notes.`);
        }
        for (const match of shown.matchAll(KEYWORD_URL)) {
            const sameRepository = match[3].toLowerCase() === repository.toLowerCase();
            const fix = sameRepository
                ? `Replace \`${match[0].trim()}\` with \`(#${match[4]})\` at the end of the bullet that delivers it, or \`(part of #${match[4]})\` if it must stay open.`
                : `Replace \`${match[0].trim()}\` with a plain \`${match[3]}#${match[4]}\` reference; another repository's issues are never closed from here.`;
            report("Closing or linking keyword", index, `${fix} Keywords such as Closes, Fixes and Refs are never used in release notes, with an issue URL either.`);
        }
        const bold = BOLD_ONLY.exec(line);
        const htmlBold = HTML_BOLD_ONLY.exec(line);
        const summarized = summaryAt.get(index);
        const boldName = bold ? bold[1] : htmlBold ? plain(htmlBold[2]) : summarized !== undefined ? summarized : null;
        // A heading's text is read without its `###` or `<h3>`, so a label there is a label like any other.
        const noted = heading ? line.replace(/^\s{0,3}#{1,6}[ \t]+|^\s*<h[1-6]\b[^>]*>/i, "") : line;
        const labelled = RESULT_LABEL.exec(noted);
        const underAllowedSection = Boolean(labelled && labelled.groups.bullet && section && !section.prose);
        // Inside a bullet under an allowed section (or a continuation line of it) wording is product wording
        // unless it reads as a result.
        const inBullet = bulletEnd[index] >= 0 && Boolean(section) && !section.prose;
        // Product wording: a bullet, a `## Summary` or the lead paragraph. Only a stand-alone line outside them
        // is checked loosely for provenance and "Reviewed by".
        const productText = inBullet || (section ? section.prose : preamble);
        const processNote = PROCESS_NOTES.find(note => note.pattern.test(noted));
        if (boldName !== null && isForbiddenHeading(normalize(boldName))) {
            // A bold line or a <summary> naming a forbidden heading is that heading, not a note.
            report("Heading not allowed", index, headingFix({ name: boldName, setext: false }));
        } else if (PROCESS_LABEL.test(noted) || (!productText && REVIEWED_BY.test(noted)) || AUTHORED.test(noted) || provenance(noted, productText) || RESULT_CLAIM.test(noted)
            || (labelled && (!underAllowedSection || RESULT_WORDING.test(labelled.groups.rest)))) {
            report("Review, verification or provenance note", index, PR_COMMENT);
        } else if (processNote && !(heading && isForbiddenHeading(normalize(heading.name)))) {
            // A forbidden heading ("### Not verified") is already reported as the heading it is.
            report(PROCESS_RULE[processNote.kind], index, PROCESS_FIX[processNote.kind]);
        }
        for (const match of prose[index].matchAll(GROUPED_ISSUES)) {
            const numbers = [...match[0].matchAll(/\d+/g)].map(number => number[0]);
            report("Issue reference position", index, `\`${match[0]}\` closes nothing: release-action only recognises a \`(#n)\` of its own. Write each delivered issue as its own \`(#n)\`: \`${numbers.map(number => `(#${number})`).join(" ")}\`.`);
        }
        for (const match of closes.filter(candidate => candidate.line === index)) {
            if (match.commented) {
                report("Issue reference position", index, `\`(#${match.number})\` sits inside an HTML comment: the release page does not show the comment, but release-action still closes #${match.number}. Delete it from the comment, or write \`(part of #${match.number})\` or \`see #${match.number}\` outside the comment if it must stay open; never put issue references in comments.`);
                continue;
            }
            const end = bulletEnd[match.endLine];
            const rest = prose[match.endLine].slice(match.endColumn);
            const after = end < 0 ? null : [rest, ...prose.slice(match.endLine + 1, end + 1)].join("\n");
            if (after !== null && !ONLY_ISSUES_AFTER.test(after)) {
                const followed = [lines[match.endLine].slice(match.endColumn), ...lines.slice(match.endLine + 1, end + 1)]
                    .join(" ").replace(/\s+/g, " ").trim().replace(/`/g, "'");
                const excerpt = followed.length > 60 ? `${followed.slice(0, 57).trimEnd()} ...` : followed;
                report("Issue reference position", index, `\`(#${match.number})\` is followed by \`${excerpt}\`; move that text before \`(#${match.number})\` so the issue reference ends the bullet, or write \`(part of #${match.number})\` or \`see #${match.number}\` if it must stay open.`);
            } else if (after === null) {
                report("Issue reference position", index, `Release-action closes \`(#${match.number})\` wherever it appears; put \`(#${match.number})\` at the end of the bullet that delivers the issue, or write \`(part of #${match.number})\` or \`see #${match.number}\` if it must stay open.`);
            }
        }
        if (TRANSCRIPT.test(line)) {
            report("Agent transcript", index, "Remove the Copilot \"Original prompt\" / agent transcript; it is not part of the release notes.");
        }
        if (PLACEHOLDERS.some(placeholder => line.toLowerCase().includes(placeholder.toLowerCase()))) {
            report("Template placeholder", index, "Replace the template placeholder with the actual change, or delete it and its section if there is nothing to say.");
        }
        const targets = [...line.matchAll(INLINE_LINK), ...line.matchAll(HTML_LINK)].map(match => match[1]);
        const reference = REFERENCE_LINK.exec(line);
        if (reference) {
            targets.push(reference[1]);
        }
        for (const target of targets.filter(target => !ABSOLUTE.test(target))) {
            const path = target.replace(/^\.?\//, "");
            report("Relative link", index, `Use an absolute https:// link, for example https://github.com/${repository}/blob/${defaultBranch}/${path}; relative links 404 on the release page. #anchors and mailto: are fine.`);
        }
        if (heading && heading.setext) {
            index++;
        }
    }

    for (const empty of sections.filter(candidate => !candidate.content)) {
        report("Empty section", empty.index, "Delete sections that have nothing in them.");
    }

    for (const bulletless of sections.filter(candidate => candidate.content && !candidate.prose && candidate.bullets === 0)) {
        report("Section without bullets", bulletless.index, "Every section lists its changes as bullets: turn the text into bullets, or delete the section (a summary belongs in `## Summary` or one lead paragraph).");
    }

    if (!sections.some(candidate => candidate.bullets > 0)) {
        report("No release notes", null, `A release-bound pull request needs at least one bullet under ${ALLOWED_LIST}, describing what a consumer compiles against, runs or observes.`);
    }

    return violations;
}

const escapeData = value => value.replace(/%/g, "%25").replace(/\r/g, "%0D").replace(/\n/g, "%0A");
const escapeProperty = value => escapeData(value).replace(/:/g, "%3A").replace(/,/g, "%2C");
const summary = markdown => {
    if (process.env.GITHUB_STEP_SUMMARY) {
        fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, `${markdown}\n`);
    }
};
const RERUN = "Only a pull request into the default branch is checked; the check re-runs when the description or a label changes.";
const skip = reason => {
    console.log(`::notice title=Release notes not checked::${escapeData(`${reason} ${RERUN}`)}`);
    summary(`### Release notes\n\nNot checked: ${reason} ${RERUN}`);
    process.exit(0);
};

// The pull request as it is now (see "Read the pull request as it is now"); the event payload when that read
// failed or did not run.
let live = null;
try {
    const parsed = process.env.PR_JSON ? JSON.parse(fs.readFileSync(process.env.PR_JSON, "utf8")) : null;
    live = parsed && typeof parsed === "object" && Number.isInteger(parsed.number) ? parsed : null;
} catch {
    live = null;
}
let labels = [];
try {
    const parsed = live ? (live.labels || []).map(label => label && label.name) : JSON.parse(process.env.PR_LABELS || "[]");
    labels = Array.isArray(parsed) ? parsed.filter(label => typeof label === "string") : [];
} catch {
    labels = [];
}
const body = live ? (typeof live.body === "string" ? live.body : "") : (process.env.PR_BODY || "");
const author = live ? (live.user && live.user.login) || "" : (process.env.PR_AUTHOR || "");
const base = live ? (live.base && live.base.ref) || "" : (process.env.PR_BASE || "");
const defaultBranch = process.env.DEFAULT_BRANCH || "";

if (!base) {
    skip("this run was not triggered by a pull request.");
}
if (author === "dependabot[bot]") {
    skip("Dependabot pull requests carry Dependabot's generated description.");
}
if (base !== defaultBranch) {
    skip(`the pull request targets ${base}, not the default branch ${defaultBranch}, so merging it releases nothing.`);
}

// A pull request carrying any of major, minor or patch is release-bound, also next to a second intent label
// (verify-release-intent rejects that combination on its own). Every other pull request may still become
// release-bound, so its violations are reported as warnings.
const releaseLabels = [...new Set(labels.filter(label => ["major", "minor", "patch"].includes(label)))];
const noRelease = labels.includes("no-release");
const releaseBound = releaseLabels.length > 0;
const violations = check({
    body,
    repository: process.env.GITHUB_REPOSITORY || "Cratis/<Repository>",
    defaultBranch,
// A no-release pull request publishes nothing, so having no change list is not a mistake there.
}).filter(violation => releaseBound || !noRelease || violation.rule !== "No release notes");

const status = releaseBound ? ""
    : noRelease ? "The pull request is labelled no-release, so this description is not published now. "
    : "The pull request carries none of major, minor or patch yet. ";
if (violations.length === 0) {
    console.log(`${status}The pull request description follows the release-note contract.`);
    summary(`### Release notes\n\n${status}The pull request description follows the release-note contract and can be published as the release notes.`);
    process.exit(0);
}

const level = releaseBound ? "error" : "warning";
const consequence = releaseBound ? VERBATIM
    : `${noRelease ? "Labelled no-release, so this is a warning" : "Not labelled yet, so this is a warning"}: it fails once the pull request is labelled major, minor or patch. ${VERBATIM}`;
for (const violation of violations) {
    const where = violation.line === null ? "The description" : `Line ${violation.line} \`${violation.text}\``;
    console.log(`::${level} title=${escapeProperty(`Release notes: ${violation.rule}`)}::${escapeData(`${violation.rule}: ${where}. ${violation.fix} ${consequence}`)}`);
}
const rows = violations.map(violation =>
    `| ${violation.line ?? ""} | ${violation.rule} | ${(violation.text ?? "").replace(/\|/g, "\\|").replace(/`/g, "'")} |`);
summary([
    "### Release notes",
    "",
    `${violations.length} release-note ${releaseBound ? "violation(s)" : "warning(s)"}. ${consequence}`,
    "",
    "| Line | Rule | Text |",
    "| --- | --- | --- |",
    ...rows,
].join("\n"));
process.exit(releaseBound ? 1 : 0);

}
if (process.argv[2] === "release-notes-drift") {
"use strict";
// cratis:program release-notes-drift
const fs = require("node:fs");
const { execFileSync } = require("node:child_process");

const ALLOWED = ["added", "changed", "fixed", "removed", "security", "deprecated"];
const REGISTRIES = [
    { name: "PyPI", mention: /\bpypi\b/i, publish: /pypi|twine|pypa\/gh-action-pypi-publish|uv publish|poetry publish/i },
    { name: "npm", mention: /\bnpm(?:js)?\b(?!\s+(?:install|ci|run|scripts?|dependenc))/i, publish: /npm publish|yarn npm publish|npm-publish|registry\.npmjs\.org|NPM_TOKEN|environment:\s*npm\b/i },
    { name: "NuGet", mention: /\bnuget(?:\.org)?\b/i, publish: /nuget push|dotnet nuget|NUGET_API_KEY|nuget\.org|environment:\s*nuget\b/i },
    { name: "Maven Central", mention: /\bmaven central\b|\bsonatype\b/i, publish: /maven|sonatype|central\.sonatype|publishToMavenCentral|gradle.*publish/i },
    { name: "Docker Hub", mention: /\bdocker hub\b|\bdockerhub\b/i, publish: /docker\/login-action|docker\.io|dockerhub|DOCKER_USERNAME|docker push/i },
    { name: "GHCR", mention: /\bghcr(?:\.io)?\b|\bgithub container registry\b/i, publish: /ghcr\.io/i },
];
const PUBLISH_CLAIM = /\b(?:publish(?:ed|es|ing)?|available|install(?:able)?|ships?|shipped|pushed|released|now on|from)\b/i;
const VERSION = /(?<![\w.-])v?(\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)(?![\w.-]*\w)/g;

const warnings = [];
const warn = (title, message) => warnings.push({ title, message });
const git = (...args) => execFileSync("git", args, { encoding: "utf8", maxBuffer: 256 * 1024 * 1024, stdio: ["ignore", "pipe", "pipe"] });
const tryGit = (...args) => {
    try {
        return git(...args);
    } catch {
        return null;
    }
};
const escapeData = value => value.replace(/%/g, "%25").replace(/\r/g, "%0D").replace(/\n/g, "%0A");
const escapeProperty = value => escapeData(value).replace(/:/g, "%3A").replace(/,/g, "%2C");
const quote = text => (text.length > 100 ? `${text.slice(0, 97)}...` : text).replace(/`/g, "'");

// The bullets under the allowed sections, each with its continuation lines joined; code fences dropped.
function bullets(body) {
    const lines = body.replace(/\r\n?/g, "\n").replace(/^[ \t]*(`{3,}|~{3,})[\s\S]*?^[ \t]*\1[ \t]*$/gm, "").replace(/<!--[\s\S]*?-->/g, "").split("\n");
    const found = [];
    let section = null;
    let current = null;
    for (const line of lines) {
        const heading = /^ {0,3}(#{1,6})\s+(.*?)\s*#*\s*$/.exec(line);
        if (heading) {
            current = null;
            if (heading[1].length <= 2) {
                section = heading[2].replace(/[*_`]/g, "").trim().toLowerCase();
            }
            continue;
        }
        if (!ALLOWED.includes(section)) {
            continue;
        }
        const item = /^(\s*)(?:[-*+]|\d{1,9}[.)])\s+(.*)$/.exec(line);
        if (item) {
            current = { text: item[2] };
            found.push(current);
        } else if (current && /^\s+\S/.test(line)) {
            current.text += ` ${line.trim()}`;
        } else if (!line.trim()) {
            continue;
        } else {
            current = null;
        }
    }
    return found;
}

async function readBody() {
    const api = process.env.GITHUB_API_URL || "https://api.github.com";
    if (process.env.GH_TOKEN && process.env.REPOSITORY && process.env.NUMBER) {
        try {
            const response = await fetch(`${api}/repos/${process.env.REPOSITORY}/pulls/${process.env.NUMBER}`, {
                headers: { authorization: `Bearer ${process.env.GH_TOKEN}`, accept: "application/vnd.github+json" },
            });
            if (response.ok) {
                const pull = await response.json();
                return { body: typeof pull.body === "string" ? pull.body : "", base: (pull.base && pull.base.ref) || process.env.BASE || "" };
            }
        } catch {
            // Falls back to the event payload below.
        }
    }
    return { body: process.env.PR_BODY || "", base: process.env.BASE || "" };
}

function compare({ body, base, before, action }) {
    const upstream = `origin/${base}`;
    const mergeBase = tryGit("merge-base", upstream, "HEAD");
    if (!mergeBase) {
        return { skipped: `could not find where this pull request branched from ${upstream}` };
    }
    const mb = mergeBase.trim();
    const names = git("diff", "--name-only", "--no-renames", mb, "HEAD").split("\n").filter(Boolean);
    const patch = tryGit("diff", "-U0", "--no-color", "--no-ext-diff", "--no-textconv", mb, "HEAD");
    const added = [];
    const removed = [];
    for (const line of (patch || "").split("\n")) {
        if (line.startsWith("+") && !line.startsWith("+++")) {
            added.push(line);
        } else if (line.startsWith("-") && !line.startsWith("---")) {
            removed.push(line);
        }
    }
    const changedText = `${names.join("\n")}\n${added.join("\n")}\n${removed.join("\n")}`;
    const addedText = added.join("\n");
    const removedText = removed.join("\n");

    // 1. A bullet naming code the diff no longer touches.
    if (patch !== null) {
        for (const bullet of bullets(body)) {
            const spans = [...bullet.text.matchAll(/(`+)([^`\n]+?)\1/g)].map(match => match[2].trim())
                .filter(span => /^[\w.@/-]{3,}$/.test(span) && !/^v?[\d.]+$/.test(span) && /[A-Za-z]{2}/.test(span));
            if (spans.length > 0 && !spans.some(span => changedText.includes(span)
                || changedText.includes(span.replace(/^\.?\//, "")) || changedText.includes(span.split("/").pop()))) {
                warn("Release notes drift: bullet not in the diff",
                    `The bullet \`${quote(bullet.text)}\` names ${spans.map(span => `\`${span}\``).join(", ")}, which this pull request's diff no longer touches; the change may already be on ${base} or was dropped in conflict resolution. Re-read the bullet against \`git diff origin/${base}...HEAD\`.`);
            }
        }

        // 2. A version the diff only removes.
        const seen = new Set();
        for (const match of body.matchAll(VERSION)) {
            const version = match[1];
            const before = body.slice(Math.max(0, match.index - 12), match.index);
            if (seen.has(version) || /\bfrom\s+$|\bwas\s+$/i.test(before)) {
                continue;
            }
            seen.add(version);
            if (removedText.includes(version) && !addedText.includes(version)) {
                warn("Release notes drift: replaced version",
                    `The description names ${version}, which the diff only removes: it is the version this pull request replaces. Name the version it moves to.`);
            }
        }
    }

    // 3. A registry the publish workflows never push to.
    const workflows = tryGit("grep", "-h", "-i", "-e", ".", "HEAD", "--", ".github/workflows") || "";
    for (const bullet of bullets(body)) {
        for (const registry of REGISTRIES) {
            if (registry.mention.test(bullet.text) && PUBLISH_CLAIM.test(bullet.text) && !registry.publish.test(workflows)) {
                warn("Release notes drift: publish claim",
                    `The bullet \`${quote(bullet.text)}\` names ${registry.name}, but no workflow in .github/workflows publishes there. State only where the release is actually published.`);
            }
        }
    }

    // 4. A merge from the base branch that changed what the pull request contains.
    const parents = (tryGit("rev-list", "--parents", "-n", "1", "HEAD") || "").trim().split(/\s+/).slice(1);
    if (action === "synchronize" && before && !/^0+$/.test(before) && parents.length === 2
        && tryGit("merge-base", "--is-ancestor", parents[1], upstream) !== null
        && tryGit("cat-file", "-e", `${before}^{commit}`) !== null) {
        const oldBase = tryGit("merge-base", before, upstream);
        if (oldBase) {
            const was = new Set(git("diff", "--name-only", "--no-renames", oldBase.trim(), before).split("\n").filter(Boolean));
            const now = new Set(names);
            const dropped = [...was].filter(name => !now.has(name));
            const gained = [...now].filter(name => !was.has(name));
            const changed = dropped.length + gained.length;
            if (dropped.length > 0 || changed >= Math.max(3, Math.ceil(was.size * 0.25))) {
                const list = files => files.slice(0, 5).map(name => `\`${name}\``).join(", ") + (files.length > 5 ? ` and ${files.length - 5} more` : "");
                warn("Release notes drift: merge from the base branch",
                    `The merge from ${base} changed what this pull request contains${dropped.length ? `: it no longer changes ${list(dropped)}` : ""}${gained.length ? `${dropped.length ? ", and" : ":"} it now changes ${list(gained)}` : ""}. Re-read the notes against \`git diff origin/${base}...HEAD\`.`);
            }
        }
    }
    return { skipped: null };
}

(async () => {
    let outcome;
    try {
        const { body, base } = await readBody();
        outcome = compare({ body, base, before: process.env.BEFORE || "", action: process.env.ACTION || "" });
    } catch (error) {
        outcome = { skipped: `the comparison failed: ${error.message.split("\n")[0]}` };
    }
    const summary = [];
    if (outcome.skipped) {
        console.log(`::notice title=Release notes drift not checked::${escapeData(`Not compared with the diff: ${outcome.skipped}.`)}`);
        summary.push(`Not compared with the diff: ${outcome.skipped}.`);
    } else if (warnings.length === 0) {
        console.log("The release notes match the diff as far as this job can tell.");
        summary.push("The release notes match the diff as far as this job can tell.");
    }
    for (const warning of warnings) {
        console.log(`::warning title=${escapeProperty(warning.title)}::${escapeData(warning.message)}`);
        summary.push(`- **${warning.title.replace(/^Release notes drift: /, "")}**: ${warning.message}`);
    }
    if (process.env.GITHUB_STEP_SUMMARY) {
        fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, `### Release notes and the diff\n\n${summary.join("\n")}\n`);
    }
    process.exit(0);
})();

}
