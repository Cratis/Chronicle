// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, writeFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
import { workflowPath, trackerTitle, requireEvidence, positive, scopeFor, eligible, historyFor,
    selectTracker, validateTracker, transition, runMetadata, jobsMetadata, renderBody } from './nightly-build-health-policy.mjs';

class ReportingError extends Error {}

const statusLabel = status => (Number.isInteger(status) && status >= 100 && status <= 599 ? status : 'unknown');

async function responseData(response, operation) {
    if (!response.ok) throw new ReportingError(`${operation}-${statusLabel(response.status)}`);
    try { return { data: await response.json(), link: response.headers.get('link') }; }
    catch { throw new ReportingError('response-shape'); }
}

// No environment discovery or networking occurs on import. Tests inject a deny-by-default transport.
function requestInit(method, token, body) {
    const headers = { Authorization: `Bearer ${token}`, Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' };
    const init = { method, redirect: 'error', signal: AbortSignal.timeout(30000), headers };
    if (body) {
        headers['Content-Type'] = 'application/json';
        init.body = JSON.stringify(body);
    }
    return init;
}

async function send(fetchImpl, token, [method, endpoint, body]) {
    requireEvidence(endpoint.startsWith('/repos/') && !endpoint.includes('://'));
    const operation = method === 'GET' ? 'read' : 'write';
    let response;
    try { response = await fetchImpl(`https://api.github.com${endpoint}`, requestInit(method, token, body)); }
    catch { throw new ReportingError(`${operation}-network`); }
    return responseData(response, operation);
}

export function restTransport({ token, fetchImpl = globalThis.fetch }) {
    return async (method, endpoint, body) => {
        requireEvidence(typeof token === 'string' && token.length > 0, 'missing-token');
        return send(fetchImpl, token, [method, endpoint, body]);
    };
}

const isRecord = item => Boolean(item) && typeof item === 'object' && !Array.isArray(item);

function pageTotal(data, field, previousTotal) {
    const total = data.total_count;
    requireEvidence(Number.isSafeInteger(total) && total >= 0, 'pagination-count');
    requireEvidence(previousTotal === undefined || previousTotal === total, 'pagination-count');
    if (field === 'workflow_runs') requireEvidence(total <= 1000, 'history-cap');
    return total;
}

function pageData(data, field, previousTotal) {
    const items = field ? data?.[field] : data;
    requireEvidence(Array.isArray(items), 'pagination-shape');
    requireEvidence(items.every(isRecord));
    return field ? { items, total: pageTotal(data, field, previousTotal) } : { items };
}

const linkEntries = link => (link ? link.split(',').map(entry => entry.trim()) : []);
const validLinkEntry = entry => /^<https:\/\/[^>]+>; rel="(?:next|prev|first|last)"$/.test(entry);

function nextLink(link) {
    requireEvidence(link === undefined || link === null || typeof link === 'string');
    const entries = linkEntries(link);
    requireEvidence(entries.every(validLinkEntry), 'pagination-link');
    const next = entries.filter(entry => entry.endsWith('rel="next"'));
    requireEvidence(next.length <= 1, 'pagination-link');
    return next[0];
}

function nextPage(link, initial, page) {
    const entry = nextLink(link);
    if (!entry) return undefined;
    const url = new URL(entry.slice(1, entry.indexOf('>')));
    const expected = new URL(initial);
    expected.searchParams.set('page', String(page + 1));
    const comparable = new URL(url);
    comparable.searchParams.sort();
    expected.searchParams.sort();
    requireEvidence(comparable.href === expected.href, 'pagination-link');
    requireEvidence(!url.username && !url.password && !url.hash, 'pagination-link');
    return url;
}

async function paginate(transport, endpoint, field) {
    const initial = new URL(`https://api.github.com${endpoint}`);
    let current = initial;
    let total;
    const records = [];
    for (let page = 1; page <= 100; page++) {
        const response = await transport('GET', current.pathname + current.search);
        const batch = pageData(response.data, field, total);
        total = batch.total;
        records.push(...batch.items);
        current = nextPage(response.link, initial, page);
        if (!current) {
            requireEvidence(total === undefined || records.length >= total, 'incomplete-history');
            return records;
        }
    }
    throw new ReportingError('pagination-cap');
}

function runNotificationScope(repo, event) {
    const run = event?.workflow_run;
    if (event.repository?.full_name !== repo || typeof event.repository.default_branch !== 'string') return false;
    return eligible(run, { repo, branch: event.repository.default_branch, workflowId: run?.workflow_id }) && positive(run.workflow_id);
}

function notificationScope(repo, eventName, event) {
    if (eventName === 'workflow_run') return runNotificationScope(repo, event);
    return ['schedule', 'workflow_dispatch'].includes(eventName);
}

async function incidentSummary({ transport, root, scope }, latest, state, reporter) {
    requireEvidence(positive(reporter?.id) && positive(reporter?.attempt), 'reporter-identity');
    let jobs = [];
    let diagnostics = 'available';
    let jobCount = 0;
    try {
        const records = await paginate(transport, `${root}/actions/runs/${latest.id}/attempts/${latest.run_attempt}/jobs?per_page=100&page=1`, 'jobs');
        jobCount = records.length;
        jobs = jobsMetadata(records, scope, latest.id);
    } catch { diagnostics = 'unavailable'; }
    return { workflow: { path: workflowPath, id: scope.workflowId }, run: runMetadata(latest, scope),
        firstFailure: state.firstFailure, jobs, jobCount, diagnostics,
        reporterUrl: `https://github.com/${scope.repo}/actions/runs/${reporter.id}`, reporterAttempt: reporter.attempt };
}

function validateWrite(written, issue, body, reopen) {
    requireEvidence(written && positive(written.number) && written.body === body, 'write-outcome');
    requireEvidence(!issue || written.number === issue.number, 'write-outcome');
    requireEvidence(written.state === (reopen ? 'open' : issue?.state ?? 'open'), 'write-outcome');
}

async function assertNoDrift({ transport, root, scope }, issue) {
    // Re-read under the workflow lock, immediately before PATCH. Drift never permits overwriting.
    const { data: current } = await transport('GET', `${root}/issues/${issue.number}`);
    validateTracker(current, scope);
    const same = current.number === issue.number && current.body === issue.body && current.state === issue.state;
    requireEvidence(same, 'tracker-drift');
}

function writeRequest(root, issue, body, reopen) {
    if (!issue) return ['POST', `${root}/issues`, { title: trackerTitle, body }];
    return ['PATCH', `${root}/issues/${issue.number}`, { body, ...(reopen ? { state: 'open' } : {}) }];
}

async function writeTracker(context, issue, state, result) {
    const { transport, root, scope } = context;
    if (issue) await assertNoDrift(context, issue);
    const body = renderBody(state, result.summary, issue, scope);
    const reopen = issue?.state === 'closed' && state.status === 'failing';
    result.attempted++;
    const { data: written } = await transport(...writeRequest(root, issue, body, reopen));
    validateWrite(written, issue, body, reopen);
    validateTracker(written, scope);
    result.succeeded++;
    result.summary.incidentUrl = `https://github.com/${scope.repo}/issues/${written.number}`;
}

async function reconcileEligible({ repo, eventName, event, transport, now, reporter }, result) {
    requireEvidence(/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repo));
    const root = `/repos/${repo}`;
    const { data: repository } = await transport('GET', root);
    const { data: workflow } = await transport('GET', `${root}/actions/workflows/dotnet-build.yml`);
    const scope = scopeFor(repo, repository, workflow);
    if (eventName === 'workflow_run' && !eligible(event.workflow_run, scope)) return;
    const runs = await paginate(transport, `${root}/actions/workflows/${scope.workflowId}/runs?event=schedule&branch=${encodeURIComponent(scope.branch)}&status=completed&per_page=100&page=1`, 'workflow_runs');
    const history = historyFor(runs, scope, now);
    const issue = selectTracker(await paginate(transport, `${root}/issues?state=all&per_page=100&page=1`), scope);
    const state = transition(history, issue, scope);
    result.status = 'ok';
    if (!state) return;
    const context = { transport, root, scope };
    result.summary = await incidentSummary(context, history[0], state, reporter);
    if (result.summary.diagnostics === 'unavailable') result.status = 'degraded';
    await writeTracker(context, issue, state, result);
}

export async function reconcile(options) {
    const result = { status: 'ignored', attempted: 0, succeeded: 0, failed: 0 };
    try {
        if (notificationScope(options.repo, options.eventName, options.event)) await reconcileEligible(options, result);
    } catch (error) {
        result.status = 'failed';
        result.failed = result.attempted - result.succeeded;
        result.error = error instanceof ReportingError ? error.message : 'reporting-failed';
    }
    return result;
}

async function runFromEnvironment(environment, fetchImpl) {
    const event = JSON.parse(await readFile(environment.GITHUB_EVENT_PATH, 'utf8'));
    // Reject unrelated deliveries before discovering credentials or making even read requests.
    if (!notificationScope(environment.GITHUB_REPOSITORY, environment.GITHUB_EVENT_NAME, event)) {
        return { status: 'ignored', attempted: 0, succeeded: 0, failed: 0 };
    }
    return reconcile({ repo: environment.GITHUB_REPOSITORY, eventName: environment.GITHUB_EVENT_NAME, event,
        transport: restTransport({ token: environment.GITHUB_TOKEN, fetchImpl }), now: Date.now(),
        reporter: { id: Number(environment.GITHUB_RUN_ID), attempt: Number(environment.GITHUB_RUN_ATTEMPT) } });
}

export async function main({ environment = process.env, fetchImpl = globalThis.fetch, output = console.log } = {}) {
    let result;
    try { result = await runFromEnvironment(environment, fetchImpl); }
    catch { result = { status: 'failed', attempted: 0, succeeded: 0, failed: 0, error: 'reporter-input' }; }
    try {
        await writeFile(path.join(environment.RUNNER_TEMP, 'nightly-health-summary.json'), `${JSON.stringify(result, null, 2)}\n`, { mode: 0o600 });
    } catch { result.status = 'failed'; result.error = 'summary-write'; }
    output(JSON.stringify({ status: result.status, attempted: result.attempted, succeeded: result.succeeded, failed: result.failed, ...(result.error ? { error: result.error } : {}) }));
    return ['failed', 'degraded'].includes(result.status) ? 1 : 0;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) process.exitCode = await main();
