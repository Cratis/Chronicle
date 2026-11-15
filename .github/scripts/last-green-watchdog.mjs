// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { restTransport, paginate } from './nightly-build-health.mjs';
import { trackerTitle, workflowPath } from './nightly-build-health-policy.mjs';

export const marker = '<!-- last-green-watchdog:v1 -->';
const STALE_HOURS = 36;
export const isBot = value => value?.user?.login === 'github-actions[bot]' && value.user.type === 'Bot';
export const safeText = value => String(value).replace(/[^a-zA-Z0-9_. /():-]/g, '').slice(0, 240);

export async function findTracker(transport, root) {
    const issues = await paginate(transport, `${root}/issues?state=all&creator=github-actions%5Bbot%5D&per_page=100&page=1`);
    const matches = issues.filter(issue => !issue.pull_request && issue.title === trackerTitle && isBot(issue));
    // The scheduled reporter owns issue creation and its strict managed cursor.
    // A cursorless staleness check cannot safely invent that managed block.
    if (!matches.length) throw new Error('tracker-missing');
    if (matches.length > 1) throw new Error('tracker-ambiguous');
    return matches[0];
}

function matchesWorkflow(run) {
    return run.status === 'completed' && run.head_branch === 'main' && run.path === workflowPath;
}

function matchesSource(run, repo) {
    return ['schedule', 'workflow_dispatch'].includes(run.event) && run.head_repository?.full_name?.toLowerCase() === repo.toLowerCase();
}

function newerRunFirst(a, b) {
    return Date.parse(b.created_at) - Date.parse(a.created_at) || b.id - a.id;
}

async function fullRuns(transport, root, repo) {
    const runs = [];
    for (const event of ['schedule', 'workflow_dispatch']) {
        runs.push(...await paginate(transport, `${root}/actions/workflows/dotnet-build.yml/runs?branch=main&status=completed&event=${event}&per_page=100&page=1`, 'workflow_runs'));
    }
    return runs.filter(run => matchesWorkflow(run) && matchesSource(run, repo)).sort(newerRunFirst);
}

const runLine = (repo, run) => run ? `[run ${Number(run.id)}](https://github.com/${repo}/actions/runs/${Number(run.id)}) · ${safeText(run.created_at)} · ${safeText(run.head_sha)}` : 'none';

function greenAge(green, now) {
    if (!green) return null;
    return Math.max(0, Math.floor((now - Date.parse(green.created_at)) / 3600000));
}

function isStale(green, now) {
    if (!green) return true;
    return now - Date.parse(green.created_at) > STALE_HOURS * 3600000;
}

function healthVerdict(stale, red) {
    if (stale) return red ? 'stale+red' : 'stale';
    return red ? 'red' : 'ok';
}

function healthState(runs, now) {
    const latest = runs[0];
    const green = runs.find(run => run.conclusion === 'success');
    const red = latest?.conclusion === 'failure';
    return { latest, green, red, age: greenAge(green, now), verdict: healthVerdict(isStale(green, now), red) };
}

function headerLines(repo, state) {
    return [marker, `## Full-matrix health: ${state.verdict}`, `Latest full-matrix success: ${runLine(repo, state.green)}`, `Age at last update (hours): ${state.age ?? 'unavailable'}`, `Latest full-matrix run: ${runLine(repo, state.latest)}`];
}

async function laneSections(context, state) {
    const lines = [];
    if (state.green) lines.push('Lanes covered by last green:', ...await laneLines(context.transport, context.root, state.green, false));
    if (state.red) lines.push('Failing lanes:', ...await laneLines(context.transport, context.root, state.latest, true));
    return lines;
}

const runId = run => run?.id ?? null;

async function verdictBody(context) {
    const state = healthState(context.runs, context.now);
    const lines = [...headerLines(context.repo, state), ...await laneSections(context, state)];
    lines.push('Failed specs: see the per-run failed-specs comments on this tracker (run and attempt markers).');
    if (state.verdict === 'ok') lines.push('Full-matrix health recovered.');
    return { verdict: state.verdict, lastGreen: runId(state.green), latest: runId(state.latest), body: `${lines.join('\n\n')}\n` };
}

async function laneLines(transport, root, run, failedOnly) {
    const jobs = await paginate(transport, `${root}/actions/runs/${Number(run.id)}/jobs?per_page=100&page=1`, 'jobs');
    const lanes = jobs.filter(job => job.name?.startsWith('integration / '));
    return lanes.filter(job => !failedOnly || job.conclusion === 'failure').slice(0, 100).map(job => `- ${safeText(job.name)}: ${safeText(job.conclusion)}`);
}

// Age is recorded when the health state/run changes, not rewritten on every push.
const withoutAge = body => body.replace(/^Age at last update \(hours\):.*$/m, 'Age at last update (hours):');

function reopenActions(root, issue, verdict) {
    if (verdict === 'ok') return [];
    if (issue.state !== 'closed') return [];
    return [['PATCH', `${root}/issues/${issue.number}`, { state: 'open' }]];
}

function newCommentActions(root, issue, state) {
    if (state.verdict === 'ok') return [];
    return [['POST', `${root}/issues/${issue.number}/comments`, { body: state.body }]];
}

function commentActions(root, issue, comment, state) {
    if (!comment) return newCommentActions(root, issue, state);
    if (withoutAge(comment.body) === withoutAge(state.body)) return [];
    return [['PATCH', `${root}/issues/comments/${comment.id}`, { body: state.body }]];
}

function plannedActions(root, issue, comment, state) {
    return [...reopenActions(root, issue, state.verdict), ...commentActions(root, issue, comment, state)];
}

async function ownedComment(transport, root, issue) {
    const comments = await paginate(transport, `${root}/issues/${issue.number}/comments?per_page=100&page=1`);
    const owned = comments.filter(comment => isBot(comment) && comment.body?.startsWith(marker));
    if (owned.length > 1) throw new Error('ambiguous-watchdog-comments');
    return owned[0];
}

export async function watchdog({ repo, transport, now = Date.now(), dryRun = false }) {
    if (!/^[\w.-]+\/[\w.-]+$/.test(repo)) throw new Error('invalid-repository');
    const root = `/repos/${repo}`;
    const runs = await fullRuns(transport, root, repo);
    const state = await verdictBody({ transport, root, repo, runs, now });
    const issue = await findTracker(transport, root);
    const comment = await ownedComment(transport, root, issue);
    const actions = plannedActions(root, issue, comment, state);
    if (!dryRun) for (const action of actions) await transport(...action);
    return { verdict: state.verdict, lastGreen: state.lastGreen, latest: state.latest, dryRun, plannedWrites: actions.map(([method, endpoint]) => ({ method, endpoint })), summary: state.body };
}

function parseGhResponse(response) {
    const separator = response.match(/\r?\n\r?\n/);
    if (!separator) throw new Error('invalid-gh-response');
    const headers = response.slice(0, separator.index);
    const data = JSON.parse(response.slice(separator.index + separator[0].length));
    return { data, link: headers.match(/^link:\s*(.*)$/im)?.[1].trim() };
}

function ghReadTransport(dryRun) {
    return async (method, endpoint) => {
        if (!dryRun || method !== 'GET') throw new Error('token-required');
        const response = execFileSync('gh', ['api', '--method', 'GET', '--include', endpoint], { encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 });
        return parseGhResponse(response);
    };
}

async function saveSummary(environment, result) {
    if (!environment.RUNNER_TEMP) return;
    const summary = { verdict: result.verdict, lastGreen: result.lastGreen, latest: result.latest };
    await writeFile(path.join(environment.RUNNER_TEMP, 'last-green-watchdog-summary.json'), `${JSON.stringify(summary)}\n`);
}

export async function main({ environment = process.env, argv = process.argv.slice(2), fetchImpl = globalThis.fetch, now = Date.now(), output = console.log } = {}) {
    try {
        if (argv.some(arg => arg !== '--dry-run')) throw new Error('unknown-argument');
        // Local dry-runs may use gh's configured credentials without exposing the token.
        const dryRun = argv.includes('--dry-run');
        const transport = environment.GITHUB_TOKEN ? restTransport({ token: environment.GITHUB_TOKEN, fetchImpl }) : ghReadTransport(dryRun);
        const result = await watchdog({ repo: environment.GITHUB_REPOSITORY ?? 'Cratis/Chronicle', transport, now, dryRun });
        await saveSummary(environment, result);
        output(JSON.stringify(result));
        return 0;
    } catch (error) {
        output(JSON.stringify({ status: 'failed', error: error.message === 'tracker-missing' ? 'tracker-missing' : 'watchdog-reporting-failed' }));
        return 1;
    }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) process.exitCode = await main();
