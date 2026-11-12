// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { CORE_PROJECT, CORE_PARTS, loadPartition, partitionUnits } from './core-spec-partition.mjs';
import { ORDINARY_SHARDS, planShards } from './spec-shards.mjs';
import { balanceJobs, balancePrefixes, slowest } from './spec-shard-balancing.mjs';
import { wallRecords, measuredCore, candidatePlans, shardOverhead, main } from './refresh-spec-shard-timings.mjs';

const units = ['A.', 'B.', 'C.', 'D.'].map(prefix => ({ prefix, weight: 0.25 }));
const records = [80, 120, 103, 102].map((seconds, index) => ({ project: `${CORE_PROJECT}#core-part-${index + 1}-of-4`, seconds }));
const log = records => records.map(record => `Spec project completed: ${record.project}; ${record.seconds} seconds including validation`).join('\n');

test('wall refresh reads explicit process intervals, not timestamp gaps or test durations', () => {
    assert.deepEqual(wallRecords([`Duration: 999s\n${log(records)}\nDone`]), records);
    assert.throws(() => wallRecords(['']), /Missing/);
    assert.throws(() => wallRecords([log([...records, records[0]])]), /duplicate/);
    assert.throws(() => wallRecords([log([{ project: 'A', seconds: 0 }])]), /Invalid/);
});

test('calibration subtracts fixed overhead before scaling each old part and conserves variable time', () => {
    const result = measuredCore(records, units, 55);
    assert.equal(result.model.variableSeconds, 185);
    assert.deepEqual(result.model.samples, records);
    assert.deepEqual(result.units.map(unit => unit.weight), [25 / 185, 65 / 185, 48 / 185, 47 / 185]);
    assert.deepEqual(measuredCore([...records].reverse(), [...units].reverse(), 55).units, result.units);
    for (const broken of [[], records.slice(1), [...records, records[0]]]) {
        assert.throws(() => measuredCore(broken, units, 55), /Incomplete/);
    }
    assert.throws(() => measuredCore(records.map(record => ({ ...record, seconds: 54 })), units, 55), /fixed overhead/);
    const partition = partitionUnits(units);
    assert.deepEqual(balancePrefixes(units, CORE_PARTS), partition);
});

test('spreading Core is chosen when normal LPT stacking has a worse critical path', () => {
    const jobs = [197, 124, 199, 126, 49, 152, 59, 66].map((seconds, index) => ({ project: String(index), seconds, core: index < 2 }));
    const plan = balanceJobs(jobs);
    assert.equal(slowest(plan), 325); // Ordinary LPT stacks both Core parts: 324 / 321 / 327.
    assert.ok(plan.every(shard => shard.projects.filter(id => Number(id) < 2).length <= 1));
    assert.deepEqual(balanceJobs([...jobs].reverse()), plan);
    assert.deepEqual(plan.flatMap(shard => shard.projects).sort(), jobs.map(job => job.project).sort());
});

test('Core spreading is not forced when stacking is faster', () => {
    const jobs = [100, 95, 90, 90, 10].map((seconds, index) => ({ project: String(index), seconds, core: index === 2 || index === 3 }));
    assert.equal(slowest(balanceJobs(jobs)), 180); // Core-first seeding would take 185.
});

test('train 8 model selects two parts; all larger counts lose even with perfect placement', async () => {
    const timings = JSON.parse(await readFile(new URL('./spec-shard-timings.json', import.meta.url), 'utf8'));
    const partition = await loadPartition();
    const candidates = candidatePlans(partition.units, timings.measurements, timings.core);
    assert.equal(candidates[0].count, CORE_PARTS);
    assert.equal(CORE_PARTS, 2);
    const best = slowest(candidates[0].shards);
    const ordinary = timings.measurements.reduce((sum, record) => sum + record.seconds, 0);
    const threePartLowerBound = (ordinary + timings.core.variableSeconds + 3 * timings.core.fixedSeconds) / ORDINARY_SHARDS;
    assert.ok(threePartLowerBound > best);
    const projects = [...timings.measurements.map(record => record.project), CORE_PROJECT];
    const plan = planShards(projects, timings, partitionUnits(partition.units));
    assert.ok(Math.abs(slowest(plan) - best - timings.shardOverheadSeconds) < 0.001);
    assert.equal(timings.measurements.length, 24);
    assert.equal(timings.core.samples.length, 4);
    assert.deepEqual(timings.samples.map(sample => sample.run), Array(3).fill('43190307308'));
    for (const core of [undefined, { fixedSeconds: -1, variableSeconds: 1 }, { fixedSeconds: 55, variableSeconds: NaN }]) {
        assert.throws(() => planShards(projects, { ...timings, core }), /Core wall-time model/);
    }
    assert.throws(() => planShards(projects, { ...timings, shardOverheadSeconds: -1 }), /shard overhead/);
});

test('shard overhead is separate from per-project intervals and requires successful step timings', () => {
    const logs = [log([{ project: 'A', seconds: 8 }]), log([{ project: 'B', seconds: 6 }])];
    const jobs = [1, 2].map(index => ({ name: `specs-other (${index})`, steps: [{ name: 'Run tests with coverage',
        conclusion: 'success', startedAt: '2026-11-12T00:00:00Z', completedAt: '2026-11-12T00:00:10Z' }] }));
    assert.equal(shardOverhead(jobs, logs), 3);
    assert.throws(() => shardOverhead([], logs), /Missing/);
    assert.throws(() => shardOverhead(jobs, [log([{ project: 'A', seconds: 20 }])]), /overhead/);
});

test('the refresh accepts one log per ordinary shard and rejects too few or too many', async () => {
    const meta = ['1', 'previous.json', 'jobs.json'];
    const logs = count => Array.from({ length: count }, (_, index) => `shard-${index + 1}.log`);
    await assert.rejects(main([...meta, ...logs(1)]), /Usage/);
    await assert.rejects(main([...meta, ...logs(ORDINARY_SHARDS + 1)]), /Usage/);
    // A valid count passes the usage check and fails only when it reads the (missing) files.
    await assert.rejects(main([...meta, ...logs(ORDINARY_SHARDS)]), error => !/Usage/.test(error.message));
    await assert.rejects(main([...meta, ...logs(3)]), error => !/Usage/.test(error.message));
});
