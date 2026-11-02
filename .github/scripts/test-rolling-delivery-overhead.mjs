// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Verdict/orchestration contracts only: these tests never execute the timing harness.
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';
import { main, statistics, summarize, validateSample } from './measure-rolling-delivery-overhead.mjs';

function measurement(enabled) {
    return { schemaVersion: 1, enabled,
        steady: { events: 100, eventsPerSecond: 20, windowMilliseconds: 5000, elapsedMilliseconds: 100, allocatedBytes: 100 },
        burst: { events: 10000, elapsedMilliseconds: 100, allocatedBytes: 100 }, retainedStateUpperBoundBytes: enabled ? 11191648 : 0 };
}
function samples() {
    return Array.from({ length: 10 }, (_, index) => ({ pair: Math.floor(index / 2) + 1, arm: index % 2 ? 'enabled' : 'disabled', measurement: measurement(index % 2 === 1) }));
}
function enabledValues(records, workload, metric, value) {
    for (const record of records.filter(sample => sample.arm === 'enabled')) record.measurement[workload][metric] = value;
}

test('median and relative max-minus-min spread use raw samples, not the mean', () => {
    assert.deepEqual(statistics([104, 100, 101, 102, 103]), { median: 102, spread: 4 / 102 });
    assert.deepEqual(statistics([0, 0, 0, 0, 0]), { median: 0, spread: 0 });
    assert.deepEqual(statistics([0, 0, 0, 0, 1]), { median: 0, spread: null });
});

test('five interleaved pairs at inclusive elapsed/allocation and storage bounds pass', () => {
    const records = samples();
    for (const metric of ['elapsedMilliseconds', 'allocatedBytes']) {
        enabledValues(records, 'steady', metric, 110);
        enabledValues(records, 'burst', metric, 125);
    }
    for (const record of records.filter(sample => sample.arm === 'enabled')) record.measurement.retainedStateUpperBoundBytes = 16 * 1024 * 1024;
    assert.equal(summarize(records).verdict, 'PASS');
});

for (const workload of ['steady', 'burst']) {
    for (const metric of ['elapsedMilliseconds', 'allocatedBytes']) {
        test(`${workload} ${metric} exceeding its enabled bound fails`, () => {
            const records = samples();
            enabledValues(records, workload, metric, workload === 'steady' ? 111 : 126);
            assert.equal(summarize(records).verdict, 'FAIL');
        });
        test(`${workload} ${metric} noisy disabled arm is INCONCLUSIVE, never PASS/FAIL`, () => {
            const records = samples();
            records[0].measurement[workload][metric] = 106;
            assert.equal(summarize(records).verdict, 'INCONCLUSIVE');
            records[0].measurement[workload][metric] = 105;
            assert.equal(summarize(records).verdict, 'PASS');
        });
    }
}

test('storage maximum, not merely its median, must satisfy the cap', () => {
    const records = samples();
    records[1].measurement.retainedStateUpperBoundBytes = 16 * 1024 * 1024 + 1;
    assert.equal(summarize(records).verdict, 'FAIL');
});

test('zero allocation baseline never divides by zero or green-washes enabled allocations', () => {
    const records = samples();
    for (const record of records) record.measurement.steady.allocatedBytes = 0;
    assert.equal(summarize(records).verdict, 'PASS');
    enabledValues(records, 'steady', 'allocatedBytes', 1);
    assert.equal(summarize(records).verdict, 'FAIL');
    records[0].measurement.steady.allocatedBytes = 1;
    assert.equal(summarize(records).verdict, 'INCONCLUSIVE');
});

test('incomplete, non-interleaved, wrong-arm and malformed evidence cannot pass', () => {
    assert.equal(summarize(samples().slice(0, 8)).verdict, 'INCONCLUSIVE');
    const records = samples();
    [records[0], records[1]] = [records[1], records[0]];
    assert.equal(summarize(records).verdict, 'INCONCLUSIVE');
    for (const corrupt of [sample => { sample.enabled = false; }, sample => { sample.steady.windowMilliseconds = 7000; }, sample => { sample.burst.events = 9999; }, sample => { sample.burst.elapsedMilliseconds = NaN; }, sample => { sample.steady.allocatedBytes = -1; }, sample => { sample.retainedStateUpperBoundBytes = 0; }]) {
        const sample = measurement(true);
        corrupt(sample);
        assert.throws(() => validateSample(sample, 'enabled'));
    }
});

async function fixture() {
    const scratch = path.resolve('.ai-work/t5ep/overhead-contracts');
    await mkdir(scratch, { recursive: true });
    const root = await mkdtemp(path.join(scratch, 'case-'));
    const normal = path.join(root, 'normal.dll'), enabled = path.join(root, 'enabled.dll');
    await writeFile(normal, 'mock output; never executed');
    await writeFile(enabled, 'mock output; never executed');
    return { normal, enabled, output: path.join(root, 'evidence'), summaryPath: path.join(root, 'summary.md') };
}
const trx = '<?xml version="1.0"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testId="sample" testName="sample" outcome="Passed" /></Results><TestDefinitions><UnitTest id="sample"><TestMethod className="Sample" name="sample" /></UnitTest></TestDefinitions><ResultSummary><Counters total="1" executed="1" passed="1" failed="0" error="0" /></ResultSummary></TestRun>';
function argumentsFor(fixture) { return ['--normal', fixture.normal, '--enabled', fixture.enabled, '--output', fixture.output]; }

for (const verdict of ['PASS', 'FAIL']) {
    test(`MOCK driver emits ${verdict} JSON/markdown/summary without failing, invokes A B five times with no build`, async () => {
        const fixturePaths = await fixture(), arms = [];
        const result = await main(argumentsFor(fixturePaths), { summaryPath: fixturePaths.summaryPath, execute: async (command, args, options) => {
            assert.equal(command, 'dotnet');
            assert.ok([fixturePaths.normal, fixturePaths.enabled].includes(args[0]));
            assert.ok(args[2].endsWith('should_write_the_overhead_sample_when_requested'));
            assert.equal(options.timeout, 30000);
            assert.equal(process.env.DOTNET_TieredCompilation, '0');
            const enabled = args[0] === fixturePaths.enabled;
            arms.push(enabled ? 'B' : 'A');
            const sample = measurement(enabled);
            if (verdict === 'FAIL' && enabled) sample.burst.elapsedMilliseconds = 200;
            await writeFile(process.env.CRATIS_ROLLING_OVERHEAD_SAMPLE, JSON.stringify(sample));
            await writeFile(args.at(-1), trx);
            return { code: 0 };
        } });
        assert.equal(arms.join(''), 'ABABABABAB');
        assert.equal(result.verdict, verdict);
        const report = JSON.parse(await readFile(path.join(fixturePaths.output, 'result.json'), 'utf8'));
        assert.equal(report.verdict, verdict);
        assert.equal(report.samples.length, 10);
        for (const file of [fixturePaths.summaryPath, path.join(fixturePaths.output, 'result.md')]) assert.ok((await readFile(file, 'utf8')).includes(`**Verdict: ${verdict}**`));
    });
}

test('MOCK deadline/missing sample is INCONCLUSIVE and leaves explicit artifacts/summary', async () => {
    const fixturePaths = await fixture();
    const result = await main(argumentsFor(fixturePaths), { summaryPath: fixturePaths.summaryPath, execute: async () => { throw new Error('Deadline exceeded'); } });
    assert.equal(result.verdict, 'INCONCLUSIVE');
    assert.match(result.reason, /Deadline exceeded/);
    assert.equal(JSON.parse(await readFile(path.join(fixturePaths.output, 'result.json'), 'utf8')).verdict, 'INCONCLUSIVE');
    assert.match(await readFile(fixturePaths.summaryPath, 'utf8'), /Verdict: INCONCLUSIVE/);
});

test('MOCK reused output paths are rejected; prior samples cannot become new passing evidence', async () => {
    const fixturePaths = await fixture();
    await mkdir(path.join(fixturePaths.output, '1-disabled'), { recursive: true });
    const result = await main(argumentsFor(fixturePaths), { summaryPath: fixturePaths.summaryPath, execute: () => assert.fail('must reject stale evidence before executing') });
    assert.equal(result.verdict, 'INCONCLUSIVE');
});

test('workflow measures before RollingUpgrade and publishes evidence without a verdict gate or extra permissions', async () => {
    const workflow = await readFile('.github/workflows/integration.yml', 'utf8');
    const start = workflow.indexOf('- name: Measure rolling delivery diagnostic overhead');
    const end = workflow.indexOf('- name: Pre-pull images', start);
    assert.ok(start > 0 && end > start && end < workflow.indexOf('- name: Run SQL clustering specifications'));
    const step = workflow.slice(start, end);
    assert.match(step, /timeout-minutes: 8/);
    assert.match(step, /continue-on-error: true/);
    assert.match(step, /if: matrix.provider == 'RollingUpgrade'/);
    assert.ok(!step.includes('${{'));
    assert.match(step, /--normal Integration\/SqlClustering\/bin\/Release/);
    assert.match(step, /--enabled "\$RUNNER_TEMP\/rolling-delivery-diagnostics/);
    assert.match(workflow, /if: always\(\) && matrix.provider == 'RollingUpgrade'\n        uses: actions\/upload-artifact@v4\n        with:\n          name: rolling-delivery-diagnostics/);
});
