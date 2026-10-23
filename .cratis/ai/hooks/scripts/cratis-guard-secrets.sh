#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-guard-secrets.sh
# PreToolUse hook (Bash) — hard block on shell commands that print credentials into the transcript.
#
# secrets.md says a credential must never enter an agent transcript, and lists the command classes that
# print one: full Kubernetes specs and secrets, the process environment, container inspection, secret-manager
# reads and credential files. This guard is what enforces it: every simple command in the shell command is
# found (quotes, $( ), backticks, subshells, pipelines, here-documents, `bash -c`, `eval`, `kubectl exec ... --`
# and `docker exec`, and the wrappers sudo/env/timeout/xargs/rtk are looked through) and classified. A command
# that only mentions a word inside an argument (echo, rg, grep, git commit -m) is never a finding.
# Exit 2 blocks the tool call and sends stderr back to the model; every other command exits 0.
#
# What it sees is the command text. A credential reached through a variable, an alias, a function, a script
# file or a pipe into a shell is invisible to it, as is a recursive search with no named file; a projection it
# cannot see through (a template or `-o jsonpath` that is not a bare `{.}`) is trusted. A redirect does not
# exempt anything: `env > f` is blocked like `env`. See .cratis/ai/hooks/README.md.
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
hook_have jq || exit 0

command_text="$(hook_json "$input" '.tool_input.command')"
[ -n "$command_text" ] || exit 0

# Cheap exit for the overwhelming majority of shell commands, which mention none of these words.
case "$(printf '%s' "$command_text" | tr '[:upper:]' '[:lower:]')" in
    *kubectl*|*printenv*|*env*|*docker*|*podman*|*"gh "*|*vault*|*pulumi*|*aws*|*"az "*|*gcloud*|*kubeconfig*|*.kube*|\
    *export*|*declare*|*typeset*|*id_rsa*|*id_ed25519*|*id_ecdsa*|*id_dsa*|*.pem*|*.p12*|*.pfx*|*token*|*.netrc*|*.npmrc*|\
    *credentials*|*.pgpass*|*secrets*) ;;
    *) exit 0 ;;
esac

IFS= read -r -d '' awk_program <<'AWK' || true
function report(what, alt) {
    if (what in seen) return
    seen[what] = 1
    printf "%s\t%s\n", what, alt
}
function enqueue(text) {
    if (text == "" || queued >= 64) return
    queue[++queued] = text
}
function base(word) { word = tolower(word); sub(/.*\//, "", word); return word }

# ── tokenizer: splits text into simple commands, queueing nested command text ──
function flush_word() {
    if (inword) { W[++nw] = word; WP[nw] = wplain }
    word = ""; inword = 0; wquoted = 0; wplain = 0
}
# Output redirections are removed before a command is analyzed, so a redirect never shifts a CLI's subcommand
# or operands (`kubectl get > /dev/stdout secrets`). Nothing is exempted: the command itself is still analyzed.
# A word is a redirection only when its whole operator lies in the word's unquoted prefix (WP: characters before
# the first quoted or escaped one), so `1">"b` and `">x"` stay operands; input redirections (`< file`) stay too.
function end_cmd(    k, m, w) {
    flush_word()
    m = 0
    for (k = 1; k <= nw; k++) {
        w = W[k]
        if (match(w, /^([0-9]*|&)>[>|]?/) && RLENGTH <= WP[k]) {
            if (RLENGTH == length(w)) k++
            continue
        }
        V[++m] = w
    }
    if (m > 0) analyze(V, 1, m)
    split("", W); split("", WP); split("", V); nw = 0
}
function cmdsub(s, i, n,    start, depth, c, j) {
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
    n = length(s); i = 1; nw = 0; word = ""; inword = 0; wquoted = 0; wplain = 0; nheredoc = 0; hd_shell = 0
    split("", W); split("", WP)
    while (i <= n) {
        c = substr(s, i, 1)
        if (c == "\\") {
            c2 = substr(s, i + 1, 1)
            if (c2 == "\n") { i += 2; continue }
            wquoted = 1; word = word c2; inword = 1; i += 2; continue
        }
        if (c == "\047") {
            wquoted = 1; inword = 1
            j = index(substr(s, i + 1), "\047")
            if (j == 0) { word = word substr(s, i + 1); i = n + 1; continue }
            word = word substr(s, i + 1, j - 1); i += j + 1; continue
        }
        if (c == "\"") {
            wquoted = 1; inword = 1; i++
            while (i <= n) {
                c = substr(s, i, 1)
                if (c == "\\") { word = word substr(s, i + 1, 1); i += 2; continue }
                if (c == "\"") { i++; break }
                if (c == "$" && substr(s, i + 1, 1) == "(") { i = cmdsub(s, i + 2, n); word = word "x"; continue }
                if (c == "`") {
                    j = index(substr(s, i + 1), "`")
                    if (j == 0) { i = n + 1; break }
                    enqueue(substr(s, i + 1, j - 1)); i += j + 1; word = word "x"; continue
                }
                word = word c; i++
            }
            continue
        }
        if (c == "$" && substr(s, i + 1, 1) == "(") { wquoted = 1; inword = 1; i = cmdsub(s, i + 2, n); word = word "x"; continue }
        if (c == "`") {
            wquoted = 1; inword = 1
            j = index(substr(s, i + 1), "`")
            if (j == 0) { i = n + 1; continue }
            enqueue(substr(s, i + 1, j - 1)); i += j + 1; word = word "x"; continue
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
        if ((c == "&" || c == "|") && inword && !wquoted && word ~ />$/) { word = word c; wplain++; i++; continue }
        if (c == "&" && substr(s, i + 1, 1) == ">") { flush_word(); word = c; inword = 1; wplain = 1; i++; continue }
        if (c == ";" || c == "&" || c == "|" || c == "(" || c == ")") { end_cmd(); i++; continue }
        if (substr(s, i, 2) == "<<" && substr(s, i, 3) != "<<<") {
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
        if (!wquoted) wplain++
        word = word c; inword = 1; i++
    }
    end_cmd()
}

# ── option parsing shared by the CLIs: positionals P/PI, output format, -f/--format value, --raw ──
function kparse(A, s, n, shortvals,    i, w, eq, key, val, f, rest) {
    split("", P); split("", PI)
    np = 0; outfmt = ""; fmtval = ""; tmpl = ""; dd = 0; raw = 0
    for (i = s; i <= n; i++) {
        w = A[i]
        if (w == "--") { dd = i; break }
        if (substr(w, 1, 2) == "--") {
            eq = index(w, "="); key = w; val = ""
            if (eq) { key = substr(w, 1, eq - 1); val = substr(w, eq + 1) }
            if (key == "--output") { if (!eq) { i++; val = A[i] } outfmt = tolower(val) }
            else if (key == "--format") { if (!eq) { i++; val = A[i] } fmtval = val }
            else if (key == "--template") { if (!eq) { i++; val = A[i] } tmpl = val }
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
# `-o jsonpath={.}` and `-o go-template={{.}}` print the whole object, exactly like -o json.
function proj_leaks(t) {
    t = tolower(t); gsub(/[ \t]/, "", t)
    return t ~ /[{](\.|@|\.items|\.items\[\*\])[}]/
}
function skipopts(A, s, n, vals) {
    while (s <= n && substr(A[s], 1, 1) == "-") {
        if (length(A[s]) == 2 && index(vals, substr(A[s], 2, 1))) s += 2
        else s++
    }
    return s
}
function is_assign(w) { return w ~ /^[A-Za-z_][A-Za-z0-9_]*=/ }

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
    if (b == ".netrc" || b == ".npmrc" || b == ".pgpass" || b == ".git-credentials") return "a credentials file"
    if (b == "credentials" && l ~ /\.aws\//) return "the AWS credentials file"
    if (l ~ /\.docker\/config\.json$/) return "the Docker registry credentials"
    if (l ~ /\/proc\/[^\/]*\/environ$/) return "a process environment"
    if (l ~ /^\/(var\/)?run\/secrets\//) return "mounted secrets"
    return ""
}

# ── tools ──
function kube(A, s, n,    verb, m, parts, k, r, rlist) {
    kparse(A, s + 1, n, "nlcsfkLR")
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
    m = split(tolower(rlist), parts, ",")
    for (k = 1; k <= m; k++) {
        r = parts[k]; sub(/\/.*/, "", r); sub(/\..*/, "", r)
        if (r == "secret" || r == "secrets") {
            if (verb == "describe" || outfmt != "name")
                report("kubectl " verb " secret prints secret data", "kubectl get secrets -o name to list them, or name the secret and key in your report instead of reading it")
        } else if (r in SENS) {
            if (verb == "describe")
                report("kubectl describe " r " prints the full container spec, including inline credentials", "kubectl get " r " -o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, or kubectl get events / kubectl logs; say which field you need")
            else if (outfmt == "json" || outfmt == "yaml")
                report("kubectl get " r " -o " outfmt " prints the full container spec, including inline credentials", "-o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, -o wide, or the status fields you need")
            else if ((outfmt ~ /^(jsonpath|go-template|jsonpath-as-json)=/ || tmpl != "") && proj_leaks(tmpl outfmt))
                report("kubectl get " r " with a projection that selects the whole object prints the full container spec, including inline credentials", "-o jsonpath=" SQ "{.items[*].spec.containers[*].image}" SQ ", -o custom-columns=NAME:.metadata.name,STATUS:.status.phase, -o name, -o wide, or the status fields you need")
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
        kind = sensitive_path(w)
        if (kind != "") report(b " of " kind " prints credentials", "name the file and the key you need in your report; ask the user to confirm a value instead of printing it; for an env template read the .env.example")
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
function analyze(A, s, n,    w, b, k, names, p) {
    while (s <= n) {
        w = A[s]; b = base(w)
        if (is_assign(w)) { s++; continue }
        if (w ~ /^[0-9]*[<>]/) { s += (w ~ /^[0-9]*[<>]+&?-?$/) ? 2 : 1; continue }
        if (b in KW) { s++; continue }
        if (b == "command" || b == "builtin") { s++; if (s <= n && (A[s] == "-v" || A[s] == "-V")) return; if (s <= n && A[s] == "-p") s++; continue }
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
                if (is_assign(w)) s++
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
    if (s > n) return
    b = base(A[s])
    if (b == "bash" || b == "sh" || b == "zsh" || b == "dash" || b == "ksh") {
        for (k = s + 1; k <= n; k++)
            if (A[k] ~ /^-[a-zA-Z]*c[a-zA-Z]*$/) { enqueue(A[k + 1]); return }
        if (nheredoc > 0) hd_shell = 1
        return
    }
    if (b == "eval") { p = ""; for (k = s + 1; k <= n; k++) p = p " " A[k]; enqueue(p); return }
    if (b == "kubectl" || b == "oc") return kube(A, s, n)
    if (b == "docker" || b == "podman") return dock(A, s, n)
    if (b == "docker-compose") { split("", T); T[1] = "docker"; T[2] = "compose"; for (k = s + 1; k <= n; k++) T[k - s + 2] = A[k]; return dock(T, 1, n - s + 2) }
    if (b == "gh") return gh(A, s, n)
    if (b == "vault") return vault(A, s, n)
    if (b == "aws") return aws(A, s, n)
    if (b == "az") return azure(A, s, n)
    if (b == "gcloud") return gcloud(A, s, n)
    if (b == "pulumi") {
        for (k = s + 1; k <= n; k++) if (A[k] ~ /^--show-secrets/) report("pulumi --show-secrets prints decrypted secrets", "pulumi config / pulumi stack output without --show-secrets (secrets stay masked)")
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
    if (b ~ /^(cat|tac|nl|less|more|head|tail|bat|batcat|strings|xxd|od|hexdump|cut|sort|uniq|base64|grep|egrep|fgrep|rg|ag|sed|awk|gawk|jq|yq)$/) reader(A, s, n)
}

BEGIN {
    SQ = "\047"
    split("namespace context kubeconfig server cluster user as as-group selector field-selector container label-columns sort-by request-timeout filename kustomize template chunk-size token env env-file workdir host config project-name file profile detach-keys log-level method header field raw-field jq repo hostname input region query endpoint-url vault-name subscription resource-group name id version project secret account", L, " ")
    for (i in L) LV["--" L[i]] = 1
    split("! { } if then else elif do while until", L, " ")
    for (i in L) KW[L[i]] = 1
    split("pods po pod deployments deployment deploy statefulsets statefulset sts daemonsets daemonset ds jobs job cronjobs cronjob cj configmaps configmap cm replicasets replicaset rs replicationcontrollers rc all", L, " ")
    for (i in L) SENS[L[i]] = 1
    CMD = ENVIRON["GS_COMMAND"]
    enqueue(CMD)
    while (qi < queued) { qi++; tokenize(queue[qi]) }
}
AWK

findings="$(GS_COMMAND="$command_text" LC_ALL=C awk "$awk_program" </dev/null)"
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
} >&2
exit 2
