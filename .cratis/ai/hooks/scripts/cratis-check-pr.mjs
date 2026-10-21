#!/usr/bin/env node
// cratis-ai-managed: hooks/scripts/cratis-check-pr.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, readdirSync, statSync, writeFileSync, renameSync } from 'node:fs';
import { homedir } from 'node:os';
import { isAbsolute, join, resolve } from 'node:path';
import { parseArgs } from 'node:util';
import { Script } from 'node:vm';
import { fileURLToPath } from 'node:url';

const intents = ['major', 'minor', 'patch', 'no-release'];
const commandTimeout = 15_000;
const reviewedRulesRef = '40125b4fcae388e62ed9471edfffd0e991422f0a';
const hookMode = process.argv[2] === '--hook';
let claudeHook = false;
const hookWarnings = [];
const warningLine = line => /^(::warning\b|::notice title=Release notes (?:drift )?not checked|Warning:)/.test(line);
const warning = message => {
    if (hookMode && claudeHook) hookWarnings.push(message);
    else console.error(message);
};
const run = (command, args, options = {}) => spawnSync(command, args, {
    encoding: 'utf8', timeout: commandTimeout, maxBuffer: 32 * 1024 * 1024, ...options,
});
const gh = (args, options = {}) => {
    const result = run('gh', args, options);
    if (result.error || result.status !== 0) throw new Error(`gh ${args[0]} failed: ${result.stderr?.trim() || result.error?.message || result.status}`);
    return result.stdout;
};
const git = (args, cwd = process.cwd()) => {
    const result = run('git', args, { cwd });
    return result.status === 0 ? result.stdout.trim() : '';
};

function workflowCallers(source) {
    return [...source.matchAll(/^\s*uses:\s*['"]?Cratis\/Workflows\/\.github\/workflows\/(verify-(?:release-notes|semver-label|release-intent))\.yml@([^\s'"#]+)/gmi)]
        .map(match => ({ name: match[1], ref: match[2] }));
}

function callers(cwd = process.cwd()) {
    const root = git(['rev-parse', '--show-toplevel'], cwd);
    if (!root) return [];
    const directory = join(root, '.github/workflows');
    try {
        return readdirSync(directory).filter(name => /\.ya?ml$/.test(name)).flatMap(name => {
            const source = readFileSync(join(directory, name), 'utf8');
            return workflowCallers(source);
        });
    } catch { return []; }
}

function repositoryName(value) {
    return value?.replace(/^(?:https?:\/\/github\.com\/|(?:ssh:\/\/)?git@github\.com[:/])/i, '')
        .replace(/^github\.com\//i, '').replace(/\/$/, '').replace(/\.git$/i, '');
}

function originRepository(cwd) {
    return repositoryName(git(['remote', 'get-url', 'origin'], cwd));
}

function targetCallers(repository, cwd) {
    if (cwd && repository?.toLowerCase() === originRepository(cwd)?.toLowerCase()) return callers(cwd);
    let files;
    try { files = JSON.parse(gh(['api', `repos/${repository}/contents/.github/workflows`])); }
    catch (error) {
        if (/HTTP 404/.test(error.message)) return [];
        throw new Error(`Could not determine release-workflow opt-in for ${repository}: ${error.message}`);
    }
    return files.filter(file => file.type === 'file' && /\.ya?ml$/.test(file.name)).flatMap(file =>
        workflowCallers(gh(['api', '-H', 'Accept: application/vnd.github.raw', `repos/${repository}/contents/${file.path}`])));
}

function optedIn(cwd) {
    return /^Cratis\/[^/]+$/i.test(originRepository(cwd)) && callers(cwd).length > 0;
}

// Fetch the actual inline programs, not a second implementation of the rules.
function programs(workflow) {
    const found = new Map();
    for (const match of workflow.matchAll(/^([ \t]*)node - <<'JS'\r?\n([\s\S]*?)^[ \t]*JS[ \t]*$/gm)) {
        const source = match[2].split('\n').map(line => line.startsWith(match[1]) ? line.slice(match[1].length) : line).join('\n');
        for (const name of ['release-notes', 'release-notes-drift']) {
            if (source.split('\n').some(line => line.trim() === `// cratis:program ${name}`)) found.set(name, source);
        }
    }
    if (found.size !== 2) throw new Error('the workflow does not contain both marked release-note programs');
    const bundle = '"use strict";\n' + [...found].map(([name, source]) => `if (process.argv[2] === ${JSON.stringify(name)}) {\n${source}\n}\n`).join('');
    new Script(bundle); // Invalid or truncated downloads must not replace a usable cache.
    return bundle;
}

function rules(warn, repositoryCallers) {
    const ref = repositoryCallers.find(caller => caller.name === 'verify-release-notes')?.ref || reviewedRulesRef;
    const floating = !/^[a-f0-9]{40}$/i.test(ref);
    const cache = join(process.env.XDG_CACHE_HOME || join(homedir(), '.cache'), 'cratis', 'release-notes', ref);
    try {
        const workflow = gh(['api', '-H', 'Accept: application/vnd.github.raw', `repos/Cratis/Workflows/contents/.github/workflows/verify-release-notes.yml?ref=${encodeURIComponent(ref)}`]);
        const bundle = programs(workflow);
        const bytes = Buffer.from(workflow);
        const sha = createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex');
        mkdirSync(cache, { recursive: true, mode: 0o700 });
        const file = join(cache, `${sha}.cjs`);
        const temporary = `${file}.${process.pid}.tmp`;
        writeFileSync(temporary, bundle, { mode: 0o600 });
        renameSync(temporary, file);
        return file;
    } catch (error) {
        if (floating) {
            const bundled = fileURLToPath(new URL('./cratis-release-notes-reviewed.cjs', import.meta.url));
            new Script(readFileSync(bundled, 'utf8'));
            warn(`Could not fetch release-note rules at ${ref}; using bundled reviewed ${reviewedRulesRef}. ${error.message}`);
            return bundled;
        }
        try {
            const cached = readdirSync(cache).filter(name => /^[a-f0-9]{40}\.cjs$/.test(name))
                .map(name => ({ file: join(cache, name), modified: statSync(join(cache, name)).mtimeMs }))
                .sort((a, b) => b.modified - a.modified);
            for (const { file } of cached) {
                try {
                    const source = readFileSync(file, 'utf8');
                    if (!source.includes('// cratis:program release-notes\n') || !source.includes('// cratis:program release-notes-drift\n')) continue;
                    new Script(source);
                    warn(`Could not fetch release-note rules; using cached ${file}. ${error.message}`);
                    return file;
                } catch { /* Try the next complete cache entry. */ }
            }
        } catch { /* No cache available. */ }
        warn(`unchecked: could not fetch release-note rules and nothing is cached. ${error.message}`);
        return undefined;
    }
}

function check(argv, repositoryCallers, lookupOptions) {
    const { values } = parseArgs({ args: argv, options: {
        'body-file': { type: 'string' }, label: { type: 'string', multiple: true },
        'add-label': { type: 'string', multiple: true }, 'remove-label': { type: 'string', multiple: true },
        pr: { type: 'string' }, base: { type: 'string' }, repo: { type: 'string' }, strict: { type: 'boolean' },
    } });
    let warned = false;
    const warn = message => { warned = true; warning(`Warning: ${message}`); };
    let pull;
    // gh requires a selector when --repo is supplied; otherwise preserve current-branch lookup.
    const lookupRepository = lookupOptions ? lookupOptions.env.GH_REPO : values.repo;
    const repoArgs = values.pr && lookupRepository ? ['--repo', lookupRepository] : [];
    if (values.pr !== undefined) {
        pull = JSON.parse(gh(['pr', 'view', ...(values.pr ? [values.pr] : []), ...repoArgs,
            '--json', 'labels,body,author,baseRefName'], lookupOptions || (values.repo ? { env: { ...process.env, GH_REPO: values.repo } } : {})));
    }
    if (!values['body-file'] && !pull) throw new Error('--body-file is required when creating a pull request');
    const split = labels => (labels || []).flatMap(label => label.split(',')).map(label => label.trim()).filter(Boolean);
    const labels = new Set(values.label ? split(values.label) : (pull?.labels || []).map(label => label.name));
    for (const label of split(values['remove-label'])) labels.delete(label);
    for (const label of split(values['add-label'])) labels.add(label);
    const intent = intents.filter(label => labels.has(label));
    if (intent.length !== 1) throw new Error(`Exactly one release-intent label is required (major/minor/patch/no-release); found ${intent.join(', ') || 'none'}.`);
    const body = values['body-file'] ? readFileSync(resolve(values['body-file']), 'utf8') : pull.body || '';
    let repository = values.repo;
    let defaultBranch = git(['symbolic-ref', '--short', 'refs/remotes/origin/HEAD']).replace(/^origin\//, '') || 'main';
    try {
        const metadata = JSON.parse(gh(['repo', 'view', ...(repository ? [repository] : []), '--json', 'nameWithOwner,defaultBranchRef']));
        repository = metadata.nameWithOwner;
        defaultBranch = metadata.defaultBranchRef?.name || defaultBranch;
    } catch {
        repository ||= git(['remote', 'get-url', 'origin']).replace(/^.*github\.com[:/]/, '').replace(/\.git$/, '') || 'Cratis/<Repository>';
    }
    let author = pull?.author?.login;
    if (pull?.author?.is_bot && author?.startsWith('app/')) author = `${author.slice(4)}[bot]`;
    if (!author) {
        try { author = gh(['api', 'user', '--jq', '.login']).trim(); } catch { author = ''; }
    }
    // Match the reusable workflow's job-level Dependabot exemption as well as its body exemption.
    // The release-intent check above still applies to bot-authored pull requests.
    if (author === 'dependabot[bot]') return 0;
    const base = values.base || pull?.baseRefName || defaultBranch;
    const file = rules(warn, repositoryCallers || targetCallers(repository, process.cwd()));
    if (!file) return 3;
    const env = { PATH: process.env.PATH, HOME: homedir(), LANG: 'C.UTF-8',
        PR_BODY: body, PR_LABELS: JSON.stringify([...labels]),
        PR_AUTHOR: author, PR_BASE: base, DEFAULT_BRANCH: defaultBranch, GITHUB_REPOSITORY: repository,
        BASE: base, REPOSITORY: repository };
    for (const program of ['release-notes', 'release-notes-drift']) {
        const result = run(process.execPath, [file, program], { env, timeout: 60_000 });
        if (result.error || result.status !== 0) {
            (hookMode ? process.stderr : process.stdout).write(result.stdout || '');
            process.stderr.write(result.stderr || '');
            if (result.error) console.error(result.error.message);
            return 1;
        }
        // no-release may omit a change list, but other contract violations still block locally.
        const output = (result.stdout || '') + (result.stderr || '');
        if (intent[0] === 'no-release' && program === 'release-notes'
            && output.split('\n').some(line => /^::warning title=Release notes(?::|%3A) (?!No release notes::)/.test(line))) {
            (hookMode ? process.stderr : process.stdout).write(result.stdout || '');
            process.stderr.write(result.stderr || '');
            return 1;
        }
        if (!hookMode) process.stdout.write(result.stdout || '');
        for (const line of ((result.stdout || '') + (result.stderr || '')).split('\n').filter(warningLine)) {
            warned = true;
            if (hookMode) warning(line);
        }
        if (!hookMode) process.stderr.write(result.stderr || '');
    }
    return values.strict && warned ? 1 : 0;
}

// A deliberately bounded shell-text guard, not a shell interpreter. Quotes and escaped spaces are
// preserved; separators are recognized only outside quotes. Variables, aliases and script files
// are outside this guard's scope. Dynamic values of guarded options fail closed.
function commands(text) {
    const result = [];
    let words = [], writes = [], word = '', started = false, quote = '', redirect;
    const heredocs = [];
    const endWord = () => {
        if (started) {
            if (redirect?.heredoc) heredocs.push({ delimiter: word, tabs: redirect.tabs });
            else if (redirect?.write && !(redirect.duplicate && /^(?:\d+|-)$/.test(word))) writes.push(word);
            else if (!redirect) words.push(word);
            redirect = undefined;
        }
        word = ''; started = false;
    };
    const endCommand = () => {
        endWord();
        if (words.length || writes.length) result.push({ words, writes });
        words = []; writes = [];
    };
    for (let index = 0; index < text.length; index++) {
        const char = text[index];
        if (char === '\\' && quote !== "'") {
            const next = text[++index];
            if (next && next !== '\n') { word += next; started = true; }
        } else if (quote) {
            if (char === quote) quote = ''; else word += char;
        } else if (char === '#' && !started) {
            // Quotes inside comments have no shell meaning.
            while (index + 1 < text.length && text[index + 1] !== '\n') index++;
        } else if (char === '"' || char === "'") { quote = char; started = true;
        } else if (char === '<' || char === '>') {
            // A numeric word touching a redirect is a file descriptor, not an argument.
            if (started && /^\d+$/.test(word)) { word = ''; started = false; }
            endWord();
            const operator = /^(?:<<<|<<-|<<|>>|<>|>&|<&|>\||[<>])/.exec(text.slice(index))[0];
            redirect = { heredoc: operator === '<<' || operator === '<<-', tabs: operator === '<<-', duplicate: operator === '>&', write: operator.startsWith('>') || operator === '<>' };
            index += operator.length - 1;
        } else if (/[;&|\n]/.test(char)) {
            endCommand();
            if (char === '\n') {
                // Here-document payloads are data; do not tokenize their quotes or gh examples.
                for (const { delimiter, tabs } of heredocs.splice(0)) {
                    let found = false;
                    while (index + 1 < text.length) {
                        const start = index + 1;
                        const end = text.indexOf('\n', start);
                        const line = text.slice(start, end < 0 ? text.length : end);
                        index = end < 0 ? text.length - 1 : end;
                        if ((tabs ? line.replace(/^\t+/, '') : line) === delimiter) { found = true; break; }
                    }
                    if (!found) throw new Error('Unclosed heredoc in pull-request command; write the body file first in a separate command.');
                }
            }
        } else if (/\s/.test(char)) endWord();
        else { word += char; started = true; }
    }
    if (quote) throw new Error('Unclosed quote in pull-request command; use a simple gh command with --body-file.');
    endCommand();
    return result;
}

function executableWords(words) {
    const result = [...words];
    let environmentRepository = process.env.GH_REPO;
    while (result.length) {
        if (/^[A-Za-z_][A-Za-z_0-9]*=/.test(result[0])) {
            if (result[0].startsWith('GH_REPO=')) environmentRepository = result[0].slice('GH_REPO='.length);
            result.shift();
        } else if (['rtk', 'env', 'command', '--'].includes(result[0])) result.shift();
        else break;
    }
    return { words: result, environmentRepository };
}

function effectiveRepository(words, cwd, environmentRepository) {
    // gh selects an edit's PR URL before --repo, then GH_REPO, then the checkout origin.
    let repository, target;
    let explicitRepository = false;
    for (let index = 3; index < words.length; index++) {
        const compact = /^(-[bFlBRtarmpHT])(.+)$/.exec(words[index]);
        const [option, ...attached] = compact ? [compact[1], compact[2]] : words[index].split('=');
        if (option === '--repo' || option === '-R') {
            explicitRepository = true;
            repository = attached.length ? attached.join('=') : words[++index];
        } else if (['--body', '-b', '--body-file', '-F', '--label', '-l', '--add-label', '--remove-label', '--base', '-B',
            '--title', '-t', '--assignee', '-a', '--reviewer', '-r', '--milestone', '-m', '--project', '-p',
            '--add-assignee', '--remove-assignee', '--add-reviewer', '--remove-reviewer', '--add-project', '--remove-project',
            '--head', '-H', '--template', '-T', '--recover'].includes(option)) {
            if (!attached.length) index++;
        } else if (!option.startsWith('-') && words[2] === 'edit' && !target) target = words[index];
    }
    try {
        const url = new URL(target);
        const pull = /^\/([^/]+)\/([^/]+)\/pull\/(\d+)/.exec(url.pathname);
        if (['http:', 'https:'].includes(url.protocol) && url.hostname === 'github.com' && pull) return `${pull[1]}/${pull[2]}`;
    } catch { /* A branch name or PR number is not a URL. */ }
    if (explicitRepository) {
        if (!repository || repository.startsWith('-') || /[$`]/.test(repository)) throw new Error('Use a literal value for --repo, not a shell expansion.');
        return repositoryName(repository);
    }
    if (environmentRepository) {
        if (/[$`]/.test(environmentRepository)) throw new Error('Use a literal value for GH_REPO, not a shell expansion.');
        return repositoryName(environmentRepository);
    }
    return cwd ? originRepository(cwd) : undefined;
}

function hook() {
    let payload;
    try { payload = JSON.parse(readFileSync(0, 'utf8')); } catch { return 0; }
    claudeHook = payload?.hook_event_name === 'PreToolUse';
    const text = payload?.tool_input?.command;
    if (typeof text !== 'string' || !/\bgh\s+pr\s+(create|edit)\b/.test(text.replace(/\\\r?\n/g, ''))) return 0;
    // Match each simple command, never the same text inside an echo/grep argument.
    let cwd = payload.cwd || process.cwd();
    let knownDirectory = true;
    let parsed;
    try { parsed = commands(text); } catch (error) { if (!optedIn(cwd)) return 0; throw error; }
    const written = new Set();
    for (const command of parsed) {
        const { words, environmentRepository } = executableWords(command.words);
        const writes = [...command.writes];
        if (words[0] === 'tee') writes.push(...words.slice(1).filter(word => !word.startsWith('-')));
        if (knownDirectory) for (const file of writes) written.add(resolve(cwd, file));
        if (words[0] === 'cd') {
            const directory = words[1] === '--' ? words[2] : words[1];
            if (!directory || /[$`~]/.test(directory)) knownDirectory = false;
            else if (knownDirectory || isAbsolute(directory)) {
                cwd = resolve(cwd, directory);
                knownDirectory = true;
            }
            continue;
        }
        if (words[0] !== 'gh' || words[1] !== 'pr' || !['create', 'edit'].includes(words[2])) continue;
        if (words.includes('--help') || words.includes('-h')) continue;
        const repository = effectiveRepository(words, knownDirectory ? cwd : undefined, environmentRepository);
        if (!knownDirectory && !repository) {
            if (!optedIn(cwd)) continue;
            throw new Error('Use a literal directory before gh pr create/edit.');
        }
        if (!/^Cratis\/[^/]+$/i.test(repository)) continue;
        const repositoryCallers = targetCallers(repository, knownDirectory ? cwd : undefined);
        if (!repositoryCallers.length) continue;
        if (!knownDirectory) throw new Error('Use a literal directory before gh pr create/edit.');
        const args = [];
        let target = '', hasBody = false;
        for (let index = 3; index < words.length; index++) {
            const compact = /^(-[FlBRtarmpHT])(.+)$/.exec(words[index]);
            const [option, ...attached] = compact ? [compact[1], compact[2]] : words[index].split('=');
            if (option === '--body' || option === '-b' || /^-b./.test(option)) {
                throw new Error('write the body to `.ai-work/pr-body.md` and use `--body-file`; inline --body/-b is blocked.');
            }
            const mapped = { '--body-file': '--body-file', '-F': '--body-file', '--label': '--label', '-l': '--label',
                '--add-label': '--add-label', '--remove-label': '--remove-label', '--base': '--base', '-B': '--base', '--repo': '--repo', '-R': '--repo' }[option];
            if (mapped) {
                const value = attached.length ? attached.join('=') : words[++index];
                if (!value || /[$`]/.test(value) || value === '-') throw new Error(`Use a literal value for ${option}, not stdin or a shell expansion.`);
                if (mapped !== '--repo') args.push(mapped, value);
                if (mapped === '--body-file') {
                    hasBody = true;
                    if (written.has(resolve(cwd, value))) throw new Error('write the body file first, then run gh pr create/edit in a separate command; an earlier redirection may change the submitted body.');
                }
            } else if (!option.startsWith('-') && words[2] === 'edit' && !target) {
                target = words[index];
            } else if (['--title', '-t', '--assignee', '-a', '--reviewer', '-r', '--milestone', '-m', '--project', '-p',
                '--add-assignee', '--remove-assignee', '--add-reviewer', '--remove-reviewer', '--add-project', '--remove-project',
                '--head', '-H', '--template', '-T', '--recover'].includes(option) && !attached.length) index++;
        }
        if (words[2] === 'create' && !hasBody) throw new Error('write the body to `.ai-work/pr-body.md` and use `--body-file` when creating a pull request.');
        if (words[2] === 'edit') args.push('--pr', target);
        args.push('--repo', repository);
        // Pin PR lookup only for a command-targeted repository; otherwise let gh resolve its base repo.
        const lookupEnvironment = { ...process.env };
        const targetedRepository = effectiveRepository(words, undefined, environmentRepository);
        if (targetedRepository) lookupEnvironment.GH_REPO = targetedRepository;
        else delete lookupEnvironment.GH_REPO;
        process.chdir(cwd);
        const code = check(args, repositoryCallers, { env: lookupEnvironment });
        if (code !== 0 && code !== 3) throw new Error('The pull-request body or release intent failed cratis-check-pr. Fix the reported violations before retrying.');
    }
    return 0;
}

try {
    process.exitCode = hookMode ? hook() : check(process.argv.slice(2));
} catch (error) {
    console.error(`${hookMode ? 'BLOCKED by cratis-guard-pr-body: ' : ''}${error.message}`);
    process.exitCode = hookMode ? 2 : 1;
} finally {
    if (claudeHook && hookWarnings.length) console.log(JSON.stringify({ systemMessage: hookWarnings.join('\n'),
        hookSpecificOutput: { hookEventName: 'PreToolUse', additionalContext: hookWarnings.join('\n') } }));
}
