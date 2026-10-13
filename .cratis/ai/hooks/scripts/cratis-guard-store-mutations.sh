#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-guard-store-mutations.sh
# PreToolUse hook (Bash) — hard block on `cratis chronicle` commands that are not read-only.
#
# The cratis-chronicle-cli-operations skill makes `--yes` the authorization boundary for changing a
# live Chronicle store, and says a request to diagnose does not authorize a mutation. This guard is
# what enforces that: every `cratis` invocation in the shell command is found and, for the
# `chronicle` group, classified against cratis-store-mutations.json. Anything that is not on the
# read-only allowlist — replay, retry, quarantine clearing, job control, recommendations, users,
# applications, subscriptions, and any command a newer CLI adds — exits 2 (block the tool call,
# stderr goes back to the model). Unknown commands fail closed.
#
# Other groups (ai, arc, context, llm, screenplay, prologue, completions) and the top-level commands
# (init, new, render, run, update, version, ...) are out of scope and always allowed.
#
# What it sees is the command text. A `cratis` reached through a variable, an alias, a function or a
# script file is invisible to it; see .cratis/ai/hooks/README.md for the full list of limits.
#
# Escape hatch for a human who authorized the mutation — set in the environment the agent harness was
# started from, never inside the command (an assignment in the command is ignored):
#   CRATIS_HOOKS_ALLOW_STORE_MUTATIONS=1
# Data file override (replaces the shipped lists; a .local.json beside this script only adds to them):
#   CRATIS_HOOKS_STORE_MUTATIONS=<path>
set -euo pipefail

# SCRIPTDIR, not a path relative to the caller: shellcheck resolves a plain relative `source=`
# against the current working directory, and these hooks are linted from wherever CI happens to run.
# shellcheck source=SCRIPTDIR/hook-lib.sh
. "$(dirname "${BASH_SOURCE[0]}")/hook-lib.sh"

[ "${CRATIS_HOOKS_ALLOW_STORE_MUTATIONS:-0}" = "1" ] && exit 0

input="$(hook_read_stdin)"
[ -n "$input" ] || exit 0
hook_have jq || exit 0

command_text="$(hook_json "$input" '.tool_input.command')"
[ -n "$command_text" ] || exit 0

# Cheap exit for the overwhelming majority of shell commands, which never mention the CLI.
case "$(printf '%s' "$command_text" | tr '[:upper:]' '[:lower:]')" in
    *cratis*) ;;
    *) exit 0 ;;
esac

# ── Command lists: shipped defaults + optional local extension, or a replacement ─────────
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
data_file="${CRATIS_HOOKS_STORE_MUTATIONS:-$here/cratis-store-mutations.json}"
local_file="$here/cratis-store-mutations.local.json"

merged=""
error_file="$data_file"
if [ -f "$data_file" ]; then
    merged="$(jq -ce 'select(type == "object" and (.group | type == "string") and
        (.readOnly | type == "array") and (.mutating | type == "array") and
        (.helpOptions | type == "array") and (.versionOptions | type == "array") and
        (.flagOptions | type == "array") and (.valueOptions | type == "array"))' "$data_file" 2>/dev/null || true)"
fi
if [ -n "$merged" ] && [ -f "$local_file" ]; then
    # A local file adds entries; it never removes one. A command it lists as mutating is blocked even
    # when the shipped file calls it read-only, because the classifier checks "mutating" first.
    extended="$(printf '%s' "$merged" | jq -c --slurpfile local "$local_file" '
        . as $base | $local[0] as $add
        | if ($add | type) != "object" or
             (["readOnly", "mutating", "helpOptions", "versionOptions", "flagOptions", "valueOptions"]
              | any(. as $key | ($add[$key] != null and ($add[$key] | type) != "array")))
          then error("invalid local list") else . end
        | $base + (["readOnly", "mutating", "helpOptions", "versionOptions", "flagOptions", "valueOptions"]
            | map({key: ., value: (($base[.] // []) + ($add[.] // []))}) | from_entries)
    ' 2>/dev/null || true)"
    if [ -n "$extended" ]; then
        merged="$extended"
    else
        merged=""
        error_file="$local_file"
    fi
fi

# An unreadable list is not an empty allowlist that happens to pass: every chronicle command is then
# refused, and the message says why.
data_ok=1
[ -n "$merged" ] || { data_ok=0; merged='{}'; }

list() {
    printf '%s' "$merged" | jq -r "$1" 2>/dev/null || true
}

group="$(list '.group // "chronicle"')"
read_only="$(list '(.readOnly // [])[] | .command // empty')"
mutating="$(list '(.mutating // [])[] | select(.command) | "\(.command)\t\(.effect // "")"')"
help_options="$(list '(.helpOptions // [])[]')"
version_options="$(list '(.versionOptions // [])[]')"
flag_options="$(list '(.flagOptions // [])[]')"
value_options="$(list '(.valueOptions // [])[]')"

tab_char="$(printf '\t')"

# ── Find and classify every cratis invocation ─────────────────────────────────────────────
# One awk pass: a small shell tokenizer (quotes, escapes, $( ) and backticks, subshells, pipelines
# and lists, redirections, here-documents) splits the command into simple commands; each is checked
# for `cratis` in command position, behind env assignments, keywords and wrappers (rtk proxy, env,
# sudo, timeout, xargs, ...). `bash -c '...'`, `eval ...` and scripts piped or redirected into a
# shell are queued and tokenized again. Output: one line per blocked invocation,
#   <kind> TAB <command path> TAB <effect>
# where kind is mutating, unknown or unparsed.
findings="$(
    CSM_COMMAND="$command_text" \
    CSM_GROUP="$group" \
    CSM_READONLY="$read_only" \
    CSM_MUTATING="$mutating" \
    CSM_HELP="$help_options" \
    CSM_VERSION="$version_options" \
    CSM_FLAGS="$flag_options" \
    CSM_VALUES="$value_options" \
    LC_ALL=C awk '
    function toset(text, set,    parts, n, k) {
        n = split(text, parts, "\n")
        for (k = 1; k <= n; k++) if (parts[k] != "") set[tolower(parts[k])] = 1
    }
    function words_in(text,    parts) { return split(text, parts, " ") }
    function norm(text) {
        text = tolower(text); gsub(/[ \t]+/, " ", text); sub(/^ /, "", text); sub(/ $/, "", text)
        return text
    }
    function report(kind, path, effect,    key) {
        key = kind SUBSEP path
        if (key in reported) return
        reported[key] = 1
        printf "%s\t%s\t%s\n", kind, path, effect
    }
    function enqueue(text) {
        if (text == "" || !index(tolower(text), "cratis")) return
        queue[++queued] = text
    }
    function base(word) { word = tolower(word); sub(/.*\//, "", word); return word }

    # ── tokenizer state ──
    function end_word() {
        if (inword) {
            if (index(tolower(word), "cratis") && candidates < 256) candidate[++candidates] = word
            if (skip_next) skip_next = 0
            else current = current (current == "" ? "" : SEP) (quoted ? "Q" : "U") word
        }
        word = ""; quoted = 0; inword = 0
    }
    function emit() {
        end_word()
        if (current != "") analyze(current)
        current = ""
    }
    function push(kind) {
        depth++
        s_current[depth] = current; s_word[depth] = word; s_quoted[depth] = quoted
        s_inword[depth] = inword; s_dq[depth] = in_dq; s_kind[depth] = frame
        s_parens[depth] = parens; s_skip[depth] = skip_next
        current = ""; word = ""; quoted = 0; inword = 0; in_dq = 0
        frame = kind; parens = 0; skip_next = 0
    }
    function pop() {
        emit()
        current = s_current[depth]; word = s_word[depth] "$()"; quoted = s_quoted[depth]
        inword = 1; in_dq = s_dq[depth]; frame = s_kind[depth]
        parens = s_parens[depth]; skip_next = s_skip[depth]
        depth--
    }
    # Index just past the parenthesis (or brace) group opening at i.
    function skip_group(s, i, opener, closer,    n, level, c) {
        n = length(s); level = 0
        for (; i <= n; i++) {
            c = substr(s, i, 1)
            if (c == opener) level++
            else if (c == closer) { level--; if (level == 0) return i + 1 }
        }
        return n + 1
    }
    # Read the bodies of the pending here-documents, which start at i. A body is data unless a
    # shell reads it as a script, so it becomes a candidate rather than a command.
    function heredocs(s, i,    n, k, nl, line, body, test) {
        n = length(s)
        for (k = 1; k <= pending; k++) {
            body = ""
            while (i <= n) {
                nl = index(substr(s, i), "\n")
                if (nl == 0) { line = substr(s, i); i = n + 1 } else { line = substr(s, i, nl - 1); i += nl }
                test = line
                if (strip[k]) sub(/^\t+/, "", test)
                if (test == delimiter[k]) break
                body = body line "\n"
            }
            if (index(tolower(body), "cratis") && candidates < 256) candidate[++candidates] = body
        }
        pending = 0
        return i
    }

    function parse(s,    n, i, c, c2, j, k) {
        current = ""; word = ""; quoted = 0; inword = 0; skip_next = 0
        in_sq = 0; in_dq = 0; depth = 0; frame = "top"; parens = 0
        pending = 0; candidates = 0; stdin_shell = 0
        n = length(s); i = 1
        while (i <= n) {
            c = substr(s, i, 1)
            if (in_sq) {
                if (c == "\047") in_sq = 0; else word = word c
                i++; continue
            }
            if (in_dq) {
                if (c == "\\") {
                    c2 = substr(s, i + 1, 1)
                    if (c2 == "\n") { i += 2; continue }
                    if (c2 == "\"" || c2 == "\\" || c2 == "$" || c2 == "`") { word = word c2; i += 2; continue }
                    word = word c; i++; continue
                }
                if (c == "\"") { in_dq = 0; i++; continue }
                if (c == "$" && substr(s, i + 1, 1) == "(") {
                    if (substr(s, i + 2, 1) == "(") { j = skip_group(s, i + 1, "(", ")"); word = word substr(s, i, j - i); i = j; continue }
                    push("subst"); i += 2; continue
                }
                if (c == "`") { if (frame == "backtick") pop(); else push("backtick"); i++; continue }
                word = word c; i++; continue
            }
            if (c == "\\") {
                c2 = substr(s, i + 1, 1)
                if (c2 != "\n") { word = word c2; inword = 1 }
                i += 2; continue
            }
            if (c == "\047") { in_sq = 1; inword = 1; quoted = 1; i++; continue }
            if (c == "\"") { in_dq = 1; inword = 1; quoted = 1; i++; continue }
            if (c == "#" && !inword) { while (i <= n && substr(s, i, 1) != "\n") i++; continue }
            if (c == "$") {
                c2 = substr(s, i + 1, 1)
                if (c2 == "(" && substr(s, i + 2, 1) == "(") { j = skip_group(s, i + 1, "(", ")"); word = word substr(s, i, j - i); inword = 1; i = j; continue }
                if (c2 == "(") { push("subst"); i += 2; continue }
                if (c2 == "{") { j = skip_group(s, i + 1, "{", "}"); word = word substr(s, i, j - i); inword = 1; i = j; continue }
                word = word c; inword = 1; i++; continue
            }
            if (c == "`") { if (frame == "backtick") pop(); else push("backtick"); i++; continue }
            if (c == "(") {
                if (!inword && substr(s, i + 1, 1) == "(") { i = skip_group(s, i, "(", ")"); continue }
                emit(); parens++; i++; continue
            }
            if (c == ")") {
                if (parens > 0) { emit(); parens--; i++; continue }
                if (frame == "subst") { pop(); i++; continue }
                emit(); i++; continue
            }
            if (c == "<" || c == ">") {
                c2 = substr(s, i + 1, 1)
                if (c2 == "(") { end_word(); push("subst"); i += 2; continue }
                if (inword && !quoted && word ~ /^[0-9]+$/) { word = ""; inword = 0 } else end_word()
                if (c == "<" && c2 == "<") {
                    if (substr(s, i + 2, 1) == "<") { skip_next = 2; i += 3; continue }
                    i += 2; pending++; strip[pending] = 0; delimiter[pending] = ""
                    if (substr(s, i, 1) == "-") { strip[pending] = 1; i++ }
                    while (substr(s, i, 1) == " " || substr(s, i, 1) == "\t") i++
                    while (i <= n) {
                        c2 = substr(s, i, 1)
                        if (c2 ~ /[ \t\n;&|<>()]/) break
                        if (c2 != "\047" && c2 != "\"" && c2 != "\\") delimiter[pending] = delimiter[pending] c2
                        i++
                    }
                    continue
                }
                i++
                if (c2 == "&") {
                    i++
                    if (substr(s, i, 1) ~ /[0-9-]/) { while (i <= n && substr(s, i, 1) ~ /[0-9-]/) i++; continue }
                    skip_next = 1; continue
                }
                if (c2 == ">" || c2 == "|" || (c == "<" && c2 == ">")) i++
                skip_next = 1; continue
            }
            if (c == "&" && substr(s, i + 1, 1) == ">") {
                end_word(); i += 2
                if (substr(s, i, 1) == ">") i++
                skip_next = 1; continue
            }
            if (c == "&" || c == ";" || c == "|") { emit(); i++; continue }
            if (c == "\n") { emit(); i++; if (pending > 0) i = heredocs(s, i); continue }
            if (c == " " || c == "\t" || c == "\r") { end_word(); i++; continue }
            word = word c; inword = 1; i++
        }
        emit()
        while (depth > 0) pop()
        emit()
        # A shell reading its script from stdin (a pipe, a here-document, a here-string, a redirect)
        # runs text the tokenizer only saw as data; re-read every such text that mentions cratis.
        if (stdin_shell) for (k = 1; k <= candidates; k++) enqueue(candidate[k])
    }

    # ── simple commands ──
    function analyze(text,    parts, n, k) {
        n = split(text, parts, SEP)
        for (k = 1; k <= n; k++) { Q[k] = substr(parts[k], 1, 1); W[k] = substr(parts[k], 2) }
        count = n
        from(1)
    }
    function joined(first,    k, out) {
        out = ""
        for (k = first; k <= count; k++) out = out (out == "" ? "" : " ") W[k]
        return out
    }
    function from(first,    i, b, j, bj) {
        i = first
        while (i <= count) {
            if (W[i] ~ /^[A-Za-z_][A-Za-z0-9_]*\+?=/) { i++; continue }
            if (Q[i] == "U" && (W[i] in keyword)) { i++; continue }
            break
        }
        if (i > count) return
        b = base(W[i])
        if (b == "cratis") { classify(i + 1); return }
        if (b in shell) { shell_command(i); return }
        if (b == "eval") { enqueue(joined(i + 1)); return }
        if (b in wrapper) {
            for (j = i + 1; j <= count; j++) {
                bj = base(W[j])
                if (bj == "cratis" || bj == "eval" || (bj in shell)) { from(j); return }
                if (W[j] ~ /[ \t\n;&|]/) enqueue(W[j])
            }
        }
    }
    function shell_command(i,    j) {
        for (j = i + 1; j <= count; j++) {
            if (W[j] ~ /^-[A-Za-z]*c[A-Za-z]*$/ || W[j] == "--command") {
                if (j < count) enqueue(W[j + 1])
                return
            }
        }
        for (j = i + 1; j <= count; j++) {
            if (W[j] == "-o" || W[j] == "+o" || W[j] == "-O" || W[j] == "+O" || W[j] == "--rcfile" || W[j] == "--init-file") { j++; continue }
            if (W[j] == "--") return (j < count) ? 0 : stdin_script()
            if (W[j] ~ /^[-+]/) continue
            return
        }
        stdin_script()
    }
    function stdin_script() { stdin_shell = 1; return 0 }

    # ── one cratis invocation: the words after `cratis` start at first ──
    function is_option(lower) { return lower ~ /^-./ }
    function known_value(lower,    eq) {
        eq = index(lower, "=")
        return eq > 1 && (substr(lower, 1, eq - 1) in value_option)
    }
    function classify(first,    j, m, x, t, lower, name, values, only_options, count_words, path, shown, key) {
        values = " "
        for (j = first; j <= count; j++) {
            lower = tolower(W[j])
            if (!is_option(lower)) break
            if (Q[j] == "U" && ((lower in help_option) || (lower in version_option))) return
            if (lower in flag_option) continue
            if (lower in value_option) { j++; if (j <= count) values = values tolower(W[j]) " "; continue }
            if (known_value(lower)) { values = values substr(lower, index(lower, "=") + 1) " "; continue }
            # An option this guard does not know, before the command group: it cannot tell which word is
            # the group, so any mention of the guarded group fails closed.
            for (m = j + 1; m <= count; m++) {
                if (tolower(W[m]) == group) { t = W[j]; sub(/=.*/, "=...", t); report("unparsed", "cratis " t " ... " group, ""); return }
            }
            return
        }
        if (j > count) return
        name = tolower(W[j])
        if (index(name, "$") || index(name, "`") || index(name, "*") || index(name, "?") || index(name, "[")) {
            report("unparsed", "cratis " W[j], ""); return
        }
        if (name != group) {
            if (index(values, " " group " ")) report("unparsed", "cratis ... " group, "")
            return
        }
        for (m = j + 1; m <= count; m++) {
            if (W[m] == "--") break
            if (Q[m] == "U" && ((tolower(W[m]) in help_option) || (tolower(W[m]) in version_option))) return
        }
        count_words = 0; only_options = 1
        for (m = j + 1; m <= count && count_words < max_depth; m++) {
            lower = tolower(W[m])
            if (lower == "--") { only_options = 0; continue }
            if (only_options && is_option(lower)) {
                if (lower in flag_option) continue
                if (lower in value_option) { m++; continue }
                if (known_value(lower)) continue
                break
            }
            count_words++; path[count_words] = lower; shown[count_words] = W[m]
        }
        if (count_words == 0) { report("unknown", "cratis " W[j], ""); return }
        for (m = count_words; m >= 1; m--) {
            key = path[1]; t = shown[1]
            for (x = 2; x <= m; x++) { key = key " " path[x]; t = t " " shown[x] }
            if (key in mutating) { report("mutating", "cratis " W[j] " " t, mutating[key]); return }
            if (key in read_only) return
        }
        t = shown[1]
        for (x = 2; x <= count_words; x++) t = t " " shown[x]
        report("unknown", "cratis " W[j] " " t, "")
    }

    BEGIN {
        SEP = "\037"
        group = tolower(ENVIRON["CSM_GROUP"]); if (group == "") group = "chronicle"
        max_depth = 0
        n = split(ENVIRON["CSM_READONLY"], lines, "\n")
        for (k = 1; k <= n; k++) {
            key = norm(lines[k]); if (key == "") continue
            read_only[key] = 1
            if (words_in(key) > max_depth) max_depth = words_in(key)
        }
        n = split(ENVIRON["CSM_MUTATING"], lines, "\n")
        for (k = 1; k <= n; k++) {
            tab = index(lines[k], "\t"); if (tab == 0) continue
            key = norm(substr(lines[k], 1, tab - 1)); if (key == "") continue
            mutating[key] = substr(lines[k], tab + 1)
            if (words_in(key) > max_depth) max_depth = words_in(key)
        }
        if (max_depth < 2) max_depth = 2
        toset(ENVIRON["CSM_HELP"], help_option)
        toset(ENVIRON["CSM_VERSION"], version_option)
        toset(ENVIRON["CSM_FLAGS"], flag_option)
        toset(ENVIRON["CSM_VALUES"], value_option)
        toset("bash\nsh\nzsh\ndash\nksh\nmksh\nfish\nsu", shell)
        toset("!\n{\n}\nif\nthen\nelse\nelif\ndo\nwhile\nuntil\ntime", keyword)
        toset("env\ncommand\nexec\nbuiltin\nnohup\ntime\nnice\nionice\nsudo\ndoas\ntimeout\ngtimeout\nxargs\nparallel\nstdbuf\nunbuffer\ncaffeinate\nwatch\nrtk\ndotnet\narch\nssh\nnoglob\nnocorrect\ntaskset\nflock\nsetsid\nchronic", wrapper)

        queued = 1; queue[1] = ENVIRON["CSM_COMMAND"]
        for (done = 1; done <= queued; done++) {
            if (done > 64) { report("unparsed", "(more than 64 nested shell scripts)", ""); break }
            if (index(tolower(queue[done]), "cratis")) parse(queue[done])
        }
    }
    '
)" || findings="unparsed${tab_char}(the classifier failed, so no cratis command in it can be checked)${tab_char}"

[ -n "$findings" ] || exit 0

# ── Block ─────────────────────────────────────────────────────────────────────────────────
{
    printf 'BLOCKED by cratis-guard-store-mutations: this command runs cratis %s commands that are not read-only.\n\n' "$group"
    while IFS="$tab_char" read -r kind path effect; do
        [ -n "$kind" ] || continue
        case "$kind" in
            mutating) printf '  - %s: %s.\n' "$path" "$effect" ;;
            unparsed) printf '  - %s: the guard cannot tell which command this runs, so it is treated as a mutation.\n' "$path" ;;
            *) printf '  - %s: not on the read-only allowlist, so it is treated as a mutation.\n' "$path" ;;
        esac
    done <<EOF
$findings
EOF
    if [ "$data_ok" = "1" ]; then
        printf '\nRead-only inspection commands and --help are allowed.\n'
    else
        printf '\nThe command lists could not be read from %s, so every cratis %s command is refused until they can.\n' "$error_file" "$group"
    fi
    cat <<EOF

The cratis-chronicle-cli-operations skill makes --yes the authorization boundary for a live Chronicle
store: a request to inspect or diagnose does not authorize a replay, a retry, clearing quarantine, job
control, a recommendation, or adding or removing users, applications or subscriptions.

Do not retry this, rephrase it, or work around the guard (another wrapper, a script file, a variable,
an alias, the terminal Workbench). Stop and report to the user:
  1. the exact command you intended to run;
  2. the target it resolves to: the context ('cratis context show'), or the --server /
     CHRONICLE_CONNECTION_STRING it uses - never print credentials;
  3. what it changes and why that is needed, and how the change is undone;
then ask the user to run it themselves or to authorize it. A person who authorizes it sets
CRATIS_HOOKS_ALLOW_STORE_MUTATIONS=1 in the environment the agent harness was started from; setting it
inside the command has no effect.
EOF
} >&2
exit 2
