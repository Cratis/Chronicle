// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { parseJobs, intervals, peak, percentile, report, UnknownReport } from './run-concurrency-report.mjs';

const base = Date.parse('2026-01-01T00:00:00Z');
const stamp = seconds => new Date(base + seconds * 1000).toISOString();
const job = (name, [created, start, end], extra = {}) => ({
    name, labels: ['ubuntu-latest'], conclusion: 'success',
    created_at: stamp(created), started_at: stamp(start), completed_at: stamp(end), ...extra,
});
const page = (jobs, total = jobs.length) => JSON.stringify({ total_count: total, jobs });

test('a completion and a start at the same instant do not overlap', () => {
    assert.equal(peak(intervals([job('a', [0, 0, 10]), job('b', [0, 10, 20])])), 1);
});

test('a chain of dependent jobs peaks at one', () => {
    assert.equal(peak(intervals([job('a', [0, 0, 5]), job('b', [5, 6, 9]), job('c', [9, 9, 12])])), 1);
});

test('fifty independent jobs peak at fifty', () => {
    const jobs = Array.from({ length: 50 }, (_, index) => job(`j${index}`, [0, 1, 100]));
    assert.equal(peak(intervals(jobs)), 50);
});

test('skipped jobs hold no slot and need no timestamps', () => {
    const skipped = { name: 'skipped', labels: ['ubuntu-latest'], conclusion: 'skipped', started_at: null };
    assert.equal(peak(intervals([job('a', [0, 0, 10]), skipped])), 1);
});

test('a missing timestamp is UNKNOWN, not zero', () => {
    assert.throws(() => intervals([job('a', [0, 0, 10], { completed_at: null })]), UnknownReport);
});

test('an unclassifiable runner label is UNKNOWN', () => {
    assert.throws(() => intervals([job('a', [0, 0, 10], { labels: ['self-hosted'] })]), /unclassifiable runner/);
    assert.throws(() => intervals([job('a', [0, 0, 10], { labels: [] })]), UnknownReport);
});

test('truncated pagination is UNKNOWN', () => {
    assert.throws(() => parseJobs(page([job('a', [0, 0, 10])], 2)), /truncated/);
});

test('concatenated paginate pages are read as one job list, braces inside strings included', () => {
    const text = `${page([job('a', [0, 0, 10])], 2)}\n${page([job('b } "tricky', [0, 0, 10])], 2)}\n`;
    assert.equal(parseJobs(text).length, 2);
});

test('input that is not a complete JSON page is UNKNOWN', () => {
    assert.throws(() => parseJobs('{"total_count": 1, "jobs": ['), UnknownReport);
    assert.throws(() => parseJobs(''), UnknownReport);
});

test('percentiles use nearest rank', () => {
    assert.equal(percentile([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], 0.5), 5);
    assert.equal(percentile([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], 0.9), 9);
    assert.equal(percentile([], 0.5), null);
});

test('the report splits integration from ordinary jobs and measures created-to-started', () => {
    const result = report([
        job('specs-other (1)', [0, 10, 100]),
        job('client-integration-specs (a)', [0, 20, 50]),
        job('neo4j (community)', [0, 30, 60]),
    ]);
    assert.equal(result.all.peak, 3);
    assert.equal(result.integration.peak, 2);
    assert.equal(result.integration.jobs, 2);
    assert.equal(result.ordinary.createdToStartedP90Seconds, 10);
    assert.equal(result.wallSeconds, 100);
});

test('a report over no executed jobs is UNKNOWN', () => {
    assert.throws(() => report([{ name: 's', labels: ['ubuntu-latest'], conclusion: 'skipped' }]), /no executed jobs/);
});
