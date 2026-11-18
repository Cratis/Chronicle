#!/usr/bin/env node
// cratis-ai-managed: hooks/scripts/cratis-check-handoff.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { parseArgs } from 'node:util';

const headings = [
    'Goal', 'Repositories and worktrees', 'Verified facts (with evidence)',
    'Plans and hypotheses (not verified)', 'Watch and action', 'Finished, results not yet read',
    'Open questions carried forward', 'User constraints (verbatim)',
    'User instructions since the last handoff (verbatim)', 'Next commands', 'Files to read',
];
const repositoryHeader = ['Path', 'Repository', 'Branch', 'HEAD (full SHA)', 'State', 'Ahead/behind'];
const watchHeader = ['Id', 'Target (immutable)', 'Action on completion', 'How to cancel'];
const clean = value => value.trim().replace(/^`(.*)`$/, '$1').trim();
const absent = value => !value || clean(value).toLowerCase() === 'none';

function git(directory, args) {
    const result = spawnSync('git', ['-C', directory, ...args], { encoding: 'utf8', timeout: 15_000 });
    return {
        ok: result.status === 0,
        output: result.stdout?.trim() || '',
        reason: result.stderr?.trim() || result.error?.message || `git exited ${result.status}`,
        status: result.status,
    };
}

function argumentsFor(argv) {
    const { values, positionals } = parseArgs({ args: argv, allowPositionals: true, options: {
        json: { type: 'boolean' }, strict: { type: 'boolean' },
        repo: { type: 'string' }, 'state-note': { type: 'string' },
    } });
    if (positionals.length > 1) throw new Error('Expected at most one handoff path.');
    return { ...values, path: positionals[0] };
}

function fallbackRoot(directory) {
    let current = directory;
    while (true) {
        if (existsSync(join(current, '.ai-work'))) return current;
        const parent = dirname(current);
        if (parent === current) return directory;
        current = parent;
    }
}

function newestHandoff(root) {
    const work = join(root, '.ai-work');
    if (!existsSync(work)) throw new Error(`No handoff found under ${work}.`);
    const candidates = readdirSync(work, { withFileTypes: true }).filter(entry => entry.isDirectory())
        .flatMap(entry => readdirSync(join(work, entry.name))
            .filter(name => /^handoff.*\.md$/.test(name)).map(name => join(work, entry.name, name)))
        .filter(path => statSync(path).isFile())
        .sort((left, right) => statSync(right).mtimeMs - statSync(left).mtimeMs || left.localeCompare(right));
    if (!candidates.length) throw new Error(`No handoff found under ${work}/*/handoff*.md.`);
    return candidates[0];
}

function input(options) {
    const start = resolve(options.repo || process.cwd());
    const discoveryRoot = git(start, ['rev-parse', '--show-toplevel']);
    const searchRoot = options.repo ? start : discoveryRoot.output || fallbackRoot(start);
    const handoff = options.path ? resolve(options.path) : newestHandoff(searchRoot);
    const body = readFileSync(handoff, 'utf8');
    if (!/^# Handoff:.*$/m.test(body)) throw new Error(`Not a handoff: ${handoff} (missing # Handoff: title).`);
    const repository = git(options.repo ? start : dirname(handoff), ['rev-parse', '--show-toplevel']);
    const repoRoot = options.repo ? start : repository.output || fallbackRoot(dirname(handoff));
    return { handoff, body, repoRoot, hasGit: repository.ok };
}

function findings(options, document) {
    const report = { ok: true, handoff: document.handoff, repoRoot: document.repoRoot, errors: [], warnings: [], skipped: [] };
    const add = (severity, code, message, line) => {
        if (options.strict && severity === 'warning') severity = 'error';
        const finding = { severity, code, message, ...(line ? { line } : {}) };
        report[severity === 'error' ? 'errors' : 'warnings'].push(finding);
    };
    return {
        report,
        error: (code, message, line) => add('error', code, message, line),
        warning: (code, message, line) => add('warning', code, message, line),
        skip: (check, reason) => report.skipped.push({ check, reason }),
    };
}

function splitSections(body) {
    const sections = new Map();
    const order = [];
    let section;
    for (const [index, text] of body.split(/\r?\n/).entries()) {
        if (text.startsWith('## ')) {
            const name = text.slice(3);
            order.push(name);
            section = { line: index + 1, lines: [] };
            sections.set(name, section);
        } else if (section) section.lines.push({ text, line: index + 1 });
    }
    return { sections, order };
}

function checkSection(name, section, context) {
    if (!section) context.error('section-missing', `Missing section: ${name}.`);
    else if (!section.lines.some(item => item.text.trim())) context.warning('section-empty', `Write none in empty section: ${name}.`, section.line);
}

function sectionsOf(body, context) {
    const { sections, order } = splitSections(body);
    for (const name of headings) checkSection(name, sections.get(name), context);
    const expected = headings.filter(name => sections.has(name));
    if (order.join('\n') !== expected.join('\n')) context.error('section-order', 'Sections must use the template headings in order.');
    return sections;
}

function cellsOf(text) {
    return text.trim().replace(/^\|/, '').replace(/\|$/, '').split('|').map(clean);
}

function tableRows(section, header, context) {
    if (!section) return [];
    const lines = section.lines.filter(item => item.text.trim().startsWith('|'));
    const first = lines[0];
    if (!first || JSON.stringify(cellsOf(first.text)) !== JSON.stringify(header)) {
        context.error('table-header', `Expected table header: | ${header.join(' | ')} |.`, first?.line || section.line);
        return [];
    }
    return lines.slice(1).filter(item => !/^\|?\s*:?-+/.test(item.text.trim()))
        .map(item => ({ cells: cellsOf(item.text), line: item.line }));
}

function bullets(section) {
    return (section?.lines || []).filter(item => /^\s*[-*+]\s+/.test(item.text))
        .map(item => ({ text: item.text.replace(/^\s*[-*+]\s+/, '').trim(), line: item.line }));
}

function pathFrom(root, value) {
    const path = clean(value);
    if (absent(path) || path.startsWith('~')) return undefined;
    return resolve(root, path);
}

function firstCodeSpan(text) {
    return /`([^`]+)`/.exec(text)?.[1];
}

const looksLikePath = value => !/\s/.test(value) && (value.includes('/') || /\.[A-Za-z0-9]+$/.test(value));

function readPaths(sections, root, context) {
    const paths = bullets(sections.get('Files to read'))
        .map(item => ({ ...item, text: firstCodeSpan(item.text) ?? clean(item.text) }))
        .filter(item => !absent(item.text));
    for (const item of paths) {
        const path = pathFrom(root, item.text);
        if (!path || !existsSync(path)) context.error('files-to-read-missing', `Cannot resolve Files to read path: ${item.text}.`, item.line);
    }
    return paths;
}

function watchRows(rows, context) {
    for (const { cells, line } of rows) {
        const [id, target = '', action = '', cancel = ''] = cells;
        if (absent(id)) continue;
        if (absent(action)) context.error('watch-action-missing', `Watch ${id} needs an Action on completion.`, line);
        if (absent(target) || /^(latest|the PR|current)$/i.test(target)) context.error('watch-target-mutable', `Watch ${id} needs an immutable target, not ${target || 'an empty cell'}.`, line);
        if (absent(cancel)) context.warning('watch-cancel-missing', `Watch ${id} needs How to cancel.`, line);
    }
}

function unreadResults(section, root, context) {
    for (const item of bullets(section)) {
        for (const match of item.text.matchAll(/`([^`]+)`/g)) {
            if (!looksLikePath(match[1])) continue;
            const path = pathFrom(root, match[1]);
            if (!path || !existsSync(path)) context.warning('result-missing', `Unread result path does not exist: ${match[1]}.`, item.line);
        }
    }
}

function gitValue(directory, args, label, context) {
    const result = git(directory, args);
    if (!result.ok) context.skip(label, result.reason);
    return result.ok ? result.output : undefined;
}

function compareBranch(repository, branch, context) {
    const { directory, label, line } = repository;
    const actual = gitValue(directory, ['branch', '--show-current'], `${label}: branch`, context);
    if (actual !== undefined && actual !== branch) context.error('branch-mismatch', `${label}: branch is ${actual || '(detached)'}, recorded ${branch}.`, line);
}

function compareHead(repository, recorded, context) {
    const { directory, label, line } = repository;
    if (!/^[a-f\d]{40}$/i.test(recorded)) {
        context.error('sha-malformed', `${label}: expected a full 40-character hexadecimal SHA, got ${recorded}.`, line);
        return;
    }
    const actual = gitValue(directory, ['rev-parse', 'HEAD'], `${label}: HEAD`, context);
    if (actual === undefined || actual === recorded.toLowerCase()) return;
    const ancestry = git(directory, ['merge-base', '--is-ancestor', recorded, actual]);
    if (ancestry.ok) context.warning('head-moved', `${label}: HEAD moved from ${recorded} to ${actual}.`, line);
    else if (ancestry.status === 1 || /(?:not a valid|bad object|invalid|unknown revision)/i.test(ancestry.reason)) {
        context.error('head-unrelated', `${label}: recorded HEAD ${recorded} is not an ancestor of ${actual}.`, line);
    } else context.skip(`${label}: ancestry`, ancestry.reason);
}

function checkRepository(row, document, context) {
    const [path, , branch = '', sha = ''] = row.cells;
    if (absent(path)) return undefined;
    const directory = pathFrom(document.repoRoot, path);
    if (!directory || !existsSync(directory)) {
        context.error('worktree-missing', `Cannot resolve worktree: ${path}.`, row.line);
        context.skip(`repository ${path}`, `worktree-missing: ${path}`);
        return undefined;
    }
    if (!document.hasGit) {
        context.skip(`repository ${path}`, 'no-git-repository');
        return undefined;
    }
    const repository = { directory, branch, sha, line: row.line, label: path };
    compareBranch(repository, branch, context);
    compareHead(repository, sha, context);
    return repository;
}

function preconditions(body, first, context) {
    if (!first) return;
    const text = body.split(/^## /m)[0];
    const branch = /\bbranch\s+`([^`]+)`/i.exec(text)?.[1];
    const sha = /\bHEAD\s+`([^`]+)`/.exec(text)?.[1];
    const repository = { ...first, label: 'preconditions', line: undefined };
    if (branch && branch !== first.branch) compareBranch(repository, branch, context);
    if (sha && sha !== first.sha) compareHead(repository, sha, context);
}

function stateNotePath(options, document, paths) {
    const named = paths.find(item => /(^|\/)STATE\.md$/.test(item.text));
    const sibling = join(dirname(document.handoff), 'STATE.md');
    const value = options['state-note'] || named?.text || sibling;
    return pathFrom(document.repoRoot, value);
}

function stateNote(note, first, document, context) {
    if (!note || !existsSync(note)) {
        context.skip('state-note', 'no-state-note');
        return;
    }
    if (!first) {
        context.skip('state-note', document.hasGit ? 'no-repository-row' : 'no-git-repository');
        return;
    }
    const timestamp = gitValue(first.directory, ['log', '-1', '--format=%ct'], 'state-note: commit time', context);
    if (timestamp !== undefined && statSync(note).mtimeMs < Number(timestamp) * 1000) {
        context.warning('state-note-stale', `State note ${note} is older than the branch tip.`);
    }
}

function validate(options, document) {
    const context = findings(options, document);
    const sections = sectionsOf(document.body, context);
    const rows = tableRows(sections.get('Repositories and worktrees'), repositoryHeader, context);
    const watches = tableRows(sections.get('Watch and action'), watchHeader, context);
    const paths = readPaths(sections, document.repoRoot, context);
    watchRows(watches, context);
    unreadResults(sections.get('Finished, results not yet read'), document.repoRoot, context);
    const checked = rows.map(row => checkRepository(row, document, context));
    preconditions(document.body, checked[0], context);
    stateNote(stateNotePath(options, document, paths), checked[0], document, context);
    if (!document.hasGit) context.skip('git preconditions', 'no-git-repository');
    context.report.ok = context.report.errors.length === 0;
    return context.report;
}

function print(report, json) {
    if (json) {
        console.log(JSON.stringify(report));
        return;
    }
    console.log(`handoff: ${report.handoff} (repo root: ${report.repoRoot})`);
    for (const finding of [...report.errors, ...report.warnings]) console.log(`${finding.severity} ${finding.code}: ${finding.message}`);
    for (const skipped of report.skipped) console.log(`skipped: ${skipped.check}: ${skipped.reason}`);
    console.log(`handoff ${report.ok ? 'OK' : 'FAILED'} (${report.errors.length} errors, ${report.warnings.length} warnings, ${report.skipped.length} checks skipped)`);
}

try {
    const options = argumentsFor(process.argv.slice(2));
    const report = validate(options, input(options));
    print(report, options.json);
    process.exitCode = report.ok ? 0 : 1;
} catch (error) {
    console.error(error.message);
    process.exitCode = 2;
}
