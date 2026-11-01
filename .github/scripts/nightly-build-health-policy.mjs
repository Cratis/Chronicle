// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const workflowPath = '.github/workflows/dotnet-build.yml';
export const trackerTitle = 'CI: scheduled .NET nightly health';
const start = '<!-- nightly-build-health:v1 ';
const end = '<!-- /nightly-build-health:v1 -->';
const verdicts = new Set(['success', 'failure']);
const conclusions = new Set(['success', 'failure', 'cancelled', 'skipped', 'neutral', 'timed_out', 'action_required', 'stale', 'startup_failure']);

export function requireEvidence(condition, code = 'invalid-evidence') {
    if (!condition) throw new Error(code);
}

export function positive(value) {
    return Number.isSafeInteger(value) && value > 0;
}

export function safeName(value) {
    if (typeof value !== 'string' || /(?:gh[pousr]_|github_pat_|bearer\s|token[=:]|[A-Za-z0-9_+\/-]{32,})/i.test(value)) return '[omitted]';
    return value.replace(/[^a-zA-Z0-9 ._()/:-]/g, ' ').replace(/\s+/g, ' ').trim().slice(0, 100) || '[omitted]';
}

export function scopeFor(repo, repository, workflow) {
    requireEvidence(/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repo));
    requireEvidence(repository?.full_name === repo && typeof repository.default_branch === 'string');
    const branch = repository.default_branch;
    requireEvidence(/^[A-Za-z0-9._/-]{1,160}$/.test(branch) && safeName(branch) !== '[omitted]');
    requireEvidence(workflow?.path === workflowPath && positive(workflow.id), 'workflow-identity');
    return { repo, branch, workflowId: workflow.id, key: JSON.stringify([repo, workflowPath, branch]) };
}

function sameIdentity(run, scope) {
    return run?.path === workflowPath && run.workflow_id === scope.workflowId && run.event === 'schedule';
}

function sameRepositoryBranch(run, scope) {
    return run.head_branch === scope.branch && run.head_repository?.full_name === scope.repo;
}

export function eligible(run, scope) {
    return sameIdentity(run, scope) && sameRepositoryBranch(run, scope) && run.status === 'completed' && verdicts.has(run.conclusion);
}

export function cursorFor(run) {
    requireEvidence(positive(run.id) && positive(run.run_number) && positive(run.run_attempt));
    requireEvidence(typeof run.created_at === 'string' && Number.isFinite(Date.parse(run.created_at)));
    return { created: new Date(run.created_at).toISOString(), number: run.run_number, id: run.id, attempt: run.run_attempt };
}

export function compare(left, right) {
    return Date.parse(left.created) - Date.parse(right.created) || left.number - right.number || left.id - right.id || left.attempt - right.attempt;
}

function validateRun(run, now) {
    requireEvidence(Date.parse(cursorFor(run).created) <= now, 'future-evidence');
    requireEvidence(typeof run.head_sha === 'string' && /^[a-f0-9]{40}$/i.test(run.head_sha));
}

// True when `run` is a newer attempt of an already-seen run id; throws on contradictory evidence.
function supersedes(run, previous) {
    const cursor = cursorFor(run);
    const prior = cursorFor(previous);
    const same = cursor.created === prior.created && cursor.number === prior.number && run.head_sha === previous.head_sha;
    requireEvidence(same, 'conflicting-run');
    requireEvidence(cursor.attempt !== prior.attempt || run.conclusion === previous.conclusion, 'conflicting-run');
    return cursor.attempt > prior.attempt;
}

export function historyFor(records, scope, now) {
    requireEvidence(Number.isFinite(now));
    const byId = new Map();
    for (const run of records.filter(item => eligible(item, scope))) {
        validateRun(run, now);
        const previous = byId.get(run.id);
        if (!previous || supersedes(run, previous)) byId.set(run.id, run);
    }
    const history = [...byId.values()].sort((a, b) => compare(cursorFor(b), cursorFor(a)));
    // Recency controls whether to act, not where an ongoing failure streak began.
    if (!history.length || Date.parse(history[0].created_at) < now - 30 * 86400000) return [];
    return history;
}

function isCanonicalTime(value) {
    return typeof value === 'string' && Number.isFinite(Date.parse(value)) && new Date(value).toISOString() === value;
}

function validCursor(cursor) {
    if (!cursor || Object.keys(cursor).sort().join(',') !== 'attempt,created,id,number') return false;
    return [cursor.id, cursor.number, cursor.attempt].every(positive) && isCanonicalTime(cursor.created);
}

function markerBounds(body) {
    const from = body.indexOf(start);
    const until = body.indexOf(end);
    const single = body.indexOf(start, from + start.length) < 0 && body.indexOf(end, until + end.length) < 0;
    requireEvidence(from >= 0 && until > from && single, 'tracker-marker');
    const lineEnd = body.indexOf(' -->', from);
    requireEvidence(lineEnd > from && lineEnd < until, 'tracker-marker');
    return { from, until, lineEnd };
}

function parseState(text) {
    try { return JSON.parse(text); } catch { throw new Error('tracker-state'); }
}

function validState(state, scope) {
    if (Object.keys(state ?? {}).sort().join(',') !== 'cursor,firstFailure,key,status') return false;
    if (state.key !== scope.key || !['failing', 'recovered'].includes(state.status)) return false;
    return validCursor(state.cursor) && validCursor(state.firstFailure) && compare(state.cursor, state.firstFailure) >= 0;
}

export function managedState(issue, scope) {
    requireEvidence(typeof issue.body === 'string', 'tracker-body');
    const { from, until, lineEnd } = markerBounds(issue.body);
    const state = parseState(issue.body.slice(from + start.length, lineEnd));
    requireEvidence(validState(state, scope), 'tracker-state');
    return { state, from, until: until + end.length };
}

const isTrackerCandidate = issue => !issue.pull_request && (issue.title === trackerTitle || issue.body?.includes(start));
const isBotOwned = issue => issue.user?.login === 'github-actions[bot]' && issue.user?.type === 'Bot';

export function selectTracker(issues, scope) {
    const candidates = issues.filter(isTrackerCandidate);
    requireEvidence(candidates.length <= 1, 'ambiguous-trackers');
    if (!candidates.length) return undefined;
    const issue = candidates[0];
    requireEvidence(positive(issue.number) && isBotOwned(issue), 'tracker-owner');
    requireEvidence(['open', 'closed'].includes(issue.state), 'tracker-state');
    managedState(issue, scope);
    return issue;
}

function unchanged(latest, prior, issue) {
    if (prior && compare(cursorFor(latest), prior.cursor) <= 0) return true;
    if (latest.conclusion === 'success') return !prior || prior.status === 'recovered';
    return issue?.state === 'closed' && prior.status !== 'recovered';
}

function failureStart(history, prior) {
    const boundary = history.findIndex(run => run.conclusion === 'success');
    const streak = boundary < 0 ? history : history.slice(0, boundary);
    const success = history[streak.length];
    if (prior?.status === 'failing') {
        if (!success || compare(cursorFor(success), prior.cursor) < 0) return prior.firstFailure;
    }
    return cursorFor(streak.at(-1));
}

export function transition(history, issue, scope) {
    const latest = history[0];
    if (!latest) return undefined; // Missing/expired evidence is unknown, never recovery.
    const prior = issue ? managedState(issue, scope).state : undefined;
    if (unchanged(latest, prior, issue)) return undefined;
    const failing = latest.conclusion === 'failure';
    return { key: scope.key, status: failing ? 'failing' : 'recovered', cursor: cursorFor(latest),
        firstFailure: failing ? failureStart(history, prior) : prior.firstFailure };
}

export function runMetadata(run, scope) {
    const cursor = cursorFor(run);
    return { id: cursor.id, attempt: cursor.attempt, number: cursor.number, created: cursor.created,
        sha: run.head_sha, status: 'completed', conclusion: run.conclusion, event: 'schedule', branch: scope.branch,
        url: `https://github.com/${scope.repo}/actions/runs/${cursor.id}/attempts/${cursor.attempt}` };
}

export function jobsMetadata(jobs, scope, runId) {
    for (const job of jobs) requireEvidence(positive(job.id) && (job.conclusion === null || conclusions.has(job.conclusion)), 'jobs-evidence');
    // Bound public evidence size; the source run links the complete job list, never raw output.
    return jobs.slice(0, 50).map(job => ({ name: safeName(job.name), conclusion: job.conclusion,
        url: `https://github.com/${scope.repo}/actions/runs/${runId}/job/${job.id}` }));
}

export function renderBody(state, summary, issue, scope) {
    const section = [
        `${start}${JSON.stringify(state)} -->`,
        `## Scheduled .NET nightly: ${state.status === 'failing' ? 'Failing' : 'Recovered'}`,
        '', `Workflow: \`${workflowPath}\` on \`${scope.branch}\`.`,
        `First failure: https://github.com/${scope.repo}/actions/runs/${state.firstFailure.id}`,
        `Latest scheduled verdict: ${summary.run.url}`,
        `Reporter and retained metadata: ${summary.reporterUrl}/artifacts`,
        `Diagnostics: ${summary.diagnostics}. Showing ${summary.jobs.length} of ${summary.jobCount} jobs.`,
        ...summary.jobs.map(job => `- ${job.name}: ${job.conclusion ?? 'unknown'} (${job.url})`),
        '', 'This tracker stays open on recovery. Only scheduled default-branch runs affect its managed status.', end
    ].join('\n');
    if (!issue) return section;
    const { from, until } = managedState(issue, scope);
    return issue.body.slice(0, from) + section + issue.body.slice(until);
}
