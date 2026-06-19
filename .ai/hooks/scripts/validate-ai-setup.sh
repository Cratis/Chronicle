#!/usr/bin/env bash
# Validates the AI corpus in this repo: structural integrity (frontmatter) and adapter
# health for each tool's actual conventions —
#   Copilot: .github/copilot-instructions.md, .github/instructions/<n>.instructions.md (applyTo),
#            .github/agents/<n>.agent.md, .github/prompts (folder), .github/skills (folder)
#   Claude:  .claude/CLAUDE.md, .claude/rules/<n>.md (paths), .claude/agents (folder),
#            .claude/commands/<n>.md, .claude/skills (folder)   [hooks live in .claude/settings.json]
#   Codex:   AGENTS.md, .agents/skills (folder)
# plus a set of content drift guards. Structural/adapter/Codex checks are FATAL; drift guards are
# WARNINGS. Portable: needs only bash + grep + sed (no ripgrep). Run from anywhere; it cd's to root.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$root"

failed=0
fail() { printf 'ai-corpus FAIL: %s\n' "$1" >&2; failed=1; }
warn() { printf 'ai-corpus warn: %s\n' "$1" >&2; }

# ── Structural: required paths ──
for p in .ai .ai/rules .ai/agents .ai/prompts .ai/skills .ai/hooks; do
    [[ -e "$p" ]] || fail "missing required path: $p"
done

# ── Structural: rules (general.md is the intentionally frontmatter-less root) ──
for rule in .ai/rules/*.md; do
    [[ -e "$rule" ]] || continue
    [[ "$rule" == ".ai/rules/general.md" ]] && continue
    if [[ "$(sed -n '1p' "$rule")" != "---" ]]; then fail "$rule: missing YAML frontmatter"; continue; fi
    grep -Eq '^applyTo:' "$rule" || fail "$rule: frontmatter must include applyTo"
    # profile (optional) must be application|framework|universal when present (absent = universal)
    if grep -Eq '^profile:' "$rule" && ! grep -Eq '^profile:[[:space:]]*(application|framework|universal)[[:space:]]*$' "$rule"; then
        fail "$rule: profile must be application|framework|universal"
    fi
done

# ── Structural: skills (name + description) ──
for skill in .ai/skills/*/SKILL.md; do
    [[ -e "$skill" ]] || continue
    if [[ "$(sed -n '1p' "$skill")" != "---" ]]; then fail "$skill: missing YAML frontmatter"; continue; fi
    grep -Eq '^name:' "$skill" || fail "$skill: frontmatter must include name"
    grep -Eq '^description:' "$skill" || fail "$skill: frontmatter must include description"
done

# ── Structural: prompts must be *.prompt.md with frontmatter + description ──
for p in .ai/prompts/*.md; do
    [[ -e "$p" ]] || continue
    case "$p" in *.prompt.md) ;; *) fail "$p: prompt must use the .prompt.md suffix (no plain-.md stubs)"; continue;; esac
    if [[ "$(sed -n '1p' "$p")" != "---" ]]; then fail "$p: missing frontmatter"; continue; fi
    grep -Eq '^description:' "$p" || fail "$p: prompt missing description frontmatter"
done

# ── Adapter integrity: every rule resolves to its canonical .ai/rules/<name>.md through both
#    a GitHub adapter (.github/instructions/<name>.instructions.md) and a Claude adapter
#    (.claude/rules/<name>.md). Each may be a symlink OR a path-reference file — both encode the
#    same relative target; the check is that it *resolves* to the right rule, not its file type. ──
adapter_target() {  # prints the relative target encoded by an adapter (symlink target or file body)
    local p="$1"
    if [[ -L "$p" ]]; then readlink "$p"; else cat "$p"; fi
}
for rule in .ai/rules/*.md; do
    [[ -e "$rule" ]] || continue
    name="$(basename "$rule" .md)"; [[ "$name" == general ]] && continue
    expected="../../.ai/rules/$name.md"
    gh=".github/instructions/$name.instructions.md"
    cl=".claude/rules/$name.md"
    if [[ ! -e "$gh" ]]; then fail "$gh: missing GitHub instruction adapter"
    elif [[ "$(adapter_target "$gh")" != "$expected" ]]; then fail "$gh: expected target '$expected'"; fi
    if [[ ! -e "$cl" ]]; then fail "$cl: missing Claude rule adapter"
    elif [[ "$(adapter_target "$cl")" != "$expected" ]]; then fail "$cl: expected target '$expected'"; fi
done

# ── Adapter targets resolve (catches dangling adapters) ──
for gh in .github/instructions/*.instructions.md; do
    [[ -e "$gh" ]] || continue
    target=".ai/rules/$(basename "$gh" .instructions.md).md"
    [[ -f "$target" ]] || fail "$gh: adapter points at missing rule $target"
done

# ── Folder-level symlinks each tool consumes directly (same convention both sides) ──
#    Copilot: prompts (.github/prompts/*.prompt.md), skills (.github/skills/<n>/SKILL.md).
#    Claude:  agents (.claude/agents/<n>.md), skills (.claude/skills/<n>/SKILL.md).
for link in .github/prompts .github/skills .claude/agents .claude/skills; do
    if [[ ! -e "$link" ]]; then fail "missing link path: $link"
    elif [[ ! -L "$link" ]]; then fail "expected symlink but found regular path: $link"; fi
done

# ── Copilot custom-agent adapters: Copilot requires .github/agents/<name>.agent.md
#    (the .agent.md suffix); the Claude side uses the .claude/agents folder symlink above. ──
for agent in .ai/agents/*.md; do
    [[ -e "$agent" ]] || continue
    name="$(basename "$agent" .md)"
    gh=".github/agents/$name.agent.md"; expected="../../.ai/agents/$name.md"
    if [[ ! -e "$gh" ]]; then fail "$gh: missing Copilot agent adapter (.agent.md suffix required)"
    elif [[ "$(adapter_target "$gh")" != "$expected" ]]; then fail "$gh: expected target '$expected'"; fi
done

# ── Claude slash-command adapters: Claude reads commands from .claude/commands/<name>.md
#    (not .claude/prompts); the Copilot side uses the .github/prompts folder symlink. ──
for prompt in .ai/prompts/*.prompt.md; do
    [[ -e "$prompt" ]] || continue
    name="$(basename "$prompt" .prompt.md)"
    cl=".claude/commands/$name.md"; expected="../../.ai/prompts/$name.prompt.md"
    if [[ ! -e "$cl" ]]; then fail "$cl: missing Claude command adapter"
    elif [[ "$(adapter_target "$cl")" != "$expected" ]]; then fail "$cl: expected target '$expected'"; fi
done

# ── General-rule root adapters ──
for f in .github/copilot-instructions.md .claude/CLAUDE.md; do
    [[ -e "$f" ]] || fail "missing general-rule adapter: $f"
done

# ── Codex adapters (we claim Codex support) ──
[[ -L AGENTS.md || -f AGENTS.md ]] || fail "AGENTS.md: missing Codex root adapter (-> .ai/rules/general.md)"
[[ -L .agents/skills ]]            || fail ".agents/skills: missing Codex skills adapter (-> ../.ai/skills)"

# ── Hook files ──
for hook in .ai/hooks/pre-commit.md .ai/hooks/agent-stop.md; do
    [[ -e "$hook" ]] || fail "missing hook file: $hook"
done

# ── Content drift guards (WARN only — heuristic, never block on a false positive) ──
if grep -rnE '\.AutoMap\(\)' .ai/rules .ai/skills .ai/agents 2>/dev/null \
        | grep -vE ':[0-9]+:[[:space:]]*#' \
        | grep -vE '(NoAutoMap|never|not |n.t |default|only|disabl)' | grep -q .; then
    warn "possible stale .AutoMap() guidance — AutoMap is on by default; call .From<>() directly"
fi
if grep -rnE '\.instructions\.md' .ai/rules .ai/skills .ai/agents .ai/prompts .ai/hooks 2>/dev/null \
        | grep -vE 'managing-ai-rules|validate-ai-setup' | grep -q .; then
    warn "'.instructions.md' cross-link leaked into canonical docs — use ./<name>.md"
fi
if grep -rnE '\[EventType\("|\[EventType\(name:|\[EventType\(id:' .ai/rules .ai/skills .ai/agents 2>/dev/null \
        | grep -viE 'never|no arg|not allowed' | grep -q .; then
    warn "stale [EventType] argument guidance — new events take no arguments (generation: only for migrations)"
fi
if grep -rnE 'Features/<Feature>/<Slice>|Features/<Feature>/<SliceName>|Source/Core/Features' .ai/rules .ai/skills .ai/agents .ai/prompts 2>/dev/null | grep -q .; then
    warn "possible retired top-level Features/ layout — use <Module>/<Feature>/<Slice>/ (Module optional)"
fi
if grep -rnE 'RouteAttribute|\[Route\(' .ai/rules .ai/skills .ai/agents 2>/dev/null | grep -q .; then
    warn "stale [Route] for model-bound queries — use [Path]"
fi
if grep -rniE 'custom exception to signal|framework converts it to (an? )?(error|failed)' .ai/rules .ai/skills .ai/prompts 2>/dev/null | grep -q .; then
    warn "stale business-rule guidance — return ValidationResult/Result<,>, not a thrown exception"
fi
if grep -rniE '\brtk\b' .ai/rules .ai/skills .ai/agents .ai/prompts 2>/dev/null | grep -q .; then
    warn "rtk reference found — rtk was dropped from this corpus"
fi

if [[ "$failed" -ne 0 ]]; then
    printf 'AI corpus validation FAILED.\n' >&2
    exit 1
fi
printf 'AI corpus validation passed.\n'
