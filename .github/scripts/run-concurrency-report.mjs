// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Reports how many hosted jobs one workflow run held at once, from saved
// `gh api repos/<owner>/<repo>/actions/runs/<id>/jobs --paginate` JSON. It measures the
// per-run peak the integration slot budget (#4765) bounds; it says nothing about the org-wide queue.
//
//   node .github/scripts/run-concurrency-report.mjs jobs-before.json jobs-after.json
//
// Exit codes: 0 reported, 2 could not report (UNKNOWN): missing timestamps, truncated
// pagination, or a runner label that is not recognisably hosted. Unknown is never a pass.

import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

export const INTEGRATION_FAMILY = /integration|mongodb|neo4j|clustering/i;
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

export function report(jobs) {
    const items = intervals(jobs);
    if (items.length === 0) throw new UnknownReport('UNKNOWN: no executed jobs in the input');
    const integration = items.filter(item => INTEGRATION_FAMILY.test(item.name));
    const ordinary = items.filter(item => !INTEGRATION_FAMILY.test(item.name));
    const wallSeconds = (Math.max(...items.map(item => item.end)) - Math.min(...items.map(item => item.created))) / 1000;
    return { all: summarize(items), integration: summarize(integration), ordinary: summarize(ordinary), wallSeconds };
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
