<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/loss-report-template.md -->
# Loss report template

File: `.ai-work/screenplay/<model-slug>/legacy/LOSS.md`. Purpose: say plainly what the candidate model
does **not** know, so nobody mistakes it for a complete description of the system. Keep it
short; cite evidence rows (`E12`) and questions (`Q3`) instead of repeating content.

```markdown
# Loss report: <system> -> <model> (source identity <commit + digest from the source-identity helper>)

## Scope
- Systems, repositories and environments examined; dates.
- Evidence sources used: generate (<provider>, CLI <version>), Prologue (<sources>, capture
  window, scenario script), UI walk, documents, experts. Sources not used and why.

## Coverage gaps (from L0)
| Entry-point family | Why not covered | What was done instead | Residual risk |

## Tool losses
- Generator diagnostics not Lowered: <count by family>; resolved <n>, waived <n> (reasons),
  open <n> (rows).
- Prologue blind spots that apply here: request bodies, GETs, PATCH, non-proxied routes,
  status codes (ignored), time-only correlation, PostgreSQL column over-reporting.
- Generator lag that applies here: generations, appended-event specs, reactions, triggers.

## Not yet extracted
Features still at Pass 0 (`re-entry-and-passes.md`), each with the reason and what would trigger its
next pass. They are never counted as done. Features whose evidence is `stale` after re-entry are
listed too.

## Behaviour not observed
Routes, jobs or tables the inventory found but no evidence exercised. Each is `unknown`,
with the bounded check done (for example "searched for callers: none found in 3 files")
and the question raised.

## Rules not enforced or not expressible today
One line per rule, named individually (never one generic platform caveat): the rule, its
evidence row, the invariant and consistency risk, and the target enforcement. State-dependent
checks use `reads` + `require`: eligible reads opt into v25 protection, while legacy/refused
shapes are marked unenforced and Stage realization remains a separate gap;
date rules and opaque policies that no declaration can state stay in the slice `documentation` and here.

## Unknowns and undecided target decisions
| Row/question | What is unknown | Who can answer | Candidate assumption |

## Confidence per slice
| Slice | Evidence rows | Strongest source | Expert verdict | Confidence (high/medium/low) | Why |

## Capture cleanup
Confirmed by <who> on <date> (rows), or "no capture run".
```

Wording: "candidate", "recovered structure", "unverified". Never "lossless", "complete",
"automatic" or "equivalent".

MCP `semantic-diff` compares canonical exports of the same application. It does not guess
renames, run specifications or prove recovered behavior, so a structural match is never an
execution-equivalence claim. Inspect its incomplete sections before quoting it in the loss report.
