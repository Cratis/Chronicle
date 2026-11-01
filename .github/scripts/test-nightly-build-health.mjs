// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import vm from 'node:vm';
import { reconcile, restTransport, main } from './nightly-build-health.mjs';
import { workflowPath, trackerTitle } from './nightly-build-health-policy.mjs';

// Any accidental default fetch fails locally; every allowed response is a synthetic fixture.
globalThis.fetch = async () => { throw new Error('Live networking is forbidden in fixtures'); };

const repo = 'example/chronicle';
const root = `/repos/${repo}`;
const now = Date.parse('2026-11-01T12:00:00Z');
const run = (id = 1, conclusion = 'failure', extra = {}) => ({
    id, run_number: id, run_attempt: 1, workflow_id: 42, path: workflowPath,
    event: 'schedule', head_branch: 'main', head_repository: { full_name: repo },
    status: 'completed', conclusion, created_at: `2026-10-${String(20 + id).padStart(2, '0')}T06:00:00Z`,
    head_sha: 'a'.repeat(40), ...extra
});

function applyWrite(issues, method, path, body) {
    if (method === 'POST' && path === `${root}/issues`) {
        const created = { ...body, number: issues.length + 1, state: 'open', user: { login: 'github-actions[bot]', type: 'Bot' } };
        issues.push(created);
        return { data: structuredClone(created) };
    }
    const issue = issues.find(item => path === `${root}/issues/${item.number}`);
    assert.ok(issue, 'fixture forbids unknown writes');
    Object.assign(issue, body);
    return { data: structuredClone(issue) };
}

const staticReads = runs => [
    [path => path === root, () => ({ full_name: repo, default_branch: 'main' })],
    [path => path === `${root}/actions/workflows/dotnet-build.yml`, () => ({ id: 42, path: workflowPath })],
    [path => path.includes('/actions/workflows/42/runs?'), () => ({ total_count: runs.length, workflow_runs: structuredClone(runs) })],
    [path => /\/actions\/runs\/\d+\/attempts\/\d+\/jobs\?/.test(path), () => ({ total_count: 1, jobs: [{ id: 90, name: 'Specs', conclusion: 'failure' }] })]
];

function applyRead(issues, runs, path) {
    if (path.startsWith(`${root}/issues?`)) return { data: structuredClone(issues) };
    if (/\/issues\/\d+$/.test(path)) return { data: structuredClone(issues.find(item => path.endsWith(`/${item.number}`))) };
    const match = staticReads(runs).find(([accepts]) => accepts(path));
    assert.ok(match, `fixture forbids unexpected endpoint: GET ${path}`);
    return { data: match[1]() };
}

function fixture(runs = [run()]) {
    const issues = [];
    const calls = [];
    const mutations = [];
    const pages = new Map();
    let fault;
    const transport = async (method, path, body) => {
        calls.push({ method, path });
        const injected = fault?.(method, path, body);
        if (injected) return injected;
        if (pages.has(path)) return structuredClone(pages.get(path));
        if (method === 'GET') return applyRead(issues, runs, path);
        mutations.push({ method, path, body });
        return applyWrite(issues, method, path, body);
    };
    return {
        issues, calls, mutations, pages, runs,
        fault: value => { fault = value; },
        check: (extra = {}) => reconcile({ repo, eventName: 'schedule', event: {}, transport, now, reporter: { id: 99, attempt: 1 }, ...extra })
    };
}

test('first failure, duplicate, recovery and recurrence reuse a body-only tracker', async () => {
    const f = fixture();
    assert.equal((await f.check()).status, 'ok');
    assert.equal(f.issues.length, 1);
    assert.match(f.issues[0].body, /Failing/);
    assert.match(f.issues[0].body, /runs\/99\/artifacts/);
    await f.check();
    assert.equal(f.mutations.length, 1);
    f.issues[0].body = `Human preface\n${f.issues[0].body}\nHuman tail`;
    f.runs.push(run(2, 'success'));
    await f.check();
    assert.match(f.issues[0].body, /Recovered/);
    assert.match(f.issues[0].body, /runs\/2/);
    assert.ok(f.issues[0].body.startsWith('Human preface\n'));
    assert.ok(f.issues[0].body.endsWith('\nHuman tail'));
    assert.equal(f.issues[0].state, 'open');
    await f.check();
    assert.equal(f.mutations.length, 2);
    f.runs.push(run(3, 'success'));
    await f.check();
    assert.equal(f.mutations.length, 2); // Later successes do not repeat the recovery update.
    f.runs.push(run(4));
    await f.check();
    assert.equal(f.issues.length, 1);
    assert.match(f.issues[0].body, /Failing/);
    assert.ok(f.calls.filter(call => /\/issues\/1$/.test(call.path) && call.method === 'GET').length >= 2);
    assert.ok(f.calls.every(call => !call.path.includes('comments')));
});

test('scope filters every event, workflow and repository identity locally', async () => {
    for (const extra of [
        { event: 'push' }, { event: 'pull_request' }, { event: 'workflow_dispatch' }, { event: 'workflow_call' },
        { workflow_id: 7 }, { path: '.github/workflows/spoof.yml', name: '.NET Build & Integration' },
        { head_branch: 'other' }, { head_repository: { full_name: 'fork/chronicle' } },
        { status: 'in_progress' }, { conclusion: 'cancelled' }, { conclusion: 'skipped' }, { conclusion: 'neutral' }
    ]) {
        const f = fixture([run(1, 'failure', extra)]);
        await f.check();
        assert.equal(f.mutations.length, 0, JSON.stringify(extra));
    }
    for (const extra of [{ event: 'push' }, { path: 'wrong' }, { head_branch: 'other' }, { head_repository: { full_name: 'fork/chronicle' } }, { conclusion: 'cancelled' }]) {
        const f = fixture();
        const result = await f.check({ eventName: 'workflow_run', event: { repository: { full_name: repo, default_branch: 'main' }, workflow_run: run(1, 'failure', extra) } });
        assert.equal(result.status, 'ignored');
        assert.equal(f.calls.length, 0);
    }
    const wrongId = fixture();
    assert.equal((await wrongId.check({ eventName: 'workflow_run', event: { repository: { full_name: repo, default_branch: 'main' }, workflow_run: run(1, 'failure', { workflow_id: 7 }) } })).status, 'ignored');
    assert.equal(wrongId.calls.length, 2); // The authoritative workflow id requires read-only resolution.
    assert.equal(wrongId.mutations.length, 0);
});

test('authoritative chronology handles stale events, reruns, ties and duplicates', async () => {
    const f = fixture([run(2, 'success'), run(), run()]);
    await f.check({ eventName: 'workflow_run', event: { repository: { full_name: repo, default_branch: 'main' }, workflow_run: run() } });
    assert.equal(f.mutations.length, 0);
    f.runs.splice(0, f.runs.length, run(2), run(1, 'success', { run_attempt: 2, updated_at: '2026-11-01T11:00:00Z' }));
    await f.check();
    assert.match(f.issues[0].body, /Failing/);
    f.runs.push(run(2, 'success', { run_attempt: 2 }));
    await f.check();
    assert.match(f.issues[0].body, /Recovered/);
    const tied = fixture([run(1, 'success'), run(2, 'failure', { created_at: run().created_at })]);
    await tied.check();
    assert.equal(tied.issues.length, 1);
    const conflict = fixture([run(), run(1, 'success')]);
    assert.equal((await conflict.check()).status, 'failed');
    assert.equal(conflict.mutations.length, 0);
});

test('unknown, old and nondecisive evidence never recovers; closed evidence is respected', async () => {
    const f = fixture();
    await f.check();
    for (const records of [[], [run(2, 'cancelled')], [run(2, 'success', { created_at: '2026-01-01T00:00:00Z' })]]) {
        f.runs.splice(0, f.runs.length, ...records);
        await f.check();
        assert.equal(f.mutations.length, 1);
    }
    f.issues[0].state = 'closed';
    f.runs.splice(0, f.runs.length, run(), run(2));
    await f.check();
    assert.equal(f.issues[0].state, 'closed');
    f.runs.push(run(3, 'success'));
    await f.check();
    assert.equal(f.issues[0].state, 'closed');
    f.runs.push(run(4));
    await f.check();
    assert.equal(f.issues[0].state, 'open');
    assert.equal(f.issues.length, 1);
});

test('pagination uses Link, including empty intermediate pages, for runs/issues/jobs', async () => {
    const f = fixture();
    const runsPath = `${root}/actions/workflows/42/runs?event=schedule&branch=main&status=completed&per_page=100&page=1`;
    const issuesPath = `${root}/issues?state=all&per_page=100&page=1`;
    const jobsPath = `${root}/actions/runs/2/attempts/1/jobs?per_page=100&page=1`;
    const next = path => `<https://api.github.com${path.replace('&page=1', '&page=2')}>; rel="next"`;
    f.pages.set(runsPath, { data: { total_count: 2, workflow_runs: [run()] }, link: next(runsPath) });
    f.pages.set(runsPath.replace('&page=1', '&page=2'), { data: { total_count: 2, workflow_runs: [run(2)] } });
    f.pages.set(issuesPath, { data: [], link: next(issuesPath) });
    f.pages.set(issuesPath.replace('&page=1', '&page=2'), { data: [] });
    f.pages.set(jobsPath, { data: { total_count: 1, jobs: [] }, link: next(jobsPath) });
    f.pages.set(jobsPath.replace('&page=1', '&page=2'), { data: { total_count: 1, jobs: [{ id: 90, name: 'Specs', conclusion: 'failure' }] } });
    const result = await f.check();
    assert.equal(result.status, 'ok');
    assert.equal(result.summary.run.id, 2);
    assert.equal(result.summary.firstFailure.id, 1);
    assert.equal(result.summary.jobs.length, 1);
});

test('ambiguous issues, tampered markers and incomplete history fail before writing', async () => {
    for (const alteration of [
        f => f.issues.push({ number: 1, title: trackerTitle, body: 'human', user: { login: 'person' } }),
        f => f.pages.set(`${root}/actions/workflows/42/runs?event=schedule&branch=main&status=completed&per_page=100&page=1`, { data: { total_count: 2, workflow_runs: [run()] } }),
        f => f.pages.set(`${root}/actions/workflows/42/runs?event=schedule&branch=main&status=completed&per_page=100&page=1`, { data: { total_count: 1001, workflow_runs: [run()] } }),
        f => f.pages.set(`${root}/issues?state=all&per_page=100&page=1`, { data: [], link: '<https://evil.invalid/>; rel="next"' })
    ]) {
        const f = fixture();
        alteration(f);
        assert.equal((await f.check()).status, 'failed');
        assert.equal(f.mutations.length, 0);
    }
    const f = fixture();
    await f.check();
    f.issues.push({ ...f.issues[0], number: 2 });
    assert.equal((await f.check()).status, 'failed');
    f.issues.pop();
    f.issues[0].body = f.issues[0].body.replace('"failing"', '"tampered"');
    assert.equal((await f.check()).status, 'failed');
});

test('required failures and ambiguous writes are sanitized, not retried; jobs failure is degraded', async () => {
    for (const status of [401, 403, 404, 429, 500, 503]) {
        const transport = restTransport({ token: 'fixture-only', fetchImpl: async () => ({ ok: false, status }) });
        const result = await fixture().check({ transport });
        assert.equal(result.status, 'failed');
        assert.match(result.error, new RegExp(String(status)));
    }
    for (const method of ['POST', 'PATCH']) {
        const f = fixture();
        if (method === 'PATCH') { await f.check(); f.runs.push(run(2, 'success')); }
        f.fault((verb) => { if (verb === method) throw new Error('synthetic-secret-DO-NOT-EMIT'); });
        const result = await f.check();
        assert.equal(result.status, 'failed');
        assert.equal(result.failed, 1);
        assert.ok(!JSON.stringify(result).includes('DO-NOT-EMIT'));
        assert.equal(f.calls.filter(call => call.method === method).length, 1);
    }
    const f = fixture();
    f.fault((method, path) => { if (path.includes('/jobs?')) throw new Error('private'); });
    assert.equal((await f.check()).status, 'degraded');
    assert.equal(f.issues.length, 1);
    assert.match(f.issues[0].body, /unavailable/);
});

test('metadata projection rejects payload injection and never forwards URLs/output', async () => {
    const f = fixture([run(1, 'failure', { html_url: 'https://evil.invalid/?secret=value', output: 'private' })]);
    f.fault((method, path) => path.includes('/jobs?') ? { data: { total_count: 2, jobs: [
        { id: 90, name: '@everyone <script> **bad**\nname', conclusion: 'failure', html_url: 'https://evil.invalid', steps: [{ output: 'private' }] },
        { id: 91, name: `ghp_${'x'.repeat(40)}`, conclusion: 'failure' }
    ] } } : undefined);
    const result = await f.check();
    const emitted = JSON.stringify(result) + f.issues[0].body;
    for (const forbidden of ['@everyone', '<script>', '**bad**', 'evil.invalid', 'private', 'ghp_']) assert.ok(!emitted.includes(forbidden), forbidden);
    assert.match(emitted, /github.com\/example\/chronicle\/actions\/runs\/1/);
});

test('serialized invocations re-read state, and workflow supplies the cross-process lock', async () => {
    const workflow = await readFile(new URL('../workflows/nightly-build-health.yml', import.meta.url), 'utf8');
    assert.match(workflow, /group: nightly-build-health\n\s+cancel-in-progress: false\n\s+queue: max/);
    assert.match(workflow, /ref: \$\{\{ github.event.repository.default_branch \}\}/);
    assert.ok(!workflow.includes('workflow_run.head_sha'));
    const f = fixture();
    // This scheduler models GitHub concurrency; the Issues API itself offers no create lock.
    let queue = Promise.resolve();
    const schedule = () => { queue = queue.then(() => f.check()); return queue; };
    await Promise.all([schedule(), schedule()]);
    assert.equal(f.issues.length, 1);
    assert.equal(f.calls.filter(call => call.path.startsWith(`${root}/issues?`)).length, 2);
});

test('tracker on a later page is reused, unrelated humans and PRs are untouched', async () => {
    const f = fixture();
    await f.check();
    const tracker = structuredClone(f.issues[0]);
    f.issues.push({ number: 2, title: 'Unrelated', body: 'Leave me alone', user: { login: 'human' }, state: 'open' });
    f.issues.push({ number: 3, title: trackerTitle, body: 'PR', pull_request: {}, state: 'open' });
    const first = `${root}/issues?state=all&per_page=100&page=1`;
    const second = first.replace('&page=1', '&page=2');
    f.pages.set(first, { data: f.issues.slice(1), link: `<https://api.github.com${second}>; rel="next"` });
    f.pages.set(second, { data: [tracker] });
    f.runs.push(run(2, 'success'));
    assert.equal((await f.check()).status, 'ok');
    assert.equal(f.issues.length, 3);
    assert.equal(f.issues[1].body, 'Leave me alone');
    assert.equal(f.issues[2].body, 'PR');
    assert.match(f.issues[0].body, /Recovered/);
});

test('workflow/repository mismatches, malformed counts/links and prewrite drift fail safely', async () => {
    for (const fault of [
        (method, endpoint) => endpoint === root ? { data: { full_name: 'wrong/repo', default_branch: 'main' } } : undefined,
        (method, endpoint) => endpoint.endsWith('/dotnet-build.yml') ? { data: { id: 42, path: 'spoof' } } : undefined,
        (method, endpoint) => endpoint.includes('/runs?') ? { data: { total_count: 1, workflow_runs: null } } : undefined,
        (method, endpoint) => endpoint.includes('/runs?') ? { data: { total_count: 1, workflow_runs: [run()] }, link: 'malformed' } : undefined
    ]) {
        const f = fixture();
        f.fault(fault);
        assert.equal((await f.check()).status, 'failed');
        assert.equal(f.mutations.length, 0);
    }
    const f = fixture();
    await f.check();
    f.runs.push(run(2, 'success'));
    f.fault((method, endpoint) => endpoint.endsWith('/issues/1') && method === 'GET' ? { data: { ...f.issues[0], body: f.issues[0].body + 'human edit' } } : undefined);
    assert.equal((await f.check()).status, 'failed');
    assert.equal(f.mutations.length, 1);
});

test('built-in REST adapter uses fixture fetch only, with no log/artifact endpoint or unsafe redirect', async () => {
    const f = fixture();
    const result = await f.check({ transport: restTransport({ token: 'fixture-only', fetchImpl: async (url, options) => {
        assert.equal(options.redirect, 'error');
        assert.equal(options.headers.Authorization, 'Bearer fixture-only');
        const parsed = new URL(url);
        assert.equal(parsed.origin, 'https://api.github.com');
        return { ok: true, status: 200, headers: { get: () => null }, json: async () => {
            if (parsed.pathname === root) return { full_name: repo, default_branch: 'main' };
            if (parsed.pathname.endsWith('/dotnet-build.yml')) return { id: 42, path: workflowPath };
            if (parsed.pathname.endsWith('/runs')) return { total_count: 1, workflow_runs: [run()] };
            if (parsed.pathname.endsWith('/jobs')) return { total_count: 0, jobs: [] };
            if (parsed.pathname.endsWith('/issues') && options.method === 'GET') return [];
            if (parsed.pathname.endsWith('/issues') && options.method === 'POST') return { ...JSON.parse(options.body), number: 1, user: { login: 'github-actions[bot]', type: 'Bot' }, state: 'open' };
            assert.fail('unapproved fixture endpoint');
        } };
    } }) });
    assert.equal(result.status, 'ok');
    assert.equal(result.succeeded, 1);
});

test('CLI writes only a sanitized summary and does not discover tokens for inert events', async () => {
    const directory = new URL('../../.ai-work/5497/fixtures/', import.meta.url);
    await mkdir(directory, { recursive: true });
    const eventPath = new URL('event.json', directory);
    await writeFile(eventPath, JSON.stringify({ repository: { full_name: repo, default_branch: 'main' }, workflow_run: run(1, 'failure', { event: 'push' }) }));
    const environment = { GITHUB_REPOSITORY: repo, GITHUB_EVENT_NAME: 'workflow_run', GITHUB_EVENT_PATH: eventPath, RUNNER_TEMP: directory.pathname };
    Object.defineProperty(environment, 'GITHUB_TOKEN', { get() { assert.fail('inert event requested credentials'); } });
    const output = [];
    assert.equal(await main({ environment, output: line => output.push(line) }), 0);
    assert.equal(JSON.parse(await readFile(new URL('nightly-health-summary.json', directory), 'utf8')).status, 'ignored');
    assert.equal(output.length, 1);
    // Required read failure must fail the CLI while still retaining the sanitized diagnostic summary.
    const failure = { ...environment, GITHUB_EVENT_NAME: 'schedule', GITHUB_TOKEN: 'fixture-only' };
    assert.equal(await main({ environment: failure, fetchImpl: async () => { throw new Error('DO-NOT-EMIT'); }, output: line => output.push(line) }), 1);
    const summary = await readFile(new URL('nightly-health-summary.json', directory), 'utf8');
    assert.match(summary, /read-network/);
    assert.ok(!summary.includes('DO-NOT-EMIT') && !summary.includes('fixture-only'));
});

test('streak origin survives the recency window and public job metadata is bounded', async () => {
    const f = fixture([run(1, 'failure', { created_at: '2026-09-01T00:00:00Z' }), run(2)]);
    f.fault((method, endpoint) => endpoint.includes('/jobs?') ? { data: { total_count: 60, jobs: Array.from({ length: 60 }, (_, index) => ({ id: 100 + index, name: 'Specs', conclusion: 'failure' })) } } : undefined);
    const result = await f.check();
    assert.equal(result.summary.firstFailure.id, 1);
    assert.equal(result.summary.jobCount, 60);
    assert.equal(result.summary.jobs.length, 50);
    assert.match(f.issues[0].body, /Showing 50 of 60 jobs/);
});

async function generic(records, { path = workflowPath, titles = [], state = 'active' } = {}) {
    const source = await readFile(new URL('../workflows/workflow-health.yml', import.meta.url), 'utf8');
    const script = source.split('          script: |\n')[1].split('\n').map(line => line.slice(12)).join('\n');
    const writes = [];
    const samples = [];
    const actions = { listRepoWorkflows: 'workflows', listWorkflowRuns: async query => { samples.push(query); return { data: { workflow_runs: records } }; } };
    const issues = { listForRepo: 'issues', create: async body => { writes.push(body); return { data: { html_url: 'https://github.com/example/chronicle/issues/1' } }; } };
    const github = { rest: { repos: { get: async () => ({ data: { default_branch: 'main' } }) }, actions, issues },
        paginate: async endpoint => endpoint === 'workflows' ? [{ id: 42, name: 'Fixture', path, state }] : titles.map(title => ({ title })) };
    await vm.runInNewContext(`(async () => {${script}\n})()`, { github, context: { repo: { owner: 'example', repo: 'chronicle' } }, core: { info() {} }, Date: class extends Date { static now() { return now; } } });
    return { writes, samples };
}

test('actual unchanged generic script retains THREE failures, mixed events and no recovery', async () => {
    assert.equal((await generic([run(), run(2)])).writes.length, 0);
    assert.equal((await generic([run(), run(2), run(3)])).writes.length, 1);
    assert.equal((await generic([run(4, 'success'), run(), run(2), run(3)])).writes.length, 0);
    const mixed = await generic([run(4, 'cancelled'), run(5, 'skipped'), run(1, 'failure', { event: 'push' }), run(2, 'failure', { event: 'pull_request' }), run(3, 'failure', { event: 'workflow_dispatch' })]);
    assert.equal(mixed.writes.length, 1);
    assert.equal(mixed.samples[0].per_page, 20);
    assert.equal(mixed.samples[0].branch, 'main');
    assert.equal(mixed.samples[0].event, undefined);
    assert.equal((await generic([run(), run(2), run(3)], { titles: ['CI: Fixture failing repeatedly'] })).writes.length, 0);
    assert.equal((await generic([run(), run(2), run(3)], { state: 'disabled_manually' })).samples.length, 0);
    assert.equal((await generic([run(), run(2), run(3)], { path: '.github/workflows/workflow-health.yml' })).samples.length, 0);
    assert.equal((await generic([run(), run(2), run(3)], { path: '.github/workflows/publish.yml' })).samples[0].branch, undefined);
    const boundary = new Date(now - 30 * 86400000).toISOString();
    assert.equal((await generic([run(), run(2), run(3, 'failure', { created_at: boundary })])).writes.length, 1);
    assert.equal((await generic([run(), run(2), run(3, 'failure', { created_at: new Date(Date.parse(boundary) - 1).toISOString() })])).writes.length, 0);
});
