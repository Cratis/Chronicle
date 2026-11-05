#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-guard-secrets.sh
# PreToolUse hook (Bash) — hard block on shell commands that print credentials into the transcript.
#
# secrets.md says a credential must never enter an agent transcript, and lists the command classes that
# print one: full Kubernetes specs and secrets, the process environment, container inspection, secret-manager
# reads and credential files. This guard is what enforces it: every simple command in the shell command is
# found (quotes, $( ), backticks, subshells, pipelines, here-documents, `bash -c`, `eval`, `kubectl exec ... --`
# and `docker exec`, and the wrappers sudo/env/timeout/xargs/rtk are looked through) and classified. A command
# that names a protected file is subject to the conservative literal-command rule below.
# Exit 2 blocks the tool call and sends stderr back to the model; every other command exits 0. While jq is
# missing the guard cannot read the tool call, so it refuses every call.
#
# The guard is conservative: a command naming a protected file runs only as a single literal read or template
# copy, and other such commands must be split into literal calls. Names produced by programs, inherited values,
# aliases, functions, script files and recursive searches without named files remain outside its text analysis.
#
# Escape hatch for a human who authorized the read — set in the environment the agent harness was started
# from, never inside the command (an assignment in the command is ignored):
#   CRATIS_HOOKS_ALLOW_SECRET_READS=1
set -euo pipefail

# SCRIPTDIR, not a path relative to the caller: shellcheck resolves a plain relative `source=`
# against the current working directory, and these hooks are linted from wherever CI happens to run.
# shellcheck source=SCRIPTDIR/hook-lib.sh
. "$(dirname "${BASH_SOURCE[0]}")/hook-lib.sh"

[ "${CRATIS_HOOKS_ALLOW_SECRET_READS:-0}" = "1" ] && exit 0

input="$(hook_read_stdin)"
[ -n "$input" ] || exit 0
hook_require_jq cratis-guard-secrets CRATIS_HOOKS_ALLOW_SECRET_READS

parser_failed() {
    printf '%s\n' 'BLOCKED by cratis-guard-secrets: parser failed; cannot check this tool call.' >&2
    exit 2
}

command_text="$(jq -r '.tool_input.command // empty' <<<"$input")" || parser_failed
[ -n "$command_text" ] || exit 0

# Cheap exit for the overwhelming majority of shell commands, which mention none of these words. The shell
# reads a word after removing quotes, escapes and line continuations, so the words are looked for in that
# form. A command that still shows none is parsed anyway when its raw text has an expansion, glob or brace
# character: this list must stay a superset of the protected stems the parser can resolve.
probe=${command_text//\\$'\n'/}
probe=${probe//[\'\"\\]/}
probe_lower="$(printf '%s' "$probe" | tr '[:upper:]' '[:lower:]')" || parser_failed
case "$probe_lower" in
    *kubectl*|*printenv*|*env*|*docker*|*podman*|*"gh "*|*vault*|*pulumi*|*show-secret*|*aws*|*"az "*|*gcloud*|*kubeconfig*|*.kube*|\
    *sh*|*extglob*|*set*|*emulate*|*'-o'*|*'+o'*|*globignore*|*export*|*declare*|*id_rsa*|*id_ed25519*|*id_ecdsa*|*id_dsa*|*.pem*|*.p12*|*.pfx*|*token*|*.netrc*|*.npmrc*|\
    *credentials*|*.pgpass*|*pypirc*|*secrets*|*oc*) ;;
    *)
        case "$command_text" in
            *'$'*|*'*'*|*'?'*|*'['*|*'{'*|*'('*|*'^'*|*'~'*|*'`'*) ;;
            *) exit 0 ;;
        esac ;;
esac

IFS= read -r -d '' awk_program <<'AWK' || true
function report(what, alt) {
    if (what in seen) return
    seen[what] = 1
    reports = reports sprintf("%s\t%s\n", what, alt)
}
function enqueue(text) {
    if (text == "" || queued >= 64) return
    queue[++queued] = text
}
function base(word) { word = tolower(word); sub(/.*\//, "", word); return word }

# ── tokenizer: splits text into simple commands, queueing nested command text ──
# Appends text to the current word and records how the shell reads each character in the parallel mask wmask:
# l literal (single-quoted, escaped or ANSI-C decoded), d inside double quotes, u unquoted, c command output.
# An uppercase mask character marks the first character after a quote boundary, including an empty segment.
function addw(text, kind,    k) {
    word = word text
    for (k = 1; k <= length(text); k++) { wmask = wmask (quote_boundary ? toupper(kind) : kind); quote_boundary = 0 }
}
function flush_word() {
    if (inword) { W[++nw] = word; WP[nw] = wplain; WM[nw] = wmask }
    word = ""; wmask = ""; inword = 0; wquoted = 0; wplain = 0; quote_boundary = 0
}
# Decodes the body of an ANSI-C string $'...' (i is just after the opening quote) into the word and returns the
# position after the closing quote. The same decoder is in cratis-guard-store-mutations.sh (`ansi_c`).
function ansi_c(s, i, n,    c, c2, v, d, k, lim, stop, digits) {
    stop = 0
    while (i <= n) {
        c = substr(s, i, 1)
        if (c == "\047") return i + 1
        if (c != "\\") { if (!stop) addw(c, "l"); i++; continue }
        c2 = substr(s, i + 1, 1); i += 2; v = -1
        if (c2 in ESC) v = ESC[c2]
        else if (c2 ~ /^[0-7]$/) {
            v = c2 + 0
            for (k = 0; k < 2 && substr(s, i, 1) ~ /^[0-7]$/; k++) { v = v * 8 + substr(s, i, 1); i++ }
            v = v % 256
        } else if (c2 == "x" || c2 == "u" || c2 == "U") {
            lim = (c2 == "x") ? 2 : (c2 == "u") ? 4 : 8; v = 0; digits = 0
            while (digits < lim && (d = tolower(substr(s, i, 1))) != "" && (d in HEX)) { v = v * 16 + HEX[d]; i++; digits++ }
            if (digits == 0) { if (!stop) addw("\\" c2, "l"); continue }
            if (c2 != "x" && v >= 128) { if (!stop) addw("?", "c"); continue }
        } else if (c2 == "c" && i <= n) { v = ORD[substr(s, i, 1)] % 32; i++ }
        else { if (!stop) addw("\\" c2, "l"); continue }
        if (v == 0) stop = 1
        else if (!stop) addw(CH[v], "l")
    }
    return i
}
# Output redirections are removed before a command is analyzed, so a redirect never shifts a CLI's subcommand
# or operands (`kubectl get > /dev/stdout secrets`). Nothing is exempted: the command itself is still analyzed.
# A word is a redirection only when its whole operator lies in the word's unquoted prefix (WP: characters before
# the first quoted or escaped one), so `1">"b` and `">x"` stay operands; input redirections (`< file`) stay too.
function stem_boundary(t, start, finish, stem,    a, b, before, after) {
    if (stem == "/environ" && substr(t, finish + 1, 1) ~ /[a-z0-9_]/) return 0
    if (stem !~ /^(id_rsa|id_ed25519|id_ecdsa|id_dsa|kubeconfig|credentials)$/) return 1
    a = start; b = finish
    while (a > 1 && substr(t, a - 1, 1) ~ /[a-z0-9_]/) a--
    while (b < length(t) && substr(t, b + 1, 1) ~ /[a-z0-9_]/) b++
    before = substr(t, a - 1, 1); after = substr(t, b + 1)
    return !(before == "$" || before == "-" || (before == "{" && substr(t, a - 2, 1) == "$") || after ~ /^(=|\+=)/)
}
function glob_end(t, p,    j, depth, c) {
    for (j = p; j <= length(t); j++) {
        c = substr(t, j, 1)
        if (c ~ /[ \t\n\047"]/) return 0
        if (c == "(") depth++
        else if (c == ")" && --depth == 0) return j
    }
    return 0
}
# Memoized matching keeps stars and grouped globs bounded by the length of a protected stem.
# Two literal characters are required: a generic pattern such as *.md is not a protected name.
function glob_negation(t, p) { return substr(t, p, 1) ~ /^[~^]$/ && p > 1 && substr(t, p - 1, 1) !~ /[\/=:]/ }
function stem_match(t, stem, p, k, literals, wildcard,    key, c, end, nextp, j, result, bracket) {
    key = p SUBSEP k SUBSEP literals SUBSEP wildcard
    if (key in SM) return SM[key]
    if (k > length(stem)) return SM[key] = (wildcard && literals >= 2 ? p - 1 : 0)
    c = substr(t, p, 1)
    if (c == "" || c ~ /[\001-\007]/) return SM[key] = 0
    nextp = 0
    if (c ~ /[?*+@!]/ && substr(t, p + 1, 1) == "(") nextp = glob_end(t, p + 1) + 1
    else if (c == "(" && (end = glob_end(t, p))) nextp = end + 1
    if (nextp > p || c == "*" || glob_negation(t, p)) {
        if (!nextp) nextp = p + 1
        for (j = k; j <= length(stem) + 1; j++) {
            result = stem_match(t, stem, nextp, j, literals, 1)
            if (result) return SM[key] = result
        }
    } else if (c == "?") return SM[key] = stem_match(t, stem, p + 1, k + 1, literals, 1)
    else if (c == "[" && (end = bracket_end(t, p)) > 0) {
        bracket = substr(t, p + 1, end - p - 1)
        if (bracket !~ /[ \t\n\047"]/ && substr(stem, k, 1) ~ rxize("\006" bracket "\007"))
            return SM[key] = stem_match(t, stem, end + 1, k + 1, literals, 1)
    } else if (c == substr(stem, k, 1)) return SM[key] = stem_match(t, stem, p + 1, k + 1, literals < 2 ? literals + 1 : 2, wildcard)
    return SM[key] = 0
}
function stem_units(t, m, start, stem) {
    delete SM
    return stem_match(t, stem, start, 1, 0, 0)
}
function stem_scan(text, mode,    t, k, stem, pos, offset, first, found, end, p, candidate) {
    t = tolower(text)
    for (k = 1; k <= nstem; k++) {
        stem = STEM[k]; offset = 0
        while ((pos = index(substr(t, offset + 1), stem)) > 0) {
            pos += offset; end = pos + length(stem) - 1
            if (stem_boundary(t, pos, end, stem) && (!first || pos < first)) { first = pos; found = stem; break }
            offset = pos
        }
        if (stem == ".kube/") {
            pos = index(t, ".kube")
            if (pos && (pos + 5 > length(t) || substr(t, pos + 5, 1) ~ /^[ \t\n;|&]$/) && (!first || pos < first)) { first = pos; found = stem }
        }
    }
    if (mode != "resolved" && t ~ /[*?[(~^]/) {
        for (p = 1; p <= length(t) && (!first || p < first); p++) {
            candidate = substr(t, p, 1)
            for (k = 1; k <= nstem; k++) {
                stem = STEM[k]
                if (candidate != substr(stem, 1, 1) && candidate !~ /[*?[(@+!~^]/) continue
                end = stem_units(t, mode, p, stem)
                if (end && stem_boundary(t, p, end, stem)) { first = p; found = stem; break }
            }
        }
    }
    return found
}
function literal_word(w, m,    k, c, mk) {
    for (k = 1; k <= length(w); k++) {
        c = substr(w, k, 1); mk = tolower(substr(m, k, 1))
        if (mk == "c" || (c == "$" && mk != "l") || (mk == "u" && (c ~ /[*?[()]/ || glob_negation(w, k)))) return 0
    }
    return !(substr(w, 1, 1) == "~" && substr(w, 2, 1) != "/")
}
# RP records only exempt regex text. Other option values are skipped while locating the pattern, not while scanning stems.
# With any -e or -f there is no implicit pattern operand. Attached patterns exempt only their suffix.
function regex_flag(w) {
    return w ~ /^--(ignore-case|no-ignore-case|word-regexp|line-regexp|fixed-strings|basic-regexp|extended-regexp|perl-regexp|invert-match|line-number|no-line-number|count|files-with-matches|files-without-match|only-matching|quiet|silent|text|binary|no-messages|with-filename|no-filename|heading|no-heading|no-ignore|hidden|no-hidden|recursive|help|version|color|colour)$/
}
function regex_value(w) {
    return w ~ /^--(file|glob|iglob|include|exclude|exclude-dir|type|type-not|type-add|type-clear|max-count|max-depth|maxdepth|max-filesize|encoding|engine|pre|pre-glob|sort|sortr|threads|replace|label|colors|after-context|before-context|context|ignore-file|path-separator|hostname-bin)$/
}
function regex_short_flag(b, c) {
    return c ~ /^[EFGPivwxcnNlLoqsaHIhVzuUSyZ0]$/ || (c ~ /^[rRT]$/ && b != "rg")
}
function regex_slots(A, s, n,    b, k, j, w, c, explicit, ended, attached, uncertain) {
    delete RP; delete ROPER
    b = base(A[s])
    if (b !~ /^(grep|egrep|fgrep|rg|ag)$/) return
    for (k = s + 1; k <= n; k++) {
        w = A[k]
        if (!ended && w == "--") { ended = 1; continue }
        if (!ended && w ~ /^--/) {
            if (w ~ /^--(regexp|file)(=|$)/) explicit = 1
            if (w ~ /^--regexp=/) RP[k] = index(w, "=")
            else if (w == "--regexp") RP[++k] = -1
            else if (w !~ /=/ && regex_value(w)) k++
            continue
        }
        if (!ended && w ~ /^-./) {
            for (j = 2; j <= length(w); j++) {
                c = substr(w, j, 1)
                if (c == "e" || c == "f") explicit = 1
                if (!regex_short_flag(b, c)) {
                    if (!index("efgGtTmABCjdrD", c)) uncertain = 1
                    attached = (j < length(w))
                    if (c == "e") { if (attached) RP[k] = j; else RP[++k] = -1 }
                    else if (!attached) k++
                    break
                }
            }
            continue
        }
        ROPER[k] = 1
    }
    if (!explicit && !uncertain) for (k = s + 1; k <= n; k++) if (k in ROPER) { RP[k] = -1; delete ROPER[k]; break }
}
function shell_quote(w,    k, c, out) {
    out = "\047"
    for (k = 1; k <= length(w); k++) {
        c = substr(w, k, 1)
        out = out (c == "\047" ? "\047\\\047\047" : c)
    }
    return out "\047"
}
function directory_operand(w,    path) {
    if (w ~ /\/$/ || w == "." || w == "..") return 1
    path = w
    if (path ~ /^~\//) path = ENVIRON["HOME"] substr(path, 2)
    if (path !~ /^\//) path = ENVIRON["GS_CWD"] "/" path
    return system("test -d " shell_quote(path)) == 0
}
function metadata_shape(A, M, n,    k, j, c, mk) {
    if (A[1] != "ls" && A[1] != "stat") return 0
    for (k = 1; k <= n; k++) for (j = 1; j <= length(A[k]); j++) {
        c = substr(A[k], j, 1); mk = tolower(substr(M[k], j, 1))
        if (mk == "c" || (c == "$" && mk != "l")) return 0
    }
    return 1
}
function literal_shape(A, M, n,    k, j, b, words, count, pattern, ended, w, paths, flags) {
    if (metadata_shape(A, M, n)) return 1
    delete words
    for (k = 1; k <= n; k++) {
        if (is_assign(A[k]) || !literal_word(A[k], M[k])) return 0
        resolve(A[k], M[k])
        for (j = 1; j <= nalt; j++) {
            if (has_sentinel(ALT[j])) return 0
            words[++count] = ALT[j]
        }
    }
    b = words[1]
    if (b ~ /\// || b !~ /^(cat|head|tail|less|more|wc|grep|egrep|fgrep|rg|ls|stat|test|cp)$/) return 0
    if (b ~ /^(ls|stat|test)$/) return 1
    if (b == "cp") return count == 3 && words[2] !~ /^-/ && words[3] !~ /^-/ && sensitive_path(words[2]) == ""
    pattern = (b ~ /^(grep|egrep|fgrep|rg)$/)
    regex_slots(words, 1, count)
    for (k = 2; k <= count; k++) {
        w = words[k]
        if (k in RP) { if (RP[k] < 0) continue; w = substr(w, 1, RP[k]) }
        if (!ended && w == "--") { ended = 1; continue }
        if (!ended && w ~ /^-./) {
            if (pattern) {
                if (w == "-e" || w == "--regexp" || w == "--regexp=") continue
                if (w ~ /^--/) {
                    if (w == "--recursive" || !regex_flag(w)) return 0
                } else {
                    flags = substr(w, 2)
                    if (flags ~ /[rR]/) return 0
                    for (j = 1; j <= length(flags); j++) if (!regex_short_flag(b, substr(flags, j, 1)) && !(substr(flags, j, 1) == "e" && (k in RP))) return 0
                }
            }
            if (stem_scan(w, "raw") != "") return 0
            continue
        }
        if (sensitive_path(w) != "") return 0
        if (pattern && (k in ROPER)) {
            paths++
            if (b == "rg" && directory_operand(w)) return 0
        }
    }
    return b != "rg" || paths > 0
}
function end_cmd(    k, m, w, j, hit) {
    flush_word()
    if (nw > 0) {
        commands++
        regex_slots(W, 1, nw)
        for (k = 1; k <= nw; k++) {
            if (is_assign(W[k])) set_var(W[k], WM[k])
            if ((k in RP) && RP[k] < 0) continue
            w = (k in RP) ? substr(W[k], 1, RP[k]) : W[k]
            hit = stem_scan(w, "raw"); if (TAINT == "") TAINT = hit
            if (!(k in RP) && expands(W[k], WM[k])) {
                resolve(W[k], WM[k])
                for (j = 1; j <= nalt; j++) { hit = stem_scan(ALT[j], "resolved"); if (TAINT == "") TAINT = hit }
            }
        }
        if (!literal_shape(W, WM, nw)) simple_ok = 0
    }
    m = 0
    for (k = 1; k <= nw; k++) {
        w = W[k]
        if (match(w, /^([0-9]*|&)>[>|]?/) && RLENGTH <= WP[k]) {
            if (RLENGTH == length(w)) k++
            continue
        }
        V[++m] = w; VM[m] = WM[k]
    }
    if (m > 0) { check_inputs(V, 1, m); analyze(V, 1, m) }
    delete W; delete WP; delete WM; delete V; delete VM; nw = 0
}
function cmdsub(s, i, n,    start, depth, c, j) {
    complex = 1
    start = i; depth = 1
    while (i <= n) {
        c = substr(s, i, 1)
        if (c == "\\") { i += 2; continue }
        if (c == "\047") { j = index(substr(s, i + 1), "\047"); if (j == 0) { i = n + 1; break } i += j + 1; continue }
        if (c == "\"") {
            i++
            while (i <= n && substr(s, i, 1) != "\"") { if (substr(s, i, 1) == "\\") i++; i++ }
            i++; continue
        }
        if (c == "(") depth++
        else if (c == ")") { depth--; if (depth == 0) break }
        i++
    }
    enqueue(substr(s, start, i - start))
    return i + 1
}
function read_heredocs(s, i, n,    h, e, line, cmp, body) {
    for (h = 1; h <= nheredoc; h++) {
        body = ""
        while (i <= n) {
            e = index(substr(s, i), "\n")
            if (e == 0) { line = substr(s, i); i = n + 1 } else { line = substr(s, i, e - 1); i += e }
            cmp = line
            if (HDDASH[h]) sub(/^\t+/, "", cmp)
            if (cmp == HD[h]) break
            body = body line "\n"
        }
        if (hd_shell) enqueue(body)
    }
    nheredoc = 0; hd_shell = 0
    return i
}
function tokenize(s,    i, n, c, c2, j, dash, delim) {
    n = length(s); i = 1; nw = 0; word = ""; wmask = ""; inword = 0; wquoted = 0; wplain = 0; nheredoc = 0; hd_shell = 0; quote_boundary = 0
    delete W; delete WP; delete WM
    while (i <= n) {
        c = substr(s, i, 1)
        if (c == "\\") {
            c2 = substr(s, i + 1, 1)
            if (c2 == "\n") { i += 2; continue }
            wquoted = 1; addw(c2, "l"); inword = 1; i += 2; continue
        }
        if (c == "\047") {
            wquoted = 1; inword = 1; quote_boundary = 1
            j = index(substr(s, i + 1), "\047")
            if (j == 0) { addw(substr(s, i + 1), "l"); i = n + 1; continue }
            addw(substr(s, i + 1, j - 1), "l"); quote_boundary = 1; i += j + 1; continue
        }
        if (c == "$" && substr(s, i + 1, 1) == "\047") { wquoted = 1; inword = 1; quote_boundary = 1; i = ansi_c(s, i + 2, n); quote_boundary = 1; continue }
        if (c == "$" && substr(s, i + 1, 1) == "\"") { i++; c = "\"" }
        if (c == "\"") {
            wquoted = 1; inword = 1; quote_boundary = 1; i++
            while (i <= n) {
                c = substr(s, i, 1)
                if (c == "\\") { addw(substr(s, i + 1, 1), "l"); i += 2; continue }
                if (c == "\"") { quote_boundary = 1; i++; break }
                if (c == "$" && substr(s, i + 1, 1) == "(") { i = cmdsub(s, i + 2, n); addw("x", "c"); continue }
                if (c == "`") {
                    complex = 1
                    j = index(substr(s, i + 1), "`")
                    if (j == 0) { i = n + 1; break }
                    enqueue(substr(s, i + 1, j - 1)); i += j + 1; addw("x", "c"); continue
                }
                addw(c, "d"); i++
            }
            continue
        }
        if (c == "$" && substr(s, i + 1, 1) == "(") { wquoted = 1; inword = 1; i = cmdsub(s, i + 2, n); addw("x", "c"); continue }
        if (c == "`") {
            complex = 1
            wquoted = 1; inword = 1
            j = index(substr(s, i + 1), "`")
            if (j == 0) { i = n + 1; continue }
            enqueue(substr(s, i + 1, j - 1)); i += j + 1; addw("x", "c"); continue
        }
        if (c == "#" && !inword) {
            j = index(substr(s, i), "\n")
            if (j == 0) { i = n + 1 } else { i += j - 1 }
            continue
        }
        if (c == " " || c == "\t" || c == "\r") { flush_word(); i++; continue }
        if (c == "\n") {
            end_cmd(); i++
            if (nheredoc > 0) i = read_heredocs(s, i, n)
            continue
        }
        # `>&`, `>|` and `&>` are parts of a redirection operator, not command separators.
        if ((c == "&" || c == "|") && inword && !wquoted && word ~ />$/) { addw(c, "u"); wplain++; i++; continue }
        if (c == "&" && substr(s, i + 1, 1) == ">") { flush_word(); addw(c, "u"); inword = 1; wplain = 1; i++; continue }
        if (c == "(" && (j = glob_end(s, i)) > 0 && (inword || nw > 0 || substr(s, j + 1, 1) ~ /^[A-Za-z0-9_.]$/)) {
            inword = 1; addw(substr(s, i, j - i + 1), "u"); i = j + 1; continue
        }
        if (c == ";" || c == "&" || c == "|" || c == "(" || c == ")") {
            end_cmd()
            complex = 1
            i++; continue
        }
        if (substr(s, i, 2) == "<<" && substr(s, i, 3) != "<<<") {
            complex = 1
            flush_word(); i += 2; dash = 0
            if (substr(s, i, 1) == "-") { dash = 1; i++ }
            while (substr(s, i, 1) == " " || substr(s, i, 1) == "\t") i++
            delim = ""
            while (i <= n) {
                c = substr(s, i, 1)
                if (c ~ /[ \t\n;&|()<>]/) break
                if (c != "\047" && c != "\"" && c != "\\") delim = delim c
                i++
            }
            nheredoc++; HD[nheredoc] = delim; HDDASH[nheredoc] = dash
            continue
        }
        if (c == "<") {
            complex = 1
            if (word !~ /^[0-9]+$/ || wquoted) flush_word()
            addw(c, "u"); inword = 1; wplain++; i++
            while (i <= n && substr(s, i, 1) ~ /[<>&]/) { addw(substr(s, i, 1), "u"); wplain++; i++ }
            flush_word(); continue
        }
        if (c == ">") complex = 1
        if (!wquoted) wplain++
        addw(c, "u"); inword = 1; i++
    }
    end_cmd()
}

# ── option parsing shared by the CLIs: positionals P/PI, output format, -f/--format value, --raw ──
function kparse(A, s, n, shortvals,    i, w, eq, key, val, f, rest) {
    delete P; delete PI
    np = 0; outfmt = ""; fmtval = ""; tmpl = ""; tmplfile = 0; dd = 0; raw = 0
    for (i = s; i <= n; i++) {
        w = A[i]
        if (w == "--") { dd = i; break }
        if (substr(w, 1, 2) == "--") {
            eq = index(w, "="); key = w; val = ""
            if (eq) { key = substr(w, 1, eq - 1); val = substr(w, eq + 1) }
            if (key == "--output") { if (!eq) { i++; val = A[i] } outfmt = tolower(val) }
            else if (key == "--format") { if (!eq) { i++; val = A[i] } fmtval = val }
            else if (key == "--template") {
                if (!eq) { i++; val = A[i] } tmpl = val
                if (val ~ /^[[:space:]]*@/ || val !~ /[{]/) tmplfile = 1
            }
            else if (key == "--template-file") { if (!eq) i++; tmplfile = 1 }
            else if (key == "--raw") raw = 1
            else if (!eq && (key in LV)) i++
            continue
        }
        if (substr(w, 1, 1) == "-" && length(w) > 1) {
            f = substr(w, 2, 1); rest = substr(w, 3)
            if (f == "o") {
                if (rest == "") { i++; val = A[i] } else val = (substr(rest, 1, 1) == "=" ? substr(rest, 2) : rest)
                outfmt = tolower(val)
            } else if (f == "f" && index(shortvals, "f")) {
                if (rest == "") { i++; val = A[i] } else val = (substr(rest, 1, 1) == "=" ? substr(rest, 2) : rest)
                fmtval = val
            } else if (rest == "" && f != "" && index(shortvals, f)) i++
            continue
        }
        np++; P[np] = w; PI[np] = i
    }
}
# Projection paths pass only through these explicit field shapes.
function proj_path_allowed(t, inrange) {
    if (!inrange) sub(/^\.items\[(\*|[0-9]+)\]/, "", t)
    if (t ~ /^\.(kind|apiversion)$/) return 1
    if (t ~ /^\.metadata\.(name|namespace|uid|creationtimestamp|resourceversion|generation|deletiontimestamp)$/) return 1
    if (t ~ /^\.metadata\.labels(\.[a-z0-9_-]+)?$/) return 1
    if (t ~ /^\.metadata\.ownerreferences\[(\*|[0-9]+)\]\.(name|kind|uid)$/) return 1
    if (t ~ /^\.status(\.[a-z0-9_-]+|\[(\*|[0-9]+)\])*$/) return 1
    if (t ~ /^\.spec(\.template\.spec)?\.(containers|initcontainers|ephemeralcontainers)\[(\*|[0-9]+)\]\.(name|image|imagepullpolicy)$/) return 1
    return t ~ /^\.spec\.(nodename|replicas|serviceaccountname|schedulername|restartpolicy|type|clusterip)$/
}
function proj_columns_allowed(t,    columns, n, k, p, colon) {
    n = split(tolower(t), columns, ","); if (!n) return 0
    for (k = 1; k <= n; k++) {
        p = columns[k]; gsub(/[[:space:]]/, "", p)
        colon = index(p, ":"); if (colon <= 1) return 0
        p = substr(p, colon + 1)
        if (p !~ /^\./) p = "." p
        if (!proj_path_allowed(p, 0)) return 0
    }
    return 1
}
function proj_jsonpath_allowed(t,    i, c, segment, active, quote, escaped, literal, depth) {
    t = tolower(t)
    for (i = 1; i <= length(t); i++) {
        c = substr(t, i, 1)
        if (!active) {
            if (c == "}") return 0
            if (c == "{") { active = 1; segment = ""; literal = 0 }
            continue
        }
        if (c == "{") return 0
        if (c == "}") {
            if (quote != "") return 0
            if (!literal) {
                if (segment == "range.items[*]") depth++
                else if (segment == "end") { if (!depth) return 0; depth-- }
                else if (!proj_path_allowed(segment, depth > 0)) return 0
            }
            active = 0; continue
        }
        if (quote != "") {
            if (escaped) escaped = 0
            else if (c == "\\") escaped = 1
            else if (c == quote) { quote = ""; literal = 1 }
        } else if (c !~ /[[:space:]]/) {
            if (literal) return 0
            if (c == "\"" || c == SQ) {
                if (segment != "") return 0
                quote = c
            } else segment = segment c
        }
    }
    return !active && !depth
}
function skipopts(A, s, n, vals) {
    while (s <= n && substr(A[s], 1, 1) == "-") {
        if (length(A[s]) == 2 && index(vals, substr(A[s], 2, 1))) s += 2
        else s++
    }
    return s
}
function is_assign(w) { return w ~ /^[A-Za-z_][A-Za-z0-9_]*(\[[^]]*\])?\+?=/ }

# ── file classification for the reader commands ──
function code_file(b) { return b ~ /\.(cs|ts|tsx|js|jsx|mjs|cjs|go|py|java|kt|rs|md)$/ }
# A design-token file is not a credential: a stylesheet, or a file under a design, theme or style directory, is
# fine unless it sits in a secrets or credentials directory. A bare token.json (an OAuth refresh-token file) is not.
function token_ok(l, b) {
    if (l ~ /(^|\/)(secrets?|credentials?)\//) return 0
    if (b ~ /\.(css|scss|less)$/) return 1
    return l ~ /(^|\/)(design|design-tokens|themes?|styles?)\//
}
function sensitive_path(w,    l, b) {
    l = tolower(w); b = l; sub(/.*\//, "", b)
    if (l ~ /(^|\/)\.kube\// || b == "kubeconfig" || b ~ /\.kubeconfig$/) return "a kubeconfig"
    if (b ~ /^\.env/ || b ~ /\.env$/) { if (b ~ /\.(example|sample|template|dist|defaults?)$/) return ""; return "an .env file" }
    if (b ~ /\.(pem|p12|pfx)$/) return "a key or certificate file"
    if (b == "id_rsa" || b == "id_ed25519" || b == "id_ecdsa" || b == "id_dsa") return "a private key"
    if (b ~ /(^|[._-])tokens?($|[._-])/ && !code_file(b) && !token_ok(l, b)) return "a token file"
    if (b == ".netrc" || b == ".npmrc" || b == ".pgpass" || b == ".pypirc" || b == ".git-credentials") return "a credentials file"
    if (b == "credentials") return "a credentials file"
    if (l ~ /\.docker\/config\.json$/) return "the Docker registry credentials"
    if (l ~ /\/proc\/[^\/]*\/environ$/) return "a process environment"
    if (l ~ /^\/(var\/)?run\/secrets\//) return "mounted secrets"
    return ""
}

# ── word resolution: what the shell reads a word as ──
# resolve() fills ALT[1..nalt]. \001 is an unresolved expansion; \003 is an external runtime value.
# \004 and \005 are pathname glob stars and single-character patterns, subject to default shell dot rules.
# Bindings anywhere in the command contribute alternatives. An incomplete expansion or a reached cap is never
# discarded: it becomes unresolved and is checked against protected names before a read is allowed.
function mask_of(A, k,    m) { m = VM[k]; return (length(m) == length(A[k])) ? m : "" }
function expands(w, m,    k, c, mk) {
    if (m == "") return 0
    for (k = length(w); k > 0; k--) {
        c = substr(w, k, 1); mk = tolower(substr(m, k, 1))
        if (mk == "c" || (c == "$" && mk != "l") || (mk == "u" && (c ~ /[*?[{()]/ || glob_negation(w, k)))) return 1
    }
    return 0
}
function find_group(    i, j, depth, c) {
    for (i = 1; i <= length(gw); i++) {
        if (tolower(substr(gm, i, 1)) != "u" || substr(gw, i, 1) != "{" || (i > 1 && substr(gw, i - 1, 1) == "$")) continue
        depth = 0; gnc = 0
        for (j = i; j <= length(gw); j++) {
            if (tolower(substr(gm, j, 1)) != "u") continue
            c = substr(gw, j, 1)
            if (c == "{") depth++
            else if (c == "}") { depth--; if (depth == 0) break }
            else if (c == "," && depth == 1) GCP[++gnc] = j
        }
        if (j > length(gw)) continue
        gs = i; ge = j
        if (gnc > 0) return 1
        if (index(substr(gw, i, j - i), "..")) return 2
    }
    return 0
}
function brace_all(w, m,    head, tail, r, k, e, a, pre, prem, suf, sufm) {
    nf = 0; delete FW; delete FM; delete QW; delete QM
    head = 1; tail = 1; QW[1] = w; QM[1] = m
    while (head <= tail) {
        gw = QW[head]; gm = QM[head]; head++
        r = find_group()
        if (r == 0) { FW[++nf] = gw; FM[nf] = gm; continue }
        pre = substr(gw, 1, gs - 1); prem = substr(gm, 1, gs - 1)
        suf = substr(gw, ge + 1); sufm = substr(gm, ge + 1)
        if (r == 2 || (tail - head + 1) + nf + gnc + 1 > 64) { overflow = 1; return }
        a = gs + 1
        for (k = 1; k <= gnc + 1; k++) {
            e = (k <= gnc) ? GCP[k] : ge
            tail++; QW[tail] = pre substr(gw, a, e - a) suf; QM[tail] = prem substr(gm, a, e - a) sufm; a = e + 1
        }
    }
}
# Parses the parameter expansion that starts at w[i] and fills PC[1..npc] with what it can be; returns the index of
# its last character. ${NAME:-w} ${NAME-w} ${NAME:=w} ${NAME=w} add w, ${NAME:+w} ${NAME+w} give w or nothing.
function variable_values(name,    k, j, count, result, values, masks, value_parts) {
    if (UNK[name] || ACTIVE[name] || value_depth >= 4) return "\001"
    ACTIVE[name] = 1; value_depth++
    result = "\003"; count = BVN[name]
    delete values; delete masks
    for (k = 1; k <= count; k++) { values[k] = BV[name, k]; masks[k] = BVM[name, k] }
    for (k = 1; k <= count; k++) {
        resolve(values[k], masks[k])
        for (j = 1; j <= nalt; j++) result = result "\034" ALT[j]
        delete value_parts
        if (split(result, value_parts, "\034") > 64) { result = "\001"; break }
    }
    value_depth--; delete ACTIVE[name]
    return result
}
function param(w, m, i,    c, j, name, inner, op, base, rest, depth, bad, special, kind) {
    delete PC; npc = 1; PC[1] = "$"
    c = substr(w, i + 1, 1); name = ""; op = ""; rest = ""
    if (c == "{") {
        depth = 1
        for (j = i + 2; j <= length(w); j++) {
            c = substr(w, j, 1)
            if (c == "{") depth++
            else if (c == "}" && --depth == 0) break
        }
        if (depth != 0) { PC[1] = "\001"; return length(w) }
        inner = substr(w, i + 2, j - i - 2); i = j
        if (match(inner, /^[A-Za-z_][A-Za-z0-9_]*/)) {
            name = substr(inner, 1, RLENGTH); rest = substr(inner, RLENGTH + 1)
            if (rest != "") {
                if (match(rest, /^:?[-=+]/)) { op = substr(rest, RLENGTH, 1); rest = substr(rest, RLENGTH + 1) } else bad = 1
            }
        } else if (inner ~ /^([0-9]+|[@*])$/) special = "\001"
        else if (inner ~ /^[?$!#-]$/) special = "\003"
        else bad = 1
        if (rest ~ /[$`{}]/) bad = 1
    } else if (c ~ /^[A-Za-z_]$/) {
        kind = tolower(substr(m, i, 1))
        for (j = i + 1; j <= length(w) && substr(w, j, 1) ~ /^[A-Za-z0-9_]$/ && substr(m, j, 1) == kind; j++) name = name substr(w, j, 1)
        i = j - 1
    } else if (c ~ /^[0-9@*#?$!-]$/) { special = (c ~ /^[1-9@*]$/) ? "\001" : "\003"; i++ }
    else return i
    if (name == "_") special = "\001"
    if (bad) { PC[1] = "\001"; return i }
    if (op == "=") bind_value(name, rest, "")
    base = (special != "") ? special : variable_values(name)
    delete PC; npc = split(base, PC, "\034")
    if (op == "+") { npc = 2; PC[1] = rest; PC[2] = "" }
    else if (op == "-" || op == "=") PC[++npc] = rest
    return i
}
function cross(    k, j, t) {
    t = 0
    if (ncur * npc > 64) { overflow = 1; return }
    for (k = 1; k <= ncur; k++) for (j = 1; j <= npc; j++) NXT[++t] = CUR[k] PC[j]
    delete CUR
    for (k = 1; k <= t; k++) CUR[k] = NXT[k]
    ncur = t; delete NXT
}
function bracket_end(w, i,    j, marker, end) {
    j = i + 1
    if (substr(w, j, 1) ~ /[!^]/) j++
    if (substr(w, j, 1) == "]") j++
    for (; j <= length(w); j++) {
        if (substr(w, j, 1) == "]") return j
        marker = substr(w, j + 1, 1)
        if (substr(w, j, 1) == "[" && marker ~ /[:.=]/) {
            end = index(substr(w, j + 2), marker "]")
            if (!end) return 0
            j += end + 2
        }
    }
    return 0
}
function expand_item(w, m,    i, n, c, mk, lit, j, saved, count) {
    n = length(w); ncur = 1; delete CUR; CUR[1] = ""; lit = ""
    for (i = 1; i <= n; i++) {
        c = substr(w, i, 1); mk = tolower(substr(m, i, 1))
        if (mk == "c") lit = lit "\003"
        else if (c == "$" && mk != "l") {
            delete PC; npc = 1; PC[1] = lit; cross(); lit = ""
            delete saved; count = ncur
            for (j = 1; j <= count; j++) saved[j] = CUR[j]
            i = param(w, m, i)
            delete CUR; ncur = count
            for (j = 1; j <= count; j++) CUR[j] = saved[j]
            if (mk == "u") for (j = 1; j <= npc; j++) if (PC[j] ~ /[*?[ \t\n]/) PC[j] = "\001"
            cross()
        }
        else if (mk == "u" && (c == "*" || glob_negation(w, i))) lit = lit "\004"
        else if (mk == "u" && c == "?") lit = lit "\005"
        else if (mk == "u" && c == "[" && (j = bracket_end(w, i)) > 0) { lit = lit "\006" substr(w, i + 1, j - i - 1) "\007"; i = j }
        else if (mk == "u" && c == "{") lit = lit "\001"
        else lit = lit c
    }
    delete PC; npc = 1; PC[1] = lit; cross()
}
function resolve(w, m,    k, j, words, masks, count, resolved_values, nr, failed) {
    overflow = 0; brace_all(w, m)
    delete words; delete masks; delete resolved_values
    count = nf; failed = overflow
    for (k = 1; k <= count; k++) { words[k] = FW[k]; masks[k] = FM[k] }
    for (k = 1; k <= count && !failed; k++) {
        expand_item(words[k], masks[k])
        if (overflow || nr + ncur > 64) failed = 1
        else for (j = 1; j <= ncur; j++) resolved_values[++nr] = CUR[j]
    }
    delete ALT; nalt = nr
    for (j = 1; j <= nr; j++) ALT[j] = resolved_values[j]
    if (failed) { delete ALT; nalt = 1; ALT[1] = "\001" }
}
function has_sentinel(b) { return b ~ /[\001-\007]/ }
function only_runtime(b,    t) { t = b; gsub("\003", "", t); return t == "" }
# An anchored regex for a name that can hold sentinels: \001 and \003 match any string, \002 one character.
function rxize(b,    k, c, r, j, bracket) {
    r = ""
    for (k = 1; k <= length(b); k++) {
        c = substr(b, k, 1)
        if (c == "\001" || c == "\003") r = r ".*"
        else if (c == "\004") r = r "[^/]*"
        else if (c == "\002" || c == "\005") r = r "[^/]"
        else if (c == "\006") {
            j = index(substr(b, k + 1), "\007"); bracket = substr(b, k + 1, j - 1)
            sub(/^!/, "^", bracket); r = r "[" bracket "]"; k += j
        }
        else if (index(".[]()*+?{}|^$\\", c)) r = r "\\" c
        else r = r c
    }
    return "^" r "$"
}
# Globs use case-sensitive, per-segment matching with Bash default leading-dot rules. Runtime expansions
# are different: they can supply a leading dot without pathname expansion.
function pattern_match(pattern, candidate,    pattern_parts, candidate_parts, np, nc, k) {
    delete pattern_parts; delete candidate_parts
    np = split(pattern, pattern_parts, "/"); nc = split(candidate, candidate_parts, "/")
    if (np != nc) return 0
    for (k = 1; k <= np; k++) {
        if (substr(candidate_parts[k], 1, 1) == "." && substr(pattern_parts[k], 1, 1) ~ /[\004-\006]/) return 0
        if (candidate_parts[k] !~ rxize(pattern_parts[k])) return 0
    }
    return 1
}
# Intersection of two wildcard segments, used for the complete protected extension suffixes rather than
# a handful of sample key names. States advance monotonically, including empty star transitions.
function pattern_units(a, U,    k, n, j) {
    delete U
    for (k = 1; k <= length(a); k++) {
        if (substr(a, k, 1) == "\006") {
            j = index(substr(a, k + 1), "\007"); U[++n] = substr(a, k, j + 1); k += j
        } else U[++n] = substr(a, k, 1)
    }
    return n
}
function non_dot_character(unit,    k, rx) {
    if (substr(unit, 1, 1) != "\006") return unit != "."
    if (unit ~ /[^\001-\177]/) return 1
    rx = rxize(unit)
    for (k = 1; k < 128; k++) if (k != 46 && k != 47 && CH[k] ~ rx) return 1
    return 0
}
function overlap(a, b,    states, i, j, z, x, y, ax, by, na, nb, au, bu, hidden) {
    delete states
    na = pattern_units(a, au); nb = pattern_units(b, bu); states[0,0,0] = 1
    hidden = (substr(a, 1, 1) ~ /[\004-\006]/)
    for (i = 0; i <= na; i++) for (j = 0; j <= nb; j++) for (z = 0; z <= 1; z++) {
        if (!states[i,j,z]) continue
        x = au[i + 1]; y = bu[j + 1]
        ax = (x ~ /^[\001\003\004]$/); by = (y == "\004")
        if (ax) states[i+1,j,z] = 1
        if (by) states[i,j+1,z] = 1
        if (i == na || j == nb) continue
        if (ax && by) states[i,j,1] = 1
        else if (ax) {
            if (!hidden || z || y != ".") states[i,j+1,1] = 1
        } else if (by) {
            if (!hidden || z || non_dot_character(x)) states[i+1,j,1] = 1
        } else if ((!hidden || z || y != ".") && y ~ rxize(x)) states[i+1,j+1,1] = 1
    }
    return states[na,nb,1]
}
function path_probe(a, probe,    path_parts, probe_parts, n, m, k, tail) {
    delete path_parts; delete probe_parts
    n = split(a, path_parts, "/"); m = split(probe, probe_parts, "/")
    if (n < m) return 0
    tail = path_parts[n-m+1]
    for (k = n-m+2; k <= n; k++) tail = tail "/" path_parts[k]
    return pattern_match(tail, probe)
}
# Literal words retain the full classifier. Glob words use canonical names and all protected extensions;
# substring-only token names outside that canonical set are not glob probes.
function alt_kind(a,    b, d, kind, k, glob) {
    # An unsupported expansion may supply slashes too, reaching a protected directory even with a fixed suffix.
    if (index(a, "\001")) return "an unresolved file name that may name a protected file"
    b = a; sub(/.*\//, "", b)
    glob = (a ~ /[\004-\006]/)
    if (!has_sentinel(a)) return sensitive_path(a)
    if (!glob) {
        kind = sensitive_path(a)
        if (kind != "") return kind
        if (only_runtime(b)) return ""
        a = tolower(a); b = tolower(b)
    }
    d = a; sub(/[^\/]*$/, "", d)
    if (has_sentinel(b)) {
        for (k = 1; k <= nprobe; k++) if (pattern_match(b, PROBE[k])) {
            kind = sensitive_path(d PROBE[k])
            if (kind != "") return kind
            if (PROBE[k] == "credentials" || PROBE[k] == ".pypirc") return "a credentials file"
        }
        for (k = 1; k <= nextension; k++) if (overlap(b, "\004." EXT[k])) return "a key or certificate file"
    } else if (glob) {
        for (k = 1; k <= nprobe; k++) if (b == PROBE[k]) {
            kind = sensitive_path(d b)
            if (kind != "") return kind
            if (b == "credentials" || b == ".pypirc") return "a credentials file"
        }
        for (k = 1; k <= nextension; k++) if (b ~ ("\\." EXT[k] "$")) return "a key or certificate file"
    }
    for (k = 1; k <= nprobe2; k++) if (path_probe(a, PROBE2[k])) return sensitive_path(PROBE2[k])
    return ""
}
# The kind of protected file operand k of A names; via_pattern says the answer came from a pattern or a variable.
function operand_kind(A, k, off,    w, m, j, kind) {
    w = A[k]; m = mask_of(A, k); via_pattern = 0
    if (off > 0) { w = substr(w, off + 1); m = substr(m, off + 1) }
    if (!expands(w, m)) return sensitive_path(w)
    resolve(w, m); via_pattern = 1
    for (j = 1; j <= nalt; j++) { kind = alt_kind(ALT[j]); if (kind != "") return kind }
    return ""
}
function bind_value(name, w, m,    key) {
    key = qi SUBSEP name SUBSEP w SUBSEP m
    if (key in BINDING) return
    if (pass == 2) { report("a variable the guard cannot follow", "run the read on its own with literal paths"); return }
    BINDING[key] = 1; bindings++
    BV[name, ++BVN[name]] = w; BVM[name, BVN[name]] = m
}
function unknown_binding(name) {
    UNK[name] = 1
    bind_value(name, "\001", "")
}
function set_var(w, m,    eq, name, append) {
    eq = index(w, "="); name = substr(w, 1, eq - 1); append = sub(/\+$/, "", name); sub(/\[.*$/, "", name)
    if (append) unknown_binding(name)
    bind_value(name, substr(w, eq + 1), substr(m, eq + 1))
}
function collect_bindings(A, s, n, b,    k, w, nameref) {
    if (b == "for" || b == "select") {
        if (A[s + 2] != "in") unknown_binding(A[s + 1])
        else for (k = s + 3; k <= n; k++) bind_value(A[s + 1], A[k], mask_of(A, k))
    }
    if (b ~ /^(declare|typeset|local)$/) for (k = s + 1; k <= n; k++) if (A[k] ~ /^-[a-zA-Z]*n/) nameref = 1
    for (k = s + 1; k <= n; k++) {
        w = A[k]
        if (b == "unset" && w !~ /^-/) bind_value(w, "", "")
        if ((b ~ /^(read|mapfile|readarray)$/ || nameref) && w ~ /^[A-Za-z_][A-Za-z0-9_]*$/) unknown_binding(w)
        if ((b == "getopts" && k == s + 2) || (b == "printf" && A[k - 1] == "-v") || (b == "wait" && A[k - 1] == "-p")) unknown_binding(w)
    }
}
function protected_config(w) { return w ~ /^(GLOBIGNORE|BASHOPTS|BASH_ENV|ENV)(\+?=|$)/ }
function config_report() {
    report("a protected shell configuration change cannot be checked with default glob matching", "run without changing shell glob options")
}
function allowed_option(option) { return option ~ /^(errexit|nounset|pipefail|xtrace|verbose|noglob|errtrace|functrace)$/ }
function shell_invocation(A, s, n,    k, j, flags, c, script) {
    for (k = s + 1; k <= n; k++) {
        if (A[k] == "--" || A[k] !~ /^[-+]/) break
        if (A[k] ~ /^--/) {
            # Only startup switches that preserve the checked shell syntax are safe.
            if (A[k] !~ /^--(norc|noprofile|noediting|verbose|login|version|help)$/) config_report()
            continue
        }
        flags = substr(A[k], 2); script = index(flags, "c")
        for (j = 1; j <= length(flags); j++) {
            c = substr(flags, j, 1)
            if (c != "o" && c != "O") continue
            if (j != length(flags) || expands(A[k + 1], mask_of(A, k + 1)) || !allowed_option(A[k + 1])) config_report()
            k++; break
        }
        if (script) { enqueue(A[k + 1]); return }
    }
    if (nheredoc > 0) hd_shell = 1
}
function shell_config(A, s, n, b,    k, option, j, flags, c) {
    if (b ~ /^(setopt|unsetopt|emulate)$/) config_report()
    if (b ~ /^(export|declare|typeset|local|readonly)$/)
        for (k = s + 1; k <= n; k++) if (protected_config(A[k])) config_report()
    if (b == "set") {
        for (k = s + 1; k <= n; k++) if (expands(A[k], mask_of(A, k))) config_report()
        for (k = s + 1; k <= n; k++) {
            if (A[k] == "--" || A[k] !~ /^[-+]/) break
            flags = substr(A[k], 2)
            for (j = 1; j <= length(flags); j++) {
                c = substr(flags, j, 1)
                if (c == "o") {
                    option = (j < length(flags)) ? substr(flags, j + 1) : A[++k]
                    if (!allowed_option(option)) config_report()
                    break
                }
                if (c !~ /^[euxfvET]$/) config_report()
            }
        }
    }
    if (b != "shopt") return
    for (k = s + 1; k <= n; k++) {
        if (A[k] == "extglob" || expands(A[k], mask_of(A, k)) || A[k] ~ /^-[a-zA-Z]*[su]/) config_report()
    }
}
function check_inputs(A, s, n,    k, kind) {
    for (k = s; k < n; k++) {
        if (A[k] !~ /^[0-9]*<+[>&]?$/ || tolower(substr(mask_of(A, k), 1, 1)) != "u") continue
        kind = operand_kind(A, k + 1, 0)
        if (kind != "") report("an input redirection reads " kind, "name the file and the key you need; for an env template read the .env.example")
        k++
    }
}
function note_redirect(A, s) {
    if (A[s] ~ /^[0-9]*<$/) { REDIRECT_TOKENS[++nrt] = s + 1; ROFF[nrt] = 0 }
    else if (match(A[s], /^[0-9]*<[^<>&]/)) { REDIRECT_TOKENS[++nrt] = s; ROFF[nrt] = RLENGTH - 1 }
}
function generic_files(A, s, n, why,    k, kind) {
    for (k = s + 1; k <= n; k++) {
        if (substr(A[k], 1, 1) == "-" && length(A[k]) > 1) continue
        kind = operand_kind(A, k, 0)
        if (kind != "") report(why " reads " kind " and prints credentials", "name the file and the key you need in your report; ask the user to confirm a value instead of printing it; list the directory with ls, then name the exact files")
    }
}
# A command word the shell builds by expansion: run the normal analysis for every name it can be, and refuse the
# operands that name a protected file when it cannot be resolved to one.
function dyn_command(A, s, n,    j, na, b, d, S, unresolved, rx) {
    resolve(A[s], mask_of(A, s))
    na = nalt; for (j = 1; j <= na; j++) S[j] = ALT[j]
    unresolved = 0
    for (j = 1; j <= na; j++) {
        b = tolower(S[j]); sub(/.*\//, "", b)
        if (!has_sentinel(b)) { A[s] = S[j]; VM[s] = ""; analyze(A, s, n); continue }
        unresolved = 1
        if (only_runtime(b)) continue
        rx = rxize(b)
        for (d = 1; d <= ndispatch; d++) if (DN[d] ~ rx) { A[s] = DN[d]; VM[s] = ""; analyze(A, s, n) }
    }
    if (unresolved) generic_files(A, s, n, "a command whose name is built from a pattern or variable")
}

# ── tools ──
function kube(A, s, n,    verb, m, parts, k, r, rlist, projection) {
    kparse(A, s + 1, n, "nlcsfkLR")
    projection = index(outfmt, "=") ? substr(outfmt, index(outfmt, "=") + 1) : ""
    verb = P[1]
    if (verb == "exec") {
        if (dd) analyze(A, dd + 1, n)
        else if (np >= 3) analyze(A, PI[3], n)
        return
    }
    if (verb == "config") {
        if (P[2] == "view" && raw) report("kubectl config view --raw prints the cluster credentials", "kubectl config view (redacted), kubectl config current-context, or kubectl config get-contexts")
        return
    }
    if (verb != "get" && verb != "describe") return
    # `get pods,cm` and `get pod web` name one resource type; `get svc/web secret/db` names one per argument.
    rlist = P[2]
    if (index(P[2], "/")) for (k = 3; k <= np; k++) if (index(P[k], "/")) rlist = rlist "," P[k]
    delete parts
    m = split(tolower(rlist), parts, ",")
    for (k = 1; k <= m; k++) {
        r = parts[k]; sub(/\/.*/, "", r); sub(/\..*/, "", r)
        if (r == "secret" || r == "secrets") {
            if (verb == "describe" || outfmt != "name")
                report("kubectl " verb " secret prints secret data", "kubectl get secrets -o name to list them, or name the secret and key in your report instead of reading it")
        } else if (r in SENS) {
            if (verb == "describe")
                report("kubectl describe " r " prints the full container spec, including inline credentials", "kubectl get " r " -o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, or kubectl get events / kubectl logs; say which field you need")
            else if (outfmt != "" && outfmt !~ /^(wide|name|jsonpath|jsonpath-as-json|custom-columns|go-template|template|templatefile|go-template-file|jsonpath-file|custom-columns-file)(=|$)/)
                report("kubectl get " r " -o " outfmt " prints the full container spec, including inline credentials", "-o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, -o wide, or the status fields you need")
            else if (tmplfile || outfmt ~ /^(templatefile|go-template-file|jsonpath-file|custom-columns-file)(=|$)/)
                report("kubectl get " r " with a file-based projection may print the full container spec, including inline credentials", "use an inline projection of the needed fields: -o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, -o wide, or the status fields you need")
            else if (outfmt ~ /^(go-template|template)(=|$)/ || (tmpl != "" && outfmt !~ /^(jsonpath|jsonpath-as-json)(=|$)/))
                report("kubectl get " r " with a projection that selects the container spec prints the full container spec, including inline credentials", "-o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, -o wide, or the status fields you need")
            else if ((outfmt ~ /^(jsonpath|jsonpath-as-json)(=|$)/ && (!proj_jsonpath_allowed(projection) || !proj_jsonpath_allowed(tmpl))) || (outfmt ~ /^custom-columns(=|$)/ && !proj_columns_allowed(projection)))
                report("kubectl get " r " with a projection that selects the container spec prints the full container spec, including inline credentials", "-o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, -o wide, or the status fields you need")
        }
    }
}
function fmt_leaks(f) {
    f = tolower(f)
    return f ~ /env|json|\.args/ || f ~ /[{][{] *\.? *[}][}]/ || f ~ /\.config *[}][}]/
}
function dock(A, s, n,    off, sub1, nxt, insp, k, listing) {
    kparse(A, s + 1, n, "Hceuwlpfv")
    off = (P[1] == "compose") ? 1 : 0
    sub1 = P[1 + off]; nxt = P[2 + off]
    insp = (sub1 == "inspect") || ((sub1 == "container" || sub1 == "image" || sub1 == "service") && nxt == "inspect")
    if (insp && (fmtval == "" || fmt_leaks(fmtval)))
        report("docker inspect prints the container environment, including credentials", "docker inspect --format " SQ "{{.State.Status}}" SQ " (or {{.Config.Image}}, {{.NetworkSettings.IPAddress}}), docker ps, or docker logs")
    if (off && sub1 == "config") {
        for (k = s + 1; k <= n; k++) if (A[k] ~ /^(--services|--volumes|--profiles|--images|--networks|--quiet|-q)$/) listing = 1
        if (!listing) report("docker compose config prints the resolved environment, including credentials", "docker compose config --services, or read the compose file and name the variables instead of printing their values")
    }
    if (sub1 == "exec" && np >= 3 + off) analyze(A, PI[3 + off], n)
}
function gh(A, s, n,    i, w, mutating, k) {
    kparse(A, s + 1, n, "XHfFqtR")
    if (P[1] == "secret") {
        if (P[2] != "set" && P[2] != "delete" && P[2] != "remove")
            report("gh secret " P[2] " reads the repository secret store", "gh secret set NAME (writes nothing to the output); refer to secrets by name only")
        return
    }
    if (P[1] == "auth") {
        if (P[2] == "token") report("gh auth token prints the GitHub token", "gh auth status (without --show-token) to confirm you are signed in")
        else if (P[2] == "status")
            for (i = s + 1; i <= n; i++) if (A[i] == "-t" || A[i] == "--show-token") report("gh auth status --show-token prints the GitHub token", "gh auth status without --show-token")
        return
    }
    if (P[1] == "api") {
        mutating = 0
        for (i = s + 1; i < n; i++) if (A[i] == "-X" || A[i] == "--method") { w = toupper(A[i + 1]); if (w == "PUT" || w == "PATCH" || w == "DELETE") mutating = 1 }
        if (!mutating)
            for (k = 2; k <= np; k++)
                if (tolower(P[k]) ~ /(^|\/)secrets([\/?]|$)/) report("gh api " P[k] " reads the repository secret store", "gh secret set NAME for writes; name the secret instead of reading it")
    }
}
function vault(A, s, n) {
    kparse(A, s + 1, n, "")
    if (P[1] == "read" || (P[1] == "kv" && (P[2] == "get" || P[2] == "read")))
        report("vault " P[1] (P[1] == "kv" ? " " P[2] : "") " prints secret values", "vault kv list or vault kv metadata get for the key names; name the path and key in your report")
}
function aws(A, s, n,    i, decrypt) {
    kparse(A, s + 1, n, "")
    if (P[1] == "secretsmanager" && P[2] == "get-secret-value") report("aws secretsmanager get-secret-value prints the secret", "aws secretsmanager list-secrets or describe-secret (metadata only)")
    if (P[1] == "configure" && P[2] == "export-credentials") report("aws configure export-credentials prints the credentials", "aws sts get-caller-identity to confirm who you are")
    if (P[1] == "ssm" && P[2] ~ /^get-parameter/) {
        decrypt = 0
        for (i = s + 1; i <= n; i++) if (A[i] == "--with-decryption") decrypt = 1
        if (decrypt) report("aws ssm " P[2] " --with-decryption prints decrypted parameters", "aws ssm describe-parameters, or get-parameter without --with-decryption")
    }
}
function azure(A, s, n) {
    kparse(A, s + 1, n, "n")
    if (P[1] == "keyvault" && P[2] == "secret" && (P[3] == "show" || P[3] == "download")) report("az keyvault secret " P[3] " prints the secret", "az keyvault secret list, or az keyvault secret show-deleted metadata only; name the secret in your report")
    if (P[1] == "account" && P[2] == "get-access-token") report("az account get-access-token prints a bearer token", "az account show")
}
function gcloud(A, s, n,    k) {
    kparse(A, s + 1, n, "")
    if (P[1] == "secrets" && P[2] == "versions" && P[3] == "access") report("gcloud secrets versions access prints the secret", "gcloud secrets versions list, or gcloud secrets describe (metadata only)")
    if (P[1] == "auth") for (k = 2; k <= np; k++) if (P[k] ~ /^print-(access|identity)-token$/) report("gcloud auth " P[k] " prints a token", "gcloud auth list")
}
# Scans one attached or clustered short option (-ePATTERN, -nePATTERN, -m5, -F:) of a pattern or script tool:
# sc_pat is set when it supplies the pattern or script, sc_skip when its value is the next word.
function scan_short(w, shortvals, sed_like,    i, c, last) {
    sc_pat = 0; sc_skip = 0; last = length(w)
    for (i = 2; i <= last; i++) {
        c = substr(w, i, 1)
        if (c == "e" || c == "f") { sc_pat = 1; sc_skip = (i == last); return }
        if (index(shortvals, c)) { sc_skip = (i == last); return }
        if (sed_like && c == "i") return
    }
}
function reader(A, s, n,    b, k, w, mode, havepat, need, afterdd, kind, valued, valued2, shortvals) {
    b = base(A[s]); mode = ""; valued = " "; valued2 = " "; shortvals = ""
    if (b ~ /^(grep|egrep|fgrep|rg|ag)$/) { mode = "pattern"; shortvals = (b ~ /^(grep|egrep|fgrep)$/) ? "mABCdD" : (b == "rg") ? "mABCdjEMrgtT" : "ABCGgmpW"; valued = " --glob --type --type-not --max-count --include --exclude --exclude-dir --context --after-context --before-context --max-depth --threads " }
    else if (b ~ /^(sed|awk|gawk|jq|yq)$/) { mode = "script"; shortvals = "vF"; valued = " --indent "; valued2 = " --arg --argjson --slurpfile --rawfile " }
    # The first operand is the pattern or script unless -e/-f/--regexp/--file/--expression supplies it; only the rest are files.
    havepat = 0
    if (mode != "") for (k = s + 1; k <= n; k++) {
        w = A[k]
        if (w == "--") break
        if (w ~ /^--(regexp|file|expression|from-file)(=|$)/) havepat = 1
        else if (w ~ /^-[a-zA-Z]/) { scan_short(w, shortvals, b == "sed"); if (sc_pat) havepat = 1 }
    }
    need = (mode != "" && !havepat); afterdd = 0
    for (k = s + 1; k <= n; k++) {
        w = A[k]
        if (w == "--" && !afterdd) { afterdd = 1; continue }
        if (!afterdd && substr(w, 1, 1) == "-" && length(w) > 1) {
            if (mode != "" && w ~ /^-[a-zA-Z]/) { scan_short(w, shortvals, b == "sed"); if (sc_skip) k++ }
            else if (mode != "" && (w ~ /^--(regexp|file|expression|from-file)$/ || index(valued, " " w " "))) k++
            else if (index(valued2, " " w " ")) k += 2
            continue
        }
        if (need) { need = 0; continue }
        kind = operand_kind(A, k, 0)
        if (kind != "" && via_pattern) report(b " of a name that can match " kind " prints credentials", "list the directory with ls, then name the exact files; for an env template read the .env.example")
        else if (kind != "") report(b " of " kind " prints credentials", "name the file and the key you need in your report; ask the user to confirm a value instead of printing it; for an env template read the .env.example")
    }
}

function drop_redirects(A, s, n,    k, m, w) {
    m = s
    for (k = s + 1; k <= n; k++) {
        w = A[k]
        if (w ~ /^[0-9]*[<>]/) { if (w ~ /^[0-9]*[<>]+&?-?$/) k++; continue }
        A[++m] = w
    }
    return m
}
function transfer_files(A, s, n, b,    k, last, options, kind, w, eq, off, ended) {
    for (k = s + 1; k <= n; k++) if (A[k] ~ /^-./) options = 1
    last = n
    if (b ~ /^(cp|mv|ln)$/ && !options && n == s + 2) last--
    for (k = s + 1; k <= last; k++) {
        w = A[k]; off = 0
        if (!ended && w == "--") { ended = 1; continue }
        if (!ended && w ~ /^-./) {
            eq = index(w, "=")
            if (w !~ /^--/ || !eq) continue
            off = eq
        }
        kind = operand_kind(A, k, off)
        if (kind != "") report(b " reads " kind, "name only non-credential source files")
    }
}
function analyze(A, s, n,    w, b, k, names, p, config, get, show, first) {
    nrt = 0; delete REDIRECT_TOKENS; delete ROFF; first = s
    while (s <= n) {
        w = A[s]; b = base(w)
        if (is_assign(w)) { if (protected_config(w)) config_report(); s++; continue }
        if (w ~ /^[0-9]*[<>]/) { note_redirect(A, s); s += (w ~ /^[0-9]*[<>]+&?-?$/) ? 2 : 1; continue }
        if (b in KW) { s++; continue }
        if (b == "command" || b == "builtin") { s++; if (s <= n && (A[s] == "-v" || A[s] == "-V")) return; if (s <= n && A[s] == "-p") s++; if (A[s] == "--") s++; continue }
        if (b == "sudo" || b == "doas") { s = skipopts(A, s + 1, n, "ugChprtUD"); continue }
        if (b == "xargs") { s = skipopts(A, s + 1, n, "IniPLdsEa"); continue }
        if (b == "timeout") { s = skipopts(A, s + 1, n, "ks") + 1; continue }
        if (b == "nice" || b == "watch") { s = skipopts(A, s + 1, n, "n"); continue }
        if (b == "nohup" || b == "time" || b == "exec" || b == "setsid") { s = skipopts(A, s + 1, n, ""); continue }
        if (b == "rtk") { s++; if (s <= n && base(A[s]) == "proxy") s++; continue }
        if (b == "env") {
            s++
            while (s <= n) {
                w = A[s]
                if (is_assign(w)) { if (protected_config(w)) config_report(); s++ }
                else if (w ~ /^[0-9]*[<>]/) s += (w ~ /^[0-9]*[<>]+&?-?$/) ? 2 : 1
                else if (w == "-u" || w == "-C" || w == "-S" || w == "--unset" || w == "--chdir" || w == "--split-string") s += 2
                else if (substr(w, 1, 1) == "-") s++
                else break
            }
            if (s > n) { report("env with no command prints the whole environment, including credentials (env | ... is blocked too)", "printenv NAME for one variable that is not a credential, or [ -n \"${NAME:-}\" ] && echo set to test that a variable exists"); return }
            continue
        }
        break
    }
    if (s > n) {
        for (k = first; k <= n; k++) if (is_assign(A[k])) set_var(A[k], mask_of(A, k))
        return
    }
    if (expands(A[s], mask_of(A, s))) return dyn_command(A, s, n)
    b = base(A[s])
    shell_config(A, s, n, b)
    collect_bindings(A, s, n, b)
    if (b == "for" || b == "select" || b == "unset") return
    if (b == "dd") {
        for (k = s + 1; k <= n; k++) if (A[k] ~ /^if=/ && (config = operand_kind(A, k, 3)) != "") report("dd reads " config, "name only a non-credential input file")
        return
    }
    if (b ~ /^(cp|mv|ln|rsync|scp|tar|zip)$/) { transfer_files(A, s, n, b); return }
    if (b ~ /^(export|declare|typeset|local|readonly)$/) for (k = s + 1; k <= n; k++) if (is_assign(A[k])) set_var(A[k], mask_of(A, k))
    if (b == "bash" || b == "sh" || b == "zsh" || b == "dash" || b == "ksh") { shell_invocation(A, s, n); return }
    if (b == "eval") { p = ""; for (k = s + 1; k <= n; k++) p = p " " A[k]; enqueue(p); return }
    if (b == "kubectl" || b == "oc") return kube(A, s, n)
    if (b == "docker" || b == "podman") return dock(A, s, n)
    if (b == "docker-compose") { delete T; T[1] = "docker"; T[2] = "compose"; for (k = s + 1; k <= n; k++) T[k - s + 2] = A[k]; return dock(T, 1, n - s + 2) }
    if (b == "gh") return gh(A, s, n)
    if (b == "vault") return vault(A, s, n)
    if (b == "aws") return aws(A, s, n)
    if (b == "az") return azure(A, s, n)
    if (b == "gcloud") return gcloud(A, s, n)
    if (b == "pulumi") {
        for (k = s + 1; k <= n; k++) if (A[k] ~ /^--show-secrets/) report("pulumi --show-secrets prints decrypted secrets", "pulumi config / pulumi stack output without --show-secrets (secrets stay masked)")
        return
    }
    # `cratis config get` prints a secret as [secret]; --show-secret resolves it through the secrets provider.
    if (b == "cratis") {
        config = 0
        for (k = s + 1; k <= n; k++) {
            if (A[k] == "config") config = 1
            else if (config && A[k] == "get") get = 1
            if (A[k] ~ /^--show-secret/) show = 1
        }
        if (config && get && show) report("cratis config get --show-secret prints a decrypted secret", "cratis config get without --show-secret (a secret prints as [secret]); name the key in your report")
        return
    }
    # A redirect is not a variable name: `printenv > f` and `export > f` still dump the whole environment.
    if (b == "printenv" || b == "export" || b == "declare" || b == "typeset") n = drop_redirects(A, s, n)
    if (b == "printenv") {
        names = 0
        for (k = s + 1; k <= n; k++) {
            if (substr(A[k], 1, 1) == "-") continue
            names++
            if (tolower(A[k]) ~ /token|secret|key|pass|pwd|credential|auth|cert|conn|dsn/) report("printenv " A[k] " prints a credential", "[ -n \"${" A[k] ":-}\" ] && echo set, which confirms it exists without printing it")
        }
        if (names == 0) report("printenv with no variable name prints the whole environment, including credentials", "printenv with the one non-credential variable you need, e.g. printenv HOME")
        return
    }
    if (b == "export") {
        names = 0; p = 0
        for (k = s + 1; k <= n; k++) { if (A[k] == "-p") p = 1; else if (substr(A[k], 1, 1) != "-") names++ }
        if (p || names == 0) report("export with no variable name prints the whole environment, including credentials", "printenv with the one non-credential variable you need")
        return
    }
    if (b == "declare" || b == "typeset") {
        names = 0; p = 0
        for (k = s + 1; k <= n; k++) { if (A[k] ~ /^-[a-zA-Z]*[px][a-zA-Z]*$/) p = 1; else if (substr(A[k], 1, 1) != "-") names++ }
        if (p && names == 0) report(b " -p/-x with no variable name prints the whole environment, including credentials", "printenv with the one non-credential variable you need")
        return
    }
    if (b ~ /^(cat|tac|nl|less|more|head|tail|bat|batcat|strings|xxd|od|hexdump|cut|sort|uniq|base64|wc|grep|egrep|fgrep|rg|ag|sed|awk|gawk|jq|yq)$/) {
        reader(A, s, n)
        for (k = 1; k <= nrt; k++) {
            if (REDIRECT_TOKENS[k] > n || operand_kind(A, REDIRECT_TOKENS[k], ROFF[k]) == "") continue
            report(b " of " operand_kind(A, REDIRECT_TOKENS[k], ROFF[k]) " through an input redirection prints credentials", "name the file and the key you need in your report; ask the user to confirm a value instead of printing it; for an env template read the .env.example")
        }
    }
}

BEGIN {
    SQ = "\047"
    split("namespace context kubeconfig server cluster user as as-group selector field-selector container label-columns sort-by request-timeout filename kustomize template chunk-size token env env-file workdir host config project-name file profile detach-keys log-level method header field raw-field jq repo hostname input region query endpoint-url vault-name subscription resource-group name id version project secret account", L, " ")
    for (i in L) LV["--" L[i]] = 1
    delete L; split("! { } if then else elif do while until", L, " ")
    for (i in L) KW[L[i]] = 1
    delete L; split("pods po pod deployments deployment deploy statefulsets statefulset sts daemonsets daemonset ds jobs job cronjobs cronjob cj configmaps configmap cm replicasets replicaset rs replicationcontrollers rc all", L, " ")
    for (i in L) SENS[L[i]] = 1
    # Decoding tables for $'...' strings, and the names a pattern is probed against.
    for (i = 1; i < 256; i++) { CH[i] = sprintf("%c", i); ORD[CH[i]] = i }
    delete L; split("0 1 2 3 4 5 6 7 8 9 a b c d e f", L, " ")
    for (i = 1; i <= 16; i++) HEX[L[i]] = i - 1
    delete L; split("a b e E f n r t v", L, " "); split("7 8 27 27 12 10 13 9 11", L2, " ")
    for (i = 1; i <= 9; i++) ESC[L[i]] = L2[i] + 0
    ESC["\\"] = 92; ESC["\047"] = 39; ESC["\""] = 34; ESC["?"] = 63
    nprobe = split(".env .env.local .env.production .envrc id_rsa id_ed25519 id_ecdsa id_dsa credentials token token.json tokens.json github_token .netrc .npmrc .pypirc .pgpass .git-credentials kubeconfig", PROBE, " ")
    nprobe2 = split(".kube/config .aws/credentials .docker/config.json /proc/1/environ /run/secrets/token /var/run/secrets/token", PROBE2, " ")
    nextension = split("pem key p12 pfx jks keystore kdbx kubeconfig env", EXT, " ")
    ndispatch = split("cat tac nl less more head tail bat batcat strings xxd od hexdump cut sort uniq base64 wc dd cp mv ln rsync scp tar zip shopt set setopt unsetopt emulate grep egrep fgrep rg ag sed awk gawk jq yq kubectl oc docker podman docker-compose gh vault aws az gcloud pulumi cratis printenv export declare typeset env bash sh zsh dash ksh eval", DN, " ")
    nstem = split(".env .pem .p12 .pfx id_rsa id_ed25519 id_ecdsa id_dsa .netrc .npmrc .pgpass .pypirc .git-credentials .kube/ kubeconfig credentials .docker/ /environ run/secrets", STEM, " ")
    CMD = ENVIRON["GS_COMMAND"]
    for (pass = 1; pass <= 2; pass++) {
        delete queue; queued = 0; qi = 0; delete seen; reports = ""
        nheredoc = 0; hd_shell = 0; commands = 0; complex = 0; simple_ok = 1; TAINT = ""
        enqueue(CMD)
        while (qi < queued) { qi++; tokenize(queue[qi]) }
        if (pass == 1 && bindings > 0) continue
        if (TAINT != "" && (complex || queued != 1 || commands != 1 || !simple_ok)) {
            other_reports = reports; reports = ""
            report("the command names a protected file (" TAINT ") and is not a single literal read", "run the read on its own with literal paths, e.g. cat .env.example; split compound commands into separate calls; do not build the name with variables, globs, loops, pipes, redirections or nested shells; for a commit message or PR body that mentions the name, write the text to a file with the file tool and pass the file")
            reports = reports other_reports
        }
        printf "%s", reports
        break
    }
}
AWK

# Only literal rg needs the invocation directory to distinguish file and directory operands.
command_cwd="$PWD"
case "$probe" in *rg*) command_cwd="$(jq -r '.cwd // empty' <<<"$input")" || parser_failed; command_cwd="${command_cwd:-$PWD}" ;; esac
findings="$(GS_COMMAND="$command_text" GS_CWD="$command_cwd" LC_ALL=C awk "$awk_program" </dev/null)" || parser_failed
[ -n "$findings" ] || exit 0

{
    while IFS=$'\t' read -r what alternative; do
        [ -n "$what" ] || continue
        printf 'BLOCKED by cratis-guard-secrets: %s. Use instead: %s.\n' "$what" "$alternative"
    done <<EOF
$findings
EOF
    cat <<'EOF'

secrets.md: a credential must never enter an agent transcript, and a transcript is saved, summarized and
shared. Ask for the field you need (a jsonpath, custom-columns, -o name, a status field) and say which one;
refer to a secret by name (secret/key, environment variable) in notes, briefs and messages, never by value.

If a value has already appeared in output: stop, do not copy it onward, report "SECURITY INCIDENT" with the
tool-call location and the credential kind, and ask for rotation.

Do not retry this, rephrase it, or work around the guard (another wrapper, a script file, a variable, an
alias). A person who authorized the read sets CRATIS_HOOKS_ALLOW_SECRET_READS=1 in the environment the agent
harness was started from; setting it inside the command has no effect.
EOF
    case "$findings" in
        *"the command names a protected file ("*) printf '%s\n' 'Running the read on its own with literal paths, as suggested above, is the intended retry.' ;;
    esac
} >&2
exit 2
