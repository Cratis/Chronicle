// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Reports how many hosted jobs one workflow run held at once, from saved
// `gh api repos/<owner>/<repo>/actions/runs/<id>/jobs --paginate` JSON. It measures the
// per-run peak the integration slot budget (#4765) bounds; it says nothing about the org-wide queue.
//
//   gh api "repos/Cratis/Chronicle/actions/runs/<id>/attempts/<N>/jobs?per_page=10" --paginate > jobs.json
//   node .github/scripts/run-concurrency-report.mjs jobs.json
//
// Use per_page=10: the jobs API returns 502 at 20 or more. Only the latest attempt in the file is measured.
//
// Exit codes: 0 reported, 2 could not report (UNKNOWN): missing timestamps, truncated
// pagination, or a runner label that is not recognisably hosted. Unknown is never a pass.

import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

export const INTEGRATION_FAMILY = /integration|mongodb|neo4j|clustering/i;
/** Called-workflow jobs are named `<caller job> / <job>`; classify on the leaf so a caller named for integration does not claim its ordinary jobs. */
export const isIntegration = item => INTEGRATION_FAMILY.test(item.name.split(' / ').at(-1));
const HOSTED_LABEL = /^(ubuntu|windows|macos)-/;

export class UnknownReport extends Error {}

/** `gh api --paginate` prints one JSON object per page back to back; split them without assuming newlines. */
export function splitJsonObjects(text) {
    const objects = [];
    const token = /"(?:[^"\\]|\\.)*"|[{}]|[^"{}]+/gy;
    let depth = 0, start = 0, match;
    while ((match = token.exec(text)) !== null) {
        depth += braceDelta(match[0]);
        if (depth < 0) throw new UnknownReport('UNKNOWN: unbalanced JSON');
        if (depth === 0 && match[0] === '}') objects.push(JSON.parse(text.slice(start, token.lastIndex)));
        if (depth === 0) start = token.lastIndex;
    }
    if (depth !== 0 || objects.length === 0) throw new UnknownReport('UNKNOWN: no complete JSON page found');
    return objects;
}

const braceDelta = token => ({ '{': 1, '}': -1 })[token] ?? 0;

export function parseJobs(text) {
    const pages = splitJsonObjects(text);
    const jobs = pages.flatMap(page => {
        if (!Array.isArray(page.jobs)) throw new UnknownReport('UNKNOWN: page without a jobs array');
        return page.jobs;
    });
    const total = pages[0].total_count;
    if (typeof total !== 'number' || jobs.length < total) {
        throw new UnknownReport(`UNKNOWN: truncated pagination (${jobs.length} of ${total} jobs)`);
    }
    return jobs;
}

const time = (job, field) => {
    const value = Date.parse(job[field]);
    if (Number.isNaN(value)) throw new UnknownReport(`UNKNOWN: job "${job.name}" has no valid ${field}`);
    return value;
};

/**
 * A re-run attempt lists jobs carried over from earlier attempts (they keep their old run_attempt and a
 * created_at later than started_at). They did not run in the latest attempt, so only the latest attempt is measured.
 */
export function latestAttempt(jobs) {
    const attempts = jobs.map(job => job.run_attempt).filter(Number.isInteger);
    if (attempts.length === 0) return { jobs, carriedOver: 0 };
    const latest = Math.max(...attempts);
    const current = jobs.filter(job => !Number.isInteger(job.run_attempt) || job.run_attempt === latest);
    return { jobs: current, carriedOver: jobs.length - current.length };
}

/** Skipped jobs never held a slot; every other job needs full timestamps and a hosted runner label. */
export function intervals(jobs) {
    return jobs.filter(job => job.conclusion !== 'skipped').map(job => {
        const labels = job.labels ?? [];
        if (labels.length === 0 || !labels.every(label => HOSTED_LABEL.test(label))) {
            throw new UnknownReport(`UNKNOWN: job "${job.name}" has unclassifiable runner labels [${labels.join(', ')}]`);
        }
        const created = time(job, 'created_at'), start = time(job, 'started_at'), end = time(job, 'completed_at');
        if (end < start || start < created) throw new UnknownReport(`UNKNOWN: job "${job.name}" has out-of-order timestamps`);
        return { name: job.name, created, start, end };
    });
}

/** Peak overlap of half-open [start, end) intervals; a completion at the same instant frees its slot before a start takes one. */
export function peak(items) {
    const events = items.flatMap(item => [[item.start, 1], [item.end, -1]]);
    events.sort((a, b) => a[0] - b[0] || a[1] - b[1]);
    let active = 0, max = 0;
    for (const [, delta] of events) {
        active += delta;
        max = Math.max(max, active);
    }
    return max;
}

/** Nearest-rank percentile in seconds. */
export function percentile(values, fraction) {
    if (values.length === 0) return null;
    const sorted = [...values].sort((a, b) => a - b);
    return sorted[Math.max(0, Math.ceil(fraction * sorted.length) - 1)];
}

export function summarize(items) {
    const waits = items.map(item => (item.start - item.created) / 1000);
    return {
        jobs: items.length,
        peak: peak(items),
        createdToStartedP50Seconds: percentile(waits, 0.5),
        createdToStartedP90Seconds: percentile(waits, 0.9),
    };
}

/** Peak of the intervals clipped to [from, to): the concurrency that falls inside one phase of the run. */
export function peakWithin(items, from, to) {
    const clipped = items
        .map(item => ({ start: Math.max(item.start, from), end: Math.min(item.end, to) }))
        .filter(item => item.start < item.end);
    return peak(clipped);
}

export function report(allJobs) {
    const { jobs, carriedOver } = latestAttempt(allJobs);
    const items = intervals(jobs);
    if (items.length === 0) throw new UnknownReport('UNKNOWN: no executed jobs in the input');
    const integration = items.filter(isIntegration);
    const ordinary = items.filter(item => !isIntegration(item));
    const wallSeconds = (Math.max(...items.map(item => item.end)) - Math.min(...items.map(item => item.created))) / 1000;
    const result = { all: summarize(items), integration: summarize(integration), ordinary: summarize(ordinary), wallSeconds, carriedOverJobsExcluded: carriedOver };
    if (integration.length > 0) {
        const integrationStart = Math.min(...integration.map(item => item.start));
        result.peakBeforeIntegrationStart = peakWithin(items, -Infinity, integrationStart);
        result.peakFromIntegrationStart = peakWithin(items, integrationStart, Infinity);
    }
    return result;
}

export async function main(argv) {
    if (argv.length === 0) {
        console.error('usage: run-concurrency-report.mjs <jobs.json>...');
        return 2;
    }
    for (const file of argv) {
        try {
            const result = report(parseJobs(await readFile(file, 'utf8')));
            console.log(`${file}: created-to-started is created_at to started_at (it includes dependency delay, not only queueing)`);
            console.log(JSON.stringify(result, null, 2));
        } catch (error) {
            if (!(error instanceof UnknownReport)) throw error;
            console.error(`${file}: ${error.message}`);
            return 2;
        }
    }
    return 0;
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) process.exitCode = await main(process.argv.slice(2));
