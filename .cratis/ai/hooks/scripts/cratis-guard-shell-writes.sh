#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-guard-shell-writes.sh
# Whole-command reference filter, not a shell interpreter or sandbox.
# The Write/Edit guard is the only protected-path policy. With any protected reference,
# accept only the reviewed command language below and its explicit copy-destination grammar.
set -euo pipefail
export LC_ALL=C
script_directory="${BASH_SOURCE[0]%/*}"
[ "$script_directory" != "${BASH_SOURCE[0]}" ] || script_directory=.
# shellcheck source=SCRIPTDIR/hook-lib.sh
. "$script_directory/hook-lib.sh"
[ "${CRATIS_HOOKS_ALLOW_PROTECTED_WRITES:-0}" = 1 ] && exit 0
input=""
IFS= read -r -d '' input || true
[ -n "$input" ] || exit 0
hook_require_jq cratis-guard-shell-writes CRATIS_HOOKS_ALLOW_PROTECTED_WRITES
parser_failed() {
    printf '%s\n' 'BLOCKED by cratis-guard-shell-writes: parser failed; cannot check this tool call.' >&2
    exit 2
}
cwd="" command_text="" parser_status=""
{
    IFS= read -r -d '' cwd
    IFS= read -r -d '' command_text
    IFS= read -r -d '' parser_status
} < <(jq -j -s '
    def field(path): ([try (path // empty) catch empty] | first // "")
        | if type == "string" then . else tojson end | gsub("\u0000"; "");
    if length == 1 then .[0] else error("expected one JSON document") end
    | [field(.cwd), field(.tool_input.command)] | .[] | ., "\u0000"
' <<<"$input" 2>/dev/null && printf 'ok\0') || parser_failed
[ "$parser_status" = ok ] || parser_failed
[ -n "$command_text" ] || exit 0
cwd="${cwd:-$PWD}"
[ "${#command_text}" -le 131072 ] || parser_failed

# Only standalone corpus installer/repair invocations may bypass reference classification.
# The helper accepts a deliberately smaller language than the shell, never shell evaluation.
if [[ "$command_text" =~ ^[[:space:]]*(cratis|ln)[[:space:]] ]] && command -v node >/dev/null 2>&1; then
    repair_status=0
    node "$script_directory/cratis-guard-shell-writes-repair.mjs" "$cwd" "$command_text" || repair_status=$?
    case "$repair_status" in
        0) exit 0 ;;
        1) ;; # Not an installer/repair exception: preserve the ordinary reference policy.
        *)
            printf '%s\n' 'BLOCKED by cratis-guard-shell-writes: managed link repair was refused; use the installer or report the intended change.' >&2
            exit 2
            ;;
    esac
fi

IFS= read -r -d '' program <<'AWK' || true
function candidate(s) {
    if (checking_marker) return
    if (s == "" || s ~ /[\n\r\t]/) return
    if (collecting) {
        if (s in collected) return
        if (++collected_count > 256 || length(s) > 4096) exit 2
        collected[s] = 1
    } else {
        if (s in seen) return
        if (++candidates > 256 || length(s) > 4096) exit 2
        seen[s] = 1; print s
    }
    if (s != tolower(s)) candidate(tolower(s))
    # A generic attached option prefix must not hide a path spelling (-t.pi/...).
    if (s ~ /^-/ && match(s, /[.\/]/) && RSTART > 1) candidate(substr(s, RSTART))
}
# An extended-glob group (`@(a|b)`, `*(x)`) is also read as one star, so `.e@(n)v` meets `.env`.
# A word with a brace span and quoting or expansion syntax is also kept whole, so the classifier sees it unsplit.
# Whole-quoted words and `${...}` parameter expansions do not brace-expand and are not kept.
function extglob_star(s,    guard) {
    while (guard++ < 64 && match(s, /[@?*+!]\([^()]*\)/)) s = substr(s, 1, RSTART - 1) "*" substr(s, RSTART + RLENGTH)
    return s
}
# Quoted text never brace-expands or opens an extended glob, so its quoting, expansion and group syntax is
# neutralized before the classifier, which refuses that syntax in an unquoted spelling.
function quoted_literal(s) {
    if (index(s, "{")) gsub(/[$`"\047\\]/, "_", s)
    gsub(/[@?*+!]\(/, "__", s)
    return s
}
function raw_brace_word(w,    quote, first, bare) {
    # Unknown interpreter/program text may concatenate brace fragments: retain its conservative refusal.
    # Modeled readers never send their opaque filters through this fallback.
    if (unparsed && index(w, "{") && !index(w, "}")) candidate("*")
    if (!(w ~ /[$`"\047\\]/)) return
    first = substr(w, 1, 1); quote = (first == "\047" || first == "\"") ? first : ""
    if (quote != "" && substr(w, length(w), 1) == quote && index(substr(w, 2, length(w) - 2), quote) == 0) return
    bare = w; gsub(/\$\{/, "", bare)
    if (index(bare, "{")) candidate(w)
}
function raw_brace_scan(s,    n, i, c, word) {
    n = length(s)
    for (i = 1; i <= n; i++) {
        c = substr(s, i, 1)
        if (c ~ /[ \t\n\r;|&<>]/) { raw_brace_word(word); word = "" }
        else word = word c
    }
    raw_brace_word(word)
}
# After an expansion (or a leading tilde) the prefix of a path is unknown and could be the repository root, so the
# literal remainder is also judged as a path relative to the working directory: the word without its variable name
# and every suffix that starts after a slash. A variable holding a whole protected path stays unseen.
function remainder(word,    rest, tail, count, slash) {
    if (word == "") return
    rest = word
    if (!sub(/^\{[^}]*\}/, "", rest)) sub(/^[A-Za-z0-9_~]+/, "", rest)
    candidate(rest)
    tail = word
    while (count++ < 16 && (slash = index(tail, "/")) > 0) { tail = substr(tail, slash + 1); candidate(tail) }
}
# Expansion syntax is not parsed: a whitespace-delimited chunk holding any of $ ` ~ ( ) { } is judged by every
# suffix that follows a slash in it, so a path after a substitution, a tilde form or a nested expansion is seen
# even when the lexer split the substitution at an inner space.
function chunk_suffixes(chunk,    slash, count) {
    gsub(/["\047\\]/, "", chunk)
    if (chunk !~ /[$`~(){}]/) return
    while (count++ < 16 && (slash = index(chunk, "/")) > 0) { chunk = substr(chunk, slash + 1); candidate(chunk) }
}
function scan(s,    n, i, c, word, whole, flat, expanded, chunk) {
    flat = extglob_star(s)
    if (flat != s) scan(flat)
    n = length(s)
    for (i = 1; i <= n; i++) {
        # The placeholder is one opaque expansion, not a variable name or literal path segment.
        # Preserve the same unknown-prefix suffix views as ${NAME}, without its operator operand.
        if (match_placeholders && substr(s, i, length(MATCH_MARKER)) == MATCH_MARKER) {
            candidate(word); candidate(whole)
            if (expanded || word ~ /^~/) remainder(word)
            word = whole = ""; expanded = 1; chunk = chunk "$"
            i += length(MATCH_MARKER) - 1
            continue
        }
        c = substr(s, i, 1)
        if (c ~ /[ \t\n\r;|&<>]/) { chunk_suffixes(chunk); chunk = "" }
        else chunk = chunk c
        if (c ~ /[ \t\n\r;|&<>()\047"`$=\\]/) {
            candidate(word)
            if (expanded || word ~ /^~/) remainder(word)
            word = ""
            if (c ~ /[`$]/) expanded = 1
            else if (c ~ /[ \t\n\r;|&<>]/) expanded = 0
        }
        else word = word c
        if (c ~ /[ \t\n\r;|&<>()\047"`$]/) { candidate(whole); whole = "" }
        else whole = whole c
    }
    candidate(word); candidate(whole); chunk_suffixes(chunk)
    if (expanded || word ~ /^~/) remainder(word)
    # Braces have a second boundary view; retain the original pattern above too.
    if (s ~ /[{}]/) { gsub(/[{}]/, " ", s); scan(s) }
}
# Reuse ANSI-C decoding for reference spellings, not for interpreting shell code.
function ansi(s, i,    c, c2, v, k, limit, d, digits) {
    ansi_word = ""
    while (i <= length(s)) {
        c = substr(s, i++, 1)
        if (c == "\047") return i
        if (c != "\\") { ansi_word = ansi_word c; continue }
        c2 = substr(s, i++, 1); v = -1
        if (c2 in ESC) v = ESC[c2]
        else if (c2 ~ /^[0-7]$/) {
            v = c2 + 0
            for (k = 0; k < 2 && substr(s, i, 1) ~ /^[0-7]$/; k++) { v = v * 8 + substr(s, i++, 1) }
            v = v % 256
        } else if (c2 == "x" || c2 == "u" || c2 == "U") {
            limit = c2 == "x" ? 2 : c2 == "u" ? 4 : 8; v = digits = 0
            while (digits < limit && (d = tolower(substr(s, i, 1))) != "" && (d in HEX)) { v = v * 16 + HEX[d]; i++; digits++ }
            if (!digits || v > 255) v = -1
        }
        if (v > 0) ansi_word = ansi_word sprintf("%c", v)
        else if (v < 0) ansi_word = ansi_word c2
    }
    return i
}
function unquote(s,    i, c, c2, q, out, quoted) {
    for (i = 1; i <= length(s); i++) {
        c = substr(s, i, 1); c2 = substr(s, i + 1, 1)
        if (c == "$" && c2 == "\047" && q != "\047") { i = ansi(s, i + 2) - 1; out = out ansi_word; continue }
        if (c == "\\" && q != "\047") { if (c2 != "\n") { out = out c2; quoted = quoted c2 }; i++; continue }
        if ((c == "\047" || c == "\"") && (q == "" || q == c)) {
            if (q == c) { candidate(quoted_literal(quoted)); quoted = ""; q = "" } else q = c
            continue
        }
        out = out c; if (q != "") quoted = quoted c
    }
    return out
}
function protected(s) { return s in protected_paths }
# A lexical or structural failure: the command text is not modeled, so write-target mode falls back to
# classifying the whole text. A command the policy does not accept (cmd_failed) does the same: a protected
# reference anywhere in a command that is not entirely read-only must keep refusing it.
# Only when every command is accepted are the words of pure readers dropped from classification.
function fail() { bad = unparsed = 1 }
# Reference extraction for one stretch of raw command text.
# Rewrite extraction data only; the lexer still rejects expansions. Nested and value-bearing
# bodies stay visible because their text can supply a protected path or execute another command.
function strip_expansion_operators(t,    out, start, rest, body, name, op, depth, i, c, count) {
    match_placeholders = 0
    if (!match_rewrite_safe || index(t, MATCH_MARKER)) return t
    while ((start = index(t, "${")) && ++count <= 256) {
        out = out substr(t, 1, start - 1); rest = substr(t, start + 2); depth = 1
        for (i = 1; i <= length(rest); i++) {
            c = substr(rest, i, 1)
            if (c == "{") depth++
            if (c == "}") { depth--; if (!depth) break }
        }
        if (depth) return out substr(t, start)
        body = substr(rest, 1, i - 1); name = op = ""
        if (match(body, /^[A-Za-z_][A-Za-z0-9_]*/)) {
            name = substr(body, 1, RLENGTH); op = substr(body, RLENGTH + 1)
        }
        if (name != "" && (op ~ /^(##?|%%?|\^\^?|,,?)[^$`(){}"\047\\;|&<>[:space:]]*$/ ||
            op ~ /^:([0-9]+| -?[0-9]+)(:([0-9]+| -?[0-9]+))?$/)) {
            out = out MATCH_MARKER; match_placeholders = 1
        } else out = out "${" body "}"
        t = substr(rest, i + 1)
    }
    return out t
}
function emit_text(t) {
    t = strip_expansion_operators(t)
    scan(t); scan(unquote(t)); raw_brace_scan(t)
    if (match_placeholders) {
        # Also retain the possible empty-value spelling, e.g. .e${x%y}nv -> .env. It supplements,
        # never replaces, the expanded/unknown-prefix view above.
        gsub(/\$__CRATIS_MATCH__/, "", t); match_placeholders = 0
        scan(t); scan(unquote(t)); raw_brace_scan(t)
    }
}
function destination_allowed(w, implicit,    s) {
    if (!implicit && w ~ /[$`*?\[{~()]/) return 0
    delete collected; collected_count = 0; collecting = 1
    candidate(w); emit_text(w)
    collecting = 0
    for (s in collected) if (protected(s)) return 0
    return 1
}
function endword() {
    if (!inword) return
    if (redirect) {
        if (!(redirect == 2 && word ~ /^([0-9]+|-)$/)) {
            file_redirect = 1
            redirect_words = redirect_words " " word
            if (changed_cwd || lead_glob) unparsed = 1
            if (changed_cwd || word ~ /[$`*?\[{~()]/ || protected(word)) bad = 1
        }
        redirect = 0
    } else {
        if (++argc > 256) fail()
        W[argc] = word
        globbed[argc] = glob
        leading[argc] = lead_glob
    }
    word = ""; inword = glob = lead_glob = 0
}
# Each exemption permits only the reviewed read/presentation options below. No unknown
# options, long-option abbreviations or implicit value-taking short clusters are accepted.
# Every table entry has a sibling ALLOW test and rejected-option neighbors.
function policy(verb, flags, longs,    words, n, k) {
    verbs[verb] = 1; short_options[verb] = flags
    n = split(longs, words, " ")
    for (k = 1; k <= n; k++) long_options[verb, words[k]] = 1
}
function short_flags(w, flags,    k) {
    if (w !~ /^-[^-]/) return 0
    for (k = 2; k <= length(w); k++) if (!index(flags, substr(w, k, 1))) return 0
    return 1
}
function number(s) { return s ~ /^[+-]?[0-9]+$/ }
function options_allowed(verb, first,    i, w, filters, patterns) {
    for (i = first; i <= argc; i++) {
        w = W[i]
        if (w == "--") return 1
        if (w !~ /^-/ || w == "-") { patterns++; continue }
        if ((verb, w) in long_options) continue
        if (verb == "head" || verb == "tail" || verb == "git log") {
            if (w == "-n" || (verb != "git log" && w == "-c")) {
                if (++i > argc || !number(W[i])) return 0
                continue
            }
            if (w ~ /^-n[+-]?[0-9]+$/ || w ~ /^-[0-9]+$/) continue
        }
        if (verb == "grep" || verb == "egrep" || verb == "fgrep") {
            if (w == "-e") { if (++i > argc) return 0; patterns++; continue }
            if (w ~ /^--(include|exclude)=./) { filters++; continue }
            if (w == "--include" || w == "--exclude") {
                if (++i > argc || W[i] == "" || globbed[i]) return 0
                filters++; continue
            }
            if (w == "-A" || w == "-B" || w == "-C") {
                if (++i > argc || !number(W[i])) return 0
                continue
            }
        }
        if (verb == "git diff" && w ~ /^-U[0-9]+$/) continue
        if (verb == "shellcheck" && w ~ /^--severity=(error|warning|info|style)$/) continue
        if ((verb == "git log" && w ~ /^--(format|pretty)=/) || (verb == "git show" && w ~ /^--format=/)) {
            if (index(w, "%(")) return 0
            continue
        }
        if (verb == "git commit") {
            if (w == "-m" || w == "--message") { if (++i > argc) return 0; continue }
            if (w ~ /^-m./) continue
            if (w == "-F") {
                # Whether the file is protected is decided from the reference list, so its words are kept.
                force_emit = 1
                if (++i > argc || W[i] ~ /[$`*?\[{]/ || protected(W[i])) return 0
                continue
            }
        }
        if ((verb == "rg" && (w == "--glob" || w == "-g")) || (verb == "git grep" && (w == "-e" || w == "--regexp"))) {
            if (++i > argc || globbed[i]) return 0
            continue
        }
        if (!short_flags(w, short_options[verb])) return 0
    }
    return !filters || patterns > 0
}
# awk is read-only only for the shape awk [-F sep] 'program' operands. Every other option (-f, -v, -e, -i,
# --include, --, ...) is refused. awk output, execution and file reads need one of > | system getline close
# fflush or the gawk @ directives; a program containing any of them (in any letter case) is not accepted.
# This deny-list relies on awk having no other output primitive, which holds for POSIX, gawk, mawk and BSD awk.
function awk_allowed(    i, w, program) {
    i = 2
    if (W[i] == "-F") { if (++i > argc || W[i] ~ /[$`]/ || globbed[i]) return 0; i++ }
    else if (W[i] ~ /^-F/) { if (W[i] ~ /[$`]/ || globbed[i]) return 0; i++ }
    if (i > argc || W[i] ~ /^-/ || globbed[i]) return 0
    program = tolower(W[i])
    if (program ~ /[>|@]/ || index(program, "system") || index(program, "getline") || index(program, "close") || index(program, "fflush")) return 0
    for (i++; i <= argc; i++) if (W[i] ~ /^-/ && W[i] != "-") return 0
    return 1
}
# Only literal git -C metadata (with exact arity) or staging/commit commands
# (with their normal option checks) are exempt; no other global option is.
# -C gives no protection from hostile repository config (fsmonitor, hooks, pager,
# diff/textconv helpers). This filter checks syntax, not repository trust.
function directory_metadata() {
    if (argc < 5 || W[3] == "" || W[3] ~ /^-/ || globbed[3]) return 0
    if (argc == 5) return (W[4] == "status" && W[5] == "--short") || (W[4] == "rev-parse" && W[5] == "HEAD") || (W[4] == "branch" && W[5] == "--show-current")
    return argc == 6 && W[4] == "tag" && W[5] == "--list" && W[6] !~ /^-/
}
# Keep this bounded sed grammar in sync with the secrets guard. Program files and unknown commands
# remain conservative; only print/substitution scripts without execution or file-write flags qualify.
function safe_sed_script(p,    delimiter, i, c, spans) {
    if (p ~ /^([0-9]+(,([0-9]+|\$))?|\$)?p$/) return 1
    if (substr(p, 1, 1) != "s" || length(p) < 4) return 0
    delimiter = substr(p, 2, 1)
    if (delimiter ~ /[A-Za-z0-9\\ \t\n]/) return 0
    for (i = 3; i <= length(p); i++) {
        c = substr(p, i, 1)
        if (c == "\n") return 0
        if (c == "\\") { if (++i > length(p) || substr(p, i, 1) == "\n") return 0; continue }
        if (c == delimiter && ++spans == 2) return substr(p, i + 1) ~ /^[gpI0-9]*$/
    }
    return 0
}
function sed_slots(A, s, n,    i, w, program, explicit, ended, scripts) {
    delete SED_DATA
    for (i = s + 1; i <= n && A[i] != "--"; i++) if (A[i] ~ /^(-e|--expression)(=|$)/ || A[i] ~ /^-e./) explicit = 1
    for (i = s + 1; i <= n; i++) {
        w = A[i]
        if (!ended && w == "--") { ended = 1; continue }
        if (!ended && (w ~ /^-[nEr]+$/ || w ~ /^--(quiet|silent|regexp-extended)$/)) continue
        if (!ended && (w == "-e" || w == "--expression")) { if (++i > n) return 0; program = A[i] }
        else if (!ended && w ~ /^(-e.|--expression=)/) { program = w; sub(/^(-e|--expression=)/, "", program) }
        else if (!ended && w ~ /^-/) return 0
        else if (!explicit && !scripts) program = w
        else continue
        if (!safe_sed_script(program)) return 0
        SED_DATA[i] = 1; scripts++
    }
    return scripts > 0
}
# Preserve the original protected-file read lane exactly; newly admitted programs below retain
# the pre-existing conservative file-operand check rather than broadening this exception.
function legacy_print_sed(    i, ended) {
    if (argc < 4 || W[2] != "-n" || W[3] !~ /^([0-9]+(,([0-9]+|\$))?|\$)?p$/) return 0
    for (i = 4; i <= argc; i++) {
        if (!ended && W[i] == "--") { ended = 1; continue }
        if (!ended && W[i] ~ /^-/) return 0
    }
    return 1
}
function print_only_sed(    i, ended) {
    if (legacy_print_sed()) return 1
    if (!sed_slots(W, 1, argc)) return 0
    for (i = 2; i <= argc; i++) {
        if (i in SED_DATA) { if (globbed[i]) return 0; continue }
        if (!ended && W[i] == "--") { ended = 1; continue }
        if (!ended && W[i] ~ /^-/) continue
        # Exempt script data, never the input paths: retain the existing protected-input refusal.
        redirect_words = redirect_words " " W[i]
        if (protected(W[i])) return 0
    }
    return 1
}
# Verbs whose reviewed options are display flags only: a word-leading glob that expands to an option-shaped
# file name can change what they print, never what they write or run. Every other verb refuses a leading glob.
function shell_quote(s) { gsub(/\047/, "\047\\\047\047", s); return "\047" s "\047" }
# Bash 3.2 initializes compgen glob flags only after pathname expansion. Prime that state in
# the same shell after setting and verifying every required option; never enumerate unchecked.
function glob_enumeration_script() {
    return "cd -- \"$1\" && [ -r . ] && [ -x . ] || exit 3; unset GLOBIGNORE; " \
        "shopt -s nullglob dotglob nocaseglob && shopt -q nullglob dotglob nocaseglob || exit 3; : *; " \
        "n=0; while IFS= read -r f; do n=$((n+1)); [ \"$n\" -le 200 ] || exit 3; " \
        "case $f in -*|*[!a-zA-Z0-9._/@+=,:%-]*) exit 3;; esac; printf \"F:%s\\n\" \"$f\"; done < <(compgen -G \"$2\"); echo OK"
}
# rg can execute --pre supplied by a filename. Check real leading-glob matches, never command text.
function reader_globs(    i, cmd, line, count, ok, script) {
    if (changed_cwd || ENVIRON["GSW_GLOB_SAFE"] != "1") return 0
    script = glob_enumeration_script()
    for (i = 2; i <= argc; i++) {
        if (W[i] == "--") return 1
        if (!leading[i]) continue
        cmd = "bash -c " shell_quote(script) " guard-glob " shell_quote(ENVIRON["GSW_CWD"]) " " shell_quote(W[i]) " 2>/dev/null"
        ok = count = 0
        while ((cmd | getline line) > 0) {
            if (line == "OK") { ok = 1; continue }
            if (ok || substr(line, 1, 2) != "F:" || ++count > 200) { ok = 0; break }
        }
        if (close(cmd) != 0 || !ok) return 0
    }
    return 1
}
function glob_safe() {
    return W[1] ~ /^(cat|head|tail|wc|ls|grep|egrep|fgrep|shellcheck)$/ || (W[1] ~ /^(rg|cp|rsync|install)$/ && reader_globs()) || (W[1] == "git" && W[2] == "ls-files")
}
function find_allowed(    i, w, predicates) {
    for (i = 2; i <= argc; i++) {
        w = W[i]
        if (w ~ /^-(name|iname|path|regex|wholename)$/) { if (++i > argc || globbed[i]) return 0; predicates++; continue }
        if (w ~ /^-(maxdepth|mindepth)$/) { if (++i > argc || W[i] !~ /^[0-9]+$/) return 0; continue }
        if (w ~ /^-/ && w !~ /^-(print|print0|type|L|P|H)$/) return 0
        if (w == "-type") { if (++i > argc || W[i] !~ /^[bcdflps]$/) return 0 }
    }
    return predicates > 0
}
# Allowlist: `gh pr|issue create|edit|comment` publishes text, it never writes a repository file. Only a title or
# body given as a literal word is admitted; a file-valued option (--body-file, -F), an expansion, an escape or
# a glob refuses, and every other option value must be plain text.
function gh_publish_allowed(    i, w) {
    for (i = 4; i <= argc; i++) {
        w = W[i]
        if (globbed[i]) return 0
        if (w ~ /^(--title|-t|--body|-b)$/) {
            if (++i > argc || globbed[i] || W[i] ~ /[$`\\]/) return 0
            continue
        }
        if (w ~ /^(--base|-B|--head|-H|--label|-l|--assignee|-a|--repo|-R|--milestone|--reviewer|-r|--project|-p)$/) {
            if (++i > argc || W[i] !~ /^[A-Za-z0-9_.,:@%+\/-]+$/ || W[i] ~ /^-/ || W[i] ~ /(^|\/)\./) return 0
            continue
        }
        if (w ~ /^(--draft|--fill)$/ || w ~ /^[0-9]+$/) continue
        return 0
    }
    return 1
}
function gh_allowed(    i, w, subcommand, read) {
    subcommand = W[2] " " W[3]
    if (subcommand ~ /^(pr|issue) (create|edit|comment)$/) return gh_publish_allowed()
    read = subcommand ~ /^(repo (view|list)|issue (view|list|status)|pr (view|list|status|diff|checks)|release (view|list)|run (view|list|watch)|search (issues|prs|repos|commits|code))$/
    if (W[2] != "api" && !read) return 0
    for (i = W[2] == "api" ? 3 : 4; i <= argc; i++) {
        w = W[i]
        if (globbed[i]) return 0
        if (w ~ /^(--jq|-q|--json|-t|--template|--repo|-R|--limit|-L|--hostname|--header|-H)$/) {
            if (++i > argc || globbed[i]) return 0
            continue
        }
        if (W[2] == "api" && w ~ /^(--method|-X)$/) { if (++i > argc || W[i] !~ /^(GET|HEAD)$/) return 0; continue }
        if (w ~ /^-/ && w !~ /^(--paginate|--slurp|--include|--silent|--log|--log-failed|--web)$/) return 0
    }
    return 1
}
# The two managed corpus scripts are admitted for execution only by exact literal spelling: the path is
# compared as a whole word (no ./, .., variable, glob or quoting variant can equal it), every other word must
# be one of the script's own documented options, and nothing is admitted after a directory change, which
# would select a different checkout's copy. Redirects stay classified by the caller.
function managed_script(    i) {
    if (changed_cwd) return 0
    if (W[1] == "node" && W[2] == ".cratis/ai/hooks/scripts/cratis-check-pr.mjs") {
        for (i = 3; i <= argc; i++) {
            if (W[i] == "--strict") continue
            if (W[i] !~ /^--(body-file|label|add-label|remove-label|pr|base|repo)$/) return 0
            if (++i > argc || W[i] == "" || W[i] ~ /^-/ || globbed[i]) return 0
        }
        return 1
    }
    if (W[1] == "bash" && W[2] == ".cratis/ai/hooks/scripts/cratis-cleanup-worktrees.sh") {
        for (i = 3; i <= argc; i++) if (W[i] !~ /^--(dry-run|quiet|no-fetch)$/) return 0
        return 1
    }
    return 0
}
function list_pattern_allowed(verb, first,    i) {
    if (W[first] != "--list" && !(verb == "git tag" && W[first] == "-l")) return 0
    for (i = first + 1; i <= argc; i++) if (W[i] == "" || W[i] ~ /^-/ || globbed[i]) return 0
    return 1
}
# Query literal paths only, never command text. Unknown destinations are directory candidates too.
# Looking up the nearest existing ancestor also handles missing directories and directory symlinks.
function copy_path_kind(w,    script, cmd, kind) {
    if (w in PATH_KIND) return PATH_KIND[w]
    script = "cd -- \"$1\" || exit 3; p=$2; case $p in /*) ;; *) p=$PWD/$p;; esac; " \
        "if [ -f \"$p\" ]; then echo file; exit; fi; " \
        "while [ ! -d \"$p\" ]; do p=${p%/*}; [ -n \"$p\" ] || p=/; done; " \
        "if git -C \"$p\" rev-parse --show-toplevel >/dev/null 2>&1; then echo inside; else echo outside; fi"
    cmd = "bash -c " shell_quote(script) " guard-copy " shell_quote(ENVIRON["GSW_CWD"]) " " shell_quote(w) " 2>/dev/null"
    if ((cmd | getline kind) != 1) kind = ""
    if (close(cmd) != 0 || kind !~ /^(file|inside|outside)$/) return ""
    PATH_KIND[w] = kind
    return kind
}
function copy_write_target(w, implicit) {
    # Keep exact decoded words (including spaces and quotes), as well as the normal suffix views.
    # A later parser failure still emits the whole command; these extra destinations only narrow it.
    if (ENVIRON["GSW_MODE"] == "targets") { candidate(w); emit_text(w) }
    return destination_allowed(w, implicit)
}
# External copies still write environment files. Expand source globs as literal metadata, judging
# each effective basename; if there are no matches, retain the conservative glob-path classifier.
function copy_external_glob(dest, source, base,    script, cmd, line, count, ok, valid, path) {
    if (ENVIRON["GSW_GLOB_SAFE"] != "1") return 0
    script = glob_enumeration_script()
    cmd = "bash -c " shell_quote(script) " guard-copy-glob " shell_quote(ENVIRON["GSW_CWD"]) " " shell_quote(source) " 2>/dev/null"
    valid = 1
    while ((cmd | getline line) > 0) {
        if (line == "OK") { ok = 1; continue }
        if (ok || substr(line, 1, 2) != "F:" || ++count > 200) { valid = 0; break }
        path = substr(line, 3); sub(/^.*\//, "", path)
        if (path == "" || !copy_write_target(dest "/" path, 1)) { valid = 0; break }
    }
    if (close(cmd) != 0 || !ok || !valid) return 0
    return count ? 1 : copy_write_target(dest "/" base, 1)
}
# Judge both explicit destinations and the basenames utilities append to directory destinations.
# rsync directory contents may introduce protected names; do not infer a safe listing inside a repo.
function copy_effective_targets(verb, targets, sources, dirs, no_target, explicit_dirs,    i, j, dest, kind, base) {
    for (i = 1; i <= targets; i++) {
        dest = destinations[i]
        if (!copy_write_target(dest, 0)) return 0
        if (dirs) continue
        kind = copy_path_kind(dest)
        if (kind == "") return 0
        if (no_target || (kind == "file" && !explicit_dirs && sources == 1 && dest !~ /\/$/)) continue
        for (j = 1; j <= sources; j++) {
            if (verb == "rsync" && copy_sources[j] ~ /\/$/ && kind == "inside") {
                if (!copy_write_target(dest "/.*", 1)) return 0
            }
            base = copy_sources[j]; sub(/\/+$/, "", base); sub(/^.*\//, "", base)
            if (base == "") return 0
            # Keep possible dotfiles separate from dependency-manifest exemptions. External glob
            # matches are classified too: environment filenames are protected outside repositories.
            if (base ~ /[*?\[]/) {
                if (kind == "outside") {
                    if (!copy_external_glob(dest, copy_sources[j], base)) return 0
                    continue
                }
                if (base ~ /^[*?\[]/ && !copy_write_target(dest "/.*", 1)) return 0
            }
            if (!copy_write_target(dest "/" base, 1)) return 0
        }
    }
    return 1
}
# Options precede operands so BSD and GNU agree about the destination. Source paths themselves
# are reads; only explicit and effective destinations are collected for the write classifier.
function copy_targets(verb,    i, w, j, ch, tail, value, targets, dirs, no_target, operands, last, flags, longs, explicit_dirs) {
    if (changed_cwd || input_redirect) return 0
    flags = verb == "cp" ? "rRpafvniuTLPH" : (verb == "rsync" ? "arvnuztp" : "cvCp")
    longs = verb == "cp" ? " --recursive --force --verbose --no-clobber --archive --no-target-directory " : \
        (verb == "rsync" ? " --archive --recursive --verbose --dry-run --update --compress --times --perms " : " ")
    delete destinations; delete copy_sources
    for (i = 2; i <= argc; i++) {
        w = W[i]
        if (w == "--") { i++; break }
        if (w == "-" || w !~ /^-/) break
        if (globbed[i]) return 0
        if (verb != "rsync" && w ~ /^--target-directory(=|$)/) {
            value = substr(w, 20)
            if (w == "--target-directory") { if (++i > argc) return 0; value = W[i] }
            if (value == "" || value ~ /^-/ || globbed[i]) return 0
            destinations[++targets] = value; continue
        }
        if (w ~ /^--/) {
            if (index(longs, " " w " ") == 0) return 0
            if (w == "--no-target-directory") no_target = 1
            continue
        }
        for (j = 2; j <= length(w); j++) {
            ch = substr(w, j, 1)
            if (verb != "rsync" && (ch == "t" || (verb == "install" && ch == "m"))) {
                tail = substr(w, j + 1); value = tail
                if (tail == "") { if (++i > argc) return 0; value = W[i] }
                if (value == "" || value ~ /^-/ || globbed[i]) return 0
                if (ch == "m") { if (value !~ /^[0-7][0-7][0-7][0-7]?$/) return 0 }
                else destinations[++targets] = value
                break
            }
            if (verb == "install" && ch == "d") { dirs = 1; continue }
            if (index(flags, ch) == 0) return 0
            if (ch == "T") no_target = 1
        }
    }
    for (; i <= argc; i++) {
        if (W[i] == "" || W[i] ~ /^-/) return 0
        operands++; last = W[i]; copy_sources[operands] = W[i]
        if (dirs) destinations[operands] = W[i]
    }
    if ((targets && (dirs || no_target)) || !operands) return 0
    explicit_dirs = targets > 0
    if (dirs) targets = operands
    else if (!targets) {
        if (operands < 2 || (verb == "rsync" && index(last, ":"))) return 0
        destinations[++targets] = last; operands--
    }
    return copy_effective_targets(verb, targets, operands, dirs, no_target, explicit_dirs)
}
function allow_command(    verb, first, i) {
    if (!argc) return
    if (++commands > 64) { fail(); return }
    for (i = 1; i <= argc; i++) if (leading[i] && !glob_safe()) { cmd_failed = 1; return }
    if (managed_script()) return
    verb = W[1]; first = 2
    if (verb == "cd") {
        directory_only = 1
        if (argc != 2 || W[2] == "" || W[2] ~ /^-/ || globbed[2] || file_redirect) fail()
        changed_cwd = 1
        return
    }
    if (verb ~ /^(cp|rsync|install)$/) { if (!copy_targets(verb)) cmd_failed = 1; return }
    if (verb == "sed") { if (!print_only_sed()) cmd_failed = 1; return }
    if (verb == "find") { if (!find_allowed()) cmd_failed = 1; return }
    if (verb == "gh") { if (!gh_allowed()) cmd_failed = 1; return }
    if (verb == "git" && W[2] == "-C") {
        if (argc >= 4 && W[3] != "" && W[3] !~ /^-/ && !globbed[3] && W[4] ~ /^(add|commit)$/) {
            verb = verb " " W[4]; first = 5
        } else {
            if (!directory_metadata()) cmd_failed = 1
            return
        }
    } else if (verb == "git") { verb = verb " " W[2]; first = 3 }
    if (verb == "git add" || verb == "git commit") changed_index = 1
    if (verb == "git branch") {
        conditional_only = 1
        if (!(argc == 3 && W[3] == "--show-current") && !list_pattern_allowed(verb, first)) cmd_failed = 1
        return
    }
    if (verb == "git tag") { conditional_only = 1; if (!list_pattern_allowed(verb, first)) cmd_failed = 1; return }
    if (verb == "awk") { if (!awk_allowed()) cmd_failed = 1; return }
    if (!(verb in verbs) || !options_allowed(verb, first)) cmd_failed = 1
}
# `upto` is the index just past the command's raw text; `resume` is where the next command's raw text starts.
# A command whose policy accepts it, with no redirect and no directory change, is a pure reader and its words
# are not write targets; every other command keeps all of its raw text classified.
function command_end(separator, upto, resume,    silent) {
    endword(); cmd_failed = 0; allow_command()
    if (cmd_failed) { bad = 1; unparsed = 1 }
    if (W[1] == "cd" && (separator == "|" || previous_pipe)) fail()
    silent = argc && !cmd_failed && !input_redirect && !force_emit && W[1] != "cd"
    if (silent && redirect_words != "") emitted[++emitted_count] = redirect_words
    if (!silent && upto > seg_start) emitted[++emitted_count] = substr(lexed, seg_start, upto - seg_start)
    seg_start = resume
    previous_pipe = (separator == "|")
    cmd_redirect = force_emit = input_redirect = 0; redirect_words = ""
    delete W; delete globbed; delete leading; argc = 0
}
function readonly(s,    i, c, c2, sq, dq) {
    lexed = s; seg_start = 1
    for (i = 1; i <= length(s); i++) {
        c = substr(s, i, 1); c2 = substr(s, i + 1, 1)
        if (sq) { if (c == "\047") sq = 0; else word = word c; continue }
        if (c == "\\") { if (c2 != "\n") { word = word c2; inword = 1 } else unparsed = 1; i++; continue }
        if (c == "\047" && !dq) { sq = inword = 1; continue }
        if (c == "\"") { dq = !dq; inword = 1; continue }
        # Expansions and globs can supply option words the per-verb tables never see.
        if (c == "`" || c == "$") fail()
        if (dq) { word = word c; continue }
        # Directory metadata requires a literal -C operand, not a pathname expansion.
        if (c == "*" || c == "?" || c == "[") glob = 1
        # A glob that starts a word, or follows a leading dash, may expand to an option word.
        # Only a word-leading * or ? is deferred to the per-verb rule; one after a leading dash, or a leading
        # bracket class (`[-]*` can name an option), is refused outright.
        if ((c == "*" || c == "?" || c == "[") && (word == "" ? !(c == "[" && c2 ~ /[ \t]/) : substr(word, 1, 1) == "-")) {
            if (c == "[" || word != "") fail(); else lead_glob = 1
        }
        if (c == "#" && !inword) { while (i <= length(s) && substr(s, i, 1) != "\n") i++; c = "\n" }
        if (c == "(" || c == ")" || c == "{" || c == "}") fail()
        if (c == ">" || c == "<" || (c == "&" && c2 == ">")) {
            cmd_redirect = 1
            if (c == "<") input_redirect = 1
            if (inword && word ~ /^[0-9]+$/) { word = ""; inword = 0 } else endword()
            if (redirect) fail()
            if (c == "<" && (c2 == "<" || c2 == "(")) fail()
            redirect = c == ">" || c == "&" || (c == "<" && c2 == ">")
            if (c == ">" && c2 == "&") redirect = 2
            if (c2 ~ /[><&|]/) { i++; if (substr(s, i + 1, 1) == ">") i++ }
            continue
        }
        if (c == "&" && c2 == "&") { command_end("&&", i, i + 2); if (redirect) fail(); i++; continue }
        if (c == "|" && c2 != "|") { has_pipeline = 1; command_end("|", i, i + 1); if (redirect) fail(); continue }
        if (c ~ /[;|&\n]/) { nonconditional_separator = 1; command_end(c, i, i + 1); if (redirect) fail(); continue }
        if (c ~ /[ \t\r]/) { endword(); continue }
        word = word c; inword = 1
    }
    command_end("", length(s) + 1, length(s) + 1)
    if (sq || dq || redirect || (conditional_only && (nonconditional_separator || has_pipeline)) ||
        (directory_only && (nonconditional_separator || (has_pipeline && changed_index)))) fail()
    return !bad
}
BEGIN {
    policy("cat", "nAvetsb", "")
    policy("head", "", ""); policy("tail", "", "")
    policy("wc", "lwcm", ""); policy("ls", "laA1hdRtrF", "")
    policy("rg", "nli", "--files")
    policy("grep", "nirRlLcvwxFEHhoqs", "")
    policy("egrep", "nirRlLcvwxFEHhoqs", ""); policy("fgrep", "nirRlLcvwxFEHhoqs", "")
    policy("echo", "neE", "")
    policy("printf", "", ""); policy("test", "", ""); policy("[", "", "")
    policy("true", "", ""); policy("false", "", ""); policy("pwd", "", "")
    policy("df", "h", "")
    # jq has no file-write or exec primitive (output goes to stdout, debug to stderr), so its filter is a plain
    # operand. No value-taking option is accepted (--arg, -f, -L, --rawfile, --slurpfile, --from-file, --indent).
    policy("jq", "rcesnRSjaMC", "--raw-output --compact-output --slurp --null-input --exit-status --raw-input --sort-keys --join-output --ascii-output --monochrome-output --tab")
    policy("awk", "", "")
    policy("shellcheck", "x", "--external-sources")
    policy("git status", "sb", "--short --porcelain")
    policy("git diff", "p", "--stat --numstat --name-only --name-status --cached --staged --check --no-color --cc")
    policy("git log", "p", "--oneline --stat --name-only --no-color --follow")
    policy("git show", "p", "--stat --name-only --no-color")
    policy("git add", "Aupv", "--all"); policy("git commit", "aq", "")
    policy("git blame", "", ""); policy("git ls-files", "s", "--stage --debug")
    policy("git branch", "", "--show-current")
    policy("git rev-parse", "", ""); policy("git check-ignore", "", ""); policy("git grep", "n", "")
    for (i = 0; i < 16; i++) HEX[substr("0123456789abcdef", i + 1, 1)] = i
    ESC["n"] = 10; ESC["r"] = 13; ESC["t"] = 9; ESC["\\"] = 92; ESC["\047"] = 39; ESC["\""] = 34
    text = ENVIRON["GSW_COMMAND"]
    MATCH_MARKER = "$__CRATIS_MATCH__"
    # Never interpret user-supplied or ANSI-decoded marker text as an internal token. Collisions
    # disable admission rather than assigning special meaning to an existing parameter or literal.
    checking_marker = 1; decoded = unquote(text); checking_marker = 0
    match_rewrite_safe = !index(text, MATCH_MARKER) && !index(decoded, MATCH_MARKER)
    if (ENVIRON["GSW_MODE"] == "references") { emit_text(text); exit 0 }
    if (ENVIRON["GSW_MODE"] == "targets") {
        readonly(text)
        if (unparsed) emit_text(text)
        else for (i = 1; i <= emitted_count; i++) emit_text(emitted[i])
        exit 0
    }
    n = split(ENVIRON["GSW_PROTECTED"], a, "\n"); for (i = 1; i <= n; i++) protected_paths[a[i]] = 1
    exit readonly(text) ? 0 : 1
}
AWK
glob_shell_safe=1
if [[ -n "${BASH_ENV+x}" || -n "${ENV+x}" || -n "${GLOBIGNORE+x}" ]]; then glob_shell_safe=0; fi
candidates_text="$(GSW_COMMAND="$command_text" GSW_CWD="$cwd" GSW_GLOB_SAFE="$glob_shell_safe" GSW_MODE=targets LC_ALL=C awk "$program" </dev/null)" || parser_failed
[ -n "$candidates_text" ] || exit 0
candidates=()
while IFS= read -r candidate; do candidates[${#candidates[@]}]="$candidate"; done <<<"$candidates_text"
classifier="$script_directory/cratis-guard-writes.sh"
[ -f "$classifier" ] || {
    printf '%s\n' 'BLOCKED by cratis-guard-shell-writes: could not check references (write guard is missing).' >&2
    exit 2
}
classifier_input="$(jq -nc --arg cwd "$cwd" --args '{cwd:$cwd,tool_input:{file_paths:$ARGS.positional}}' -- "${candidates[@]}")" || parser_failed
# Tool labels and this hook shell do not establish the execution shell options.
# Keep conservative dotfile intersections for every command, especially mutators.
classifier_error="$(mktemp)" || parser_failed
trap 'rm -f "$classifier_error"' EXIT
references="$("$BASH" "$classifier" --references <<<"$classifier_input" 2>"$classifier_error")" || {
    # A command that is not read-only is refused whatever the classifier could model, so an unmodeled word
    # (for example program text inside an interpreter heredoc) must not hide the refusal behind a parser
    # failure. A read-only command keeps the fail-closed parser failure.
    if ! GSW_COMMAND="$command_text" GSW_MODE=readonly GSW_CWD="$cwd" GSW_GLOB_SAFE="$glob_shell_safe" GSW_PROTECTED="" LC_ALL=C awk "$program" </dev/null; then
        printf '%s\n' 'BLOCKED by cratis-guard-shell-writes: this command is not read-only and its references could not all be modeled; use the edit/write tools, or report the intended change.' >&2
        exit 2
    fi
    cat "$classifier_error" >&2
    printf '%s\n' 'BLOCKED by cratis-guard-shell-writes: could not check references (write guard failed).' >&2
    exit 2
}
[ -n "$references" ] || exit 0
if GSW_COMMAND="$command_text" GSW_MODE=readonly GSW_CWD="$cwd" GSW_GLOB_SAFE="$glob_shell_safe" GSW_PROTECTED="$references" LC_ALL=C awk "$program" </dev/null; then exit 0; fi
printf "BLOCKED by cratis-guard-shell-writes: protected reference '%s' (cratis-guard-writes). Use the edit/write tools, or report the intended change; for a read, name literal paths or use the Read/Grep tools.\n" "${references%%$'\n'*}" >&2
exit 2
