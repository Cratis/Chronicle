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

async function fullRuns(transport, root, repo) {
    const runs = [];
    for (const event of ['schedule', 'workflow_dispatch']) {
        runs.push(...await paginate(transport, `${root}/actions/workflows/dotnet-build.yml/runs?branch=main&status=completed&event=${event}&per_page=100&page=1`, 'workflow_runs'));
    }
    return runs.filter(run => run.status === 'completed' && run.head_branch === 'main' && run.path === workflowPath && ['schedule', 'workflow_dispatch'].includes(run.event) && run.head_repository?.full_name?.toLowerCase() === repo.toLowerCase())
        .sort((a, b) => Date.parse(b.created_at) - Date.parse(a.created_at) || b.id - a.id);
}

const runLine = (repo, run) => run ? `[run ${Number(run.id)}](https://github.com/${repo}/actions/runs/${Number(run.id)}) · ${safeText(run.created_at)} · ${safeText(run.head_sha)}` : 'none';

async function verdictBody({ transport, root, repo, runs, now }) {
    const latest = runs[0];
    const green = runs.find(run => run.conclusion === 'success');
    const age = green ? Math.max(0, Math.floor((now - Date.parse(green.created_at)) / 3600000)) : null;
    const stale = age === null || now - Date.parse(green.created_at) > STALE_HOURS * 3600000;
    const red = latest?.conclusion === 'failure';
    const verdict = stale ? (red ? 'stale+red' : 'stale') : (red ? 'red' : 'ok');
    const lines = [marker, `## Full-matrix health: ${verdict}`, `Latest full-matrix success: ${runLine(repo, green)}`, `Age at last update (hours): ${age ?? 'unavailable'}`, `Latest full-matrix run: ${runLine(repo, latest)}`];
    if (green) lines.push('Lanes covered by last green:', ...await laneLines(transport, root, green, false));
    if (red) lines.push('Failing lanes:', ...await laneLines(transport, root, latest, true));
    lines.push('Failed specs: see the per-run failed-specs comments on this tracker (run and attempt markers).');
    if (verdict === 'ok') lines.push('Full-matrix health recovered.');
    return { verdict, lastGreen: green?.id ?? null, latest: latest?.id ?? null, body: `${lines.join('\n\n')}\n` };
}

async function laneLines(transport, root, run, failedOnly) {
    const jobs = await paginate(transport, `${root}/actions/runs/${Number(run.id)}/jobs?per_page=100&page=1`, 'jobs');
    const lanes = jobs.filter(job => job.name?.startsWith('integration / '));
    return lanes.filter(job => !failedOnly || job.conclusion === 'failure').slice(0, 100).map(job => `- ${safeText(job.name)}: ${safeText(job.conclusion)}`);
}

// Age is recorded when the health state/run changes, not rewritten on every push.
const withoutAge = body => body.replace(/^Age at last update \(hours\):.*$/m, 'Age at last update (hours):');

function plannedActions(root, issue, comment, state) {
    const actions = [];
    if (state.verdict !== 'ok' && issue.state === 'closed') actions.push(['PATCH', `${root}/issues/${issue.number}`, { state: 'open' }]);
    if (state.verdict === 'ok' && !comment) return actions;
    if (!comment) actions.push(['POST', `${root}/issues/${issue.number}/comments`, { body: state.body }]);
    else if (withoutAge(comment.body) !== withoutAge(state.body)) actions.push(['PATCH', `${root}/issues/comments/${comment.id}`, { body: state.body }]);
    return actions;
}

export async function watchdog({ repo, transport, now = Date.now(), dryRun = false }) {
    if (!/^[\w.-]+\/[\w.-]+$/.test(repo)) throw new Error('invalid-repository');
    const root = `/repos/${repo}`;
    const runs = await fullRuns(transport, root, repo);
    const state = await verdictBody({ transport, root, repo, runs, now });
    const issue = await findTracker(transport, root);
    const comments = await paginate(transport, `${root}/issues/${issue.number}/comments?per_page=100&page=1`);
    const owned = comments.filter(comment => isBot(comment) && comment.body?.startsWith(marker));
    if (owned.length > 1) throw new Error('ambiguous-watchdog-comments');
    const actions = plannedActions(root, issue, owned[0], state);
    if (!dryRun) for (const action of actions) await transport(...action);
    return { verdict: state.verdict, lastGreen: state.lastGreen, latest: state.latest, dryRun, plannedWrites: actions.map(([method, endpoint]) => ({ method, endpoint })), summary: state.body };
}

function ghReadTransport(dryRun) {
    return async (method, endpoint) => {
        if (!dryRun || method !== 'GET') throw new Error('token-required');
        const response = execFileSync('gh', ['api', '--method', 'GET', '--include', endpoint], { encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 });
        const separator = response.match(/\r?\n\r?\n/);
        if (!separator) throw new Error('invalid-gh-response');
        const headers = response.slice(0, separator.index);
        const data = JSON.parse(response.slice(separator.index + separator[0].length));
        return { data, link: headers.match(/^link:\s*(.*)$/im)?.[1].trim() };
    };
}

export async function main({ environment = process.env, argv = process.argv.slice(2), fetchImpl = globalThis.fetch, now = Date.now(), output = console.log } = {}) {
    try {
        if (argv.some(arg => arg !== '--dry-run')) throw new Error('unknown-argument');
        // Local dry-runs may use gh's configured credentials without exposing the token.
        const dryRun = argv.includes('--dry-run');
        const transport = environment.GITHUB_TOKEN ? restTransport({ token: environment.GITHUB_TOKEN, fetchImpl }) : ghReadTransport(dryRun);
        const result = await watchdog({ repo: environment.GITHUB_REPOSITORY ?? 'Cratis/Chronicle', transport, now, dryRun });
        if (environment.RUNNER_TEMP) await writeFile(path.join(environment.RUNNER_TEMP, 'last-green-watchdog-summary.json'), `${JSON.stringify({ verdict: result.verdict, lastGreen: result.lastGreen, latest: result.latest })}\n`);
        output(JSON.stringify(result));
        return 0;
    } catch (error) {
        output(JSON.stringify({ status: 'failed', error: error.message === 'tracker-missing' ? 'tracker-missing' : 'watchdog-reporting-failed' }));
        return 1;
    }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) process.exitCode = await main();
