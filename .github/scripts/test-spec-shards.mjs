// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, readFile, unlink } from 'node:fs/promises';
import path from 'node:path';
import { v3Projects, shippingProjects, ordinaryProjects, validateSourceProjects } from './mixed-specs-selection.mjs';
import { planShards, parseShardArgs, validateTimings, sourcePlan, verifyReceipts, verifyResults, receiptFor } from './spec-shards.mjs';

const ordinary = ordinaryProjects(v3Projects.map(project => ({ project }))).map(x => x.project);
const weights = { version: 1, defaultSeconds: 60, measurements: [] };
const context = { sha: 'head', runId: '123', runAttempt: '1' };
async function fixture() {
    const scratch = path.resolve('.ai-work/5501/fixtures');
    await mkdir(scratch, { recursive: true });
    const root = await mkdtemp(path.join(scratch, 'partition-'));
    for (const project of [...v3Projects, ...shippingProjects]) {
        await mkdir(path.dirname(path.join(root, project)), { recursive: true });
        await writeFile(path.join(root, project), '<Project/>');
    }
    await writeFile(path.join(root, 'Chronicle.slnx'), `<Solution>${v3Projects.filter(p => !p.startsWith('Benchmarks/')).map(p => `<Project Path="${p}"/>`).join('')}</Solution>`);
    return root;
}

test('LPT is deterministic, ordinal on ties, nonempty and exactly once, with positive defaults', () => {
    const plan = planShards(ordinary, weights);
    assert.deepEqual(plan, planShards([...ordinary].reverse(), weights));
    assert.deepEqual(plan.flatMap(s => s.projects).sort(), [...ordinary].sort());
    assert.ok(plan.every(s => s.projects.length > 0));
    assert.equal(plan[0].projects[0], [...ordinary].sort()[0]);
    assert.equal(plan[1].projects[0], [...ordinary].sort()[1]);
    assert.ok(plan.every(s => s.seconds === s.projects.length * 60));
    assert.deepEqual(plan.flatMap(s => s.defaulted).sort(), [...ordinary].sort());
    const future = 'Source/Future.Specs/Future.Specs.csproj';
    assert.equal(planShards([...ordinary, future], weights).flatMap(s => s.projects).filter(p => p === future).length, 1);
    assert.throws(() => planShards([...ordinary, ordinary[0]], weights), /duplicate/i);
    assert.throws(() => planShards([], weights), /two nonempty/i);
    assert.throws(() => planShards([ordinary[0]], weights), /two nonempty/i);
});

test('timings reject bad schema, duplicate, stale, nonpositive and nonfinite weights', () => {
    for (const seconds of [0, -1, NaN, Infinity, '60', null]) {
        assert.throws(() => validateTimings({ ...weights, measurements: [{ project: ordinary[0], seconds }] }, ordinary), /seconds/i);
    }
    for (const invalid of [null, {}, { ...weights, version: 2 }, { ...weights, measurements: {} }, { ...weights, defaultSeconds: 0 }]) {
        assert.throws(() => validateTimings(invalid, ordinary));
    }
    const record = { project: ordinary[0], seconds: 10 };
    assert.throws(() => validateTimings({ ...weights, measurements: [record, record] }, ordinary), /duplicate/i);
    for (const project of [v3Projects.find(p => p.startsWith('Integration/')), shippingProjects[0], '../outside.csproj']) {
        assert.throws(() => validateTimings({ ...weights, measurements: [{ project, seconds: 10 }] }, ordinary), /stale|unknown/i);
    }
});

test('only strict paired two-shard arguments are admitted; no arguments stays compatible', () => {
    assert.equal(parseShardArgs([]), null);
    assert.equal(parseShardArgs(['--shard', '1', '--shards', '2']), 1);
    assert.equal(parseShardArgs(['--shards', '2', '--shard', '2']), 2);
    for (const args of [['--shard', '1'], ['--shards', '2'], ['--shard', '0', '--shards', '2'], ['--shard', '3', '--shards', '2'], ['--shard', '1.0', '--shards', '2'], ['--shard', '1', '--shards', '3'], ['--shard', '1', '--shard', '2'], ['--other', '1', '--shards', '2'], ['--shard', '01', '--shards', '2']]) {
        assert.throws(() => parseShardArgs(args), /shard|Usage/);
    }
});

test('source proof covers actual source including Benchmarks, not just the registry', async () => {
    const root = await fixture();
    const discovered = await validateSourceProjects(root);
    assert.equal(discovered.executable.length, 31);
    assert.equal(discovered.ordinary.length, 24);
    assert.equal(discovered.integration.length, 6);
    assert.equal(discovered.sql.length, 1);
    assert.equal(discovered.shipping.length, 3);
    const plan = await sourcePlan(root, weights);
    assert.deepEqual(plan.flatMap(s => s.projects).sort(), [...ordinary].sort());
    for (const project of ['Source/New.Specs/New.Specs.csproj', 'Benchmarks/New.Specs/New.Specs.csproj', 'Integration/New/New.csproj']) {
        await mkdir(path.dirname(path.join(root, project)), { recursive: true });
        await writeFile(path.join(root, project), '<Project/>');
        await assert.rejects(validateSourceProjects(root), /Unclassified test project/);
        await unlink(path.join(root, project));
    }
    const future = 'Source/New.Specs/New.Specs.csproj';
    await writeFile(path.join(root, future), '<Project/>');
    const classified = await validateSourceProjects(root, [...v3Projects, future]);
    assert.equal(planShards(classified.ordinary, weights).flatMap(s => s.projects).filter(p => p === future).length, 1);
    await unlink(path.join(root, future));
    await unlink(path.join(root, ordinary[0]));
    await assert.rejects(validateSourceProjects(root), /Missing classified project/);
});

test('duplicate solution entries and duplicate classified identities fail closed', async () => {
    const root = await fixture();
    await assert.rejects(validateSourceProjects(root, [...v3Projects, v3Projects[0]]), /duplicate/i);
    const xml = await readFile(path.join(root, 'Chronicle.slnx'), 'utf8');
    await writeFile(path.join(root, 'Chronicle.slnx'), xml.replace('</Solution>', `<Project Path="${ordinary.find(p => p.startsWith('Source/'))}"/></Solution>`));
    await assert.rejects(validateSourceProjects(root), /duplicate/i);
});

test('completed receipts prove each shard and the global multiset, not merely total counts', () => {
    const plan = planShards(ordinary, weights);
    const receipts = plan.map(s => receiptFor(s, s.projects, context));
    assert.doesNotThrow(() => verifyReceipts(receipts, plan, context));
    const reject = mutate => {
        const bad = structuredClone(receipts);
        mutate(bad);
        assert.throws(() => verifyReceipts(bad, plan, context), /receipt|shard|project|completion|context|version/i);
    };
    reject(r => r.pop());
    reject(r => r.push(r[0]));
    reject(r => { r[1].shard = 1; });
    reject(r => { r[0].shards = 3; });
    reject(r => { r[0].version = 0; });
    reject(r => { r[0].context.sha = 'stale'; });
    reject(r => { r[0].context.runId = 'another-run'; });
    reject(r => { r[0].context.runAttempt = '0'; });
    reject(r => { r[0].completed = []; });
    reject(r => { r[0].completed.pop(); });
    reject(r => { r[0].completed[0] = r[0].completed[1]; });
    reject(r => { r[0].projects[0] = r[1].projects[0]; });
    reject(r => { r[0].completed[0] = 'Integration/Api/Api.csproj'; });
    reject(r => { r[0].completed = null; });
    reject(r => { r[0] = null; });
});

test('SQL-only failed-jobs retry retains both successful attempt-1 ordinary receipts', () => {
    const plan = planShards(ordinary, weights);
    const receipts = plan.map(s => receiptFor(s, s.projects, context));
    const retry = { ...context, runAttempt: '2' };
    // SQL failed in attempt 1; only SQL and the aggregate rerun in attempt 2.
    const producers = [1, 2].map(shard => ({ shard, runAttempt: '1' }));
    assert.doesNotThrow(() => verifyReceipts(receipts, plan, retry, producers));
});

test('mixed producer attempts keep exact receipt freshness and complete unique execution', () => {
    const plan = planShards(ordinary, weights);
    const retry = { ...context, runAttempt: '2' };
    const producers = [{ shard: 1, runAttempt: '1' }, { shard: 2, runAttempt: '2' }];
    const receipts = plan.map(s => receiptFor(s, s.projects, { ...context, runAttempt: producers[s.shard - 1].runAttempt }));
    assert.doesNotThrow(() => verifyReceipts(receipts, plan, retry, producers));
    for (const field of ['runAttempt', 'runId', 'sha']) {
        const bad = structuredClone(receipts);
        bad[1].context[field] = 'stale';
        assert.throws(() => verifyReceipts(bad, plan, retry, producers), /context/);
    }
    for (const bad of [[], [producers[0], producers[0]], [{ shard: 1, runAttempt: '3' }, producers[1]]]) {
        assert.throws(() => verifyReceipts(receipts, plan, retry, bad), /producer/i);
    }
    const incomplete = structuredClone(receipts);
    incomplete[1].completed.pop();
    assert.throws(() => verifyReceipts(incomplete, plan, retry, producers), /completion/);
});

test('result-directory verifier rejects absent, extra and malformed receipt files', async () => {
    const root = await fixture();
    const plan = await sourcePlan(root, weights);
    const results = path.join(root, 'results');
    await mkdir(results);
    await assert.rejects(verifyResults(results, plan, context), /receipt/i);
    for (const shard of plan) {
        const directory = path.join(results, `coverage-results-specs-other-${shard.shard}`);
        await mkdir(directory);
        await writeFile(path.join(directory, 'shard-receipt.json'), JSON.stringify(receiptFor(shard, shard.projects, context)));
    }
    await verifyResults(results, plan, context);
    const retry = { ...context, runAttempt: '2' };
    const evidence = { context: retry, producers: plan.map(s => ({ shard: s.shard, runAttempt: '1', artifactName: `coverage-results-specs-other-${s.shard}` })) };
    await verifyResults(results, plan, retry, evidence);
    await assert.rejects(verifyResults(results, plan, { ...retry, runAttempt: '3' }, evidence), /evidence context/);
    const wrongArtifact = structuredClone(evidence);
    wrongArtifact.producers[0].artifactName = 'unexpected-artifact';
    await assert.rejects(verifyResults(results, plan, retry, wrongArtifact), /artifact receipt path/);
    await writeFile(path.join(results, 'shard-receipt.json'), '{broken');
    await assert.rejects(verifyResults(results, plan, context));
    await writeFile(path.join(results, 'shard-receipt.json'), JSON.stringify(receiptFor(plan[0], plan[0].projects, context)));
    await assert.rejects(verifyResults(results, plan, context), /receipt/i);
});
