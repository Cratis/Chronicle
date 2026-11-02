// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Verdict/orchestration contracts only: these tests never execute the timing harness.
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';
import { criteria, main, statistics, summarize, validateSample } from './measure-rolling-delivery-overhead.mjs';
import { runProcess } from './mixed-specs-process.mjs';

function measurement(enabled) {
    return { schemaVersion: 1, enabled,
        steady: { events: 100, eventsPerSecond: 20, windowMilliseconds: 5000, elapsedMilliseconds: enabled ? 20 : 10, allocatedBytes: enabled ? 100000 : 10000 },
        burst: { events: 10000, elapsedMilliseconds: enabled ? 200 : 100, allocatedBytes: enabled ? 10000000 : 1000000 }, retainedStateUpperBoundBytes: enabled ? 11191648 : 0 };
}
function samples(pairs = 5) {
    return Array.from({ length: pairs * 2 }, (_, index) => ({ pair: Math.floor(index / 2) + 1, arm: index % 2 ? 'enabled' : 'disabled', measurement: measurement(index % 2 === 1) }));
}
function addedCosts(records, workload, metric, deltas) {
    for (let index = 0; index < records.length; index += 2) {
        const before = records[index].measurement[workload], after = records[index + 1].measurement[workload];
        after[metric] = before[metric] + deltas[index / 2] * before.events / (metric === 'elapsedMilliseconds' ? 1000 : 1);
    }
}

test('L0 decision constants and odd/even medians with relative max-minus-min spread', () => {
    assert.deepEqual(criteria, { minimumPairs: 5, addedSpread: 0.25, elapsedFloorMicroseconds: 1, steadyAddedMicroseconds: 500, burstAddedMicroseconds: 50, addedAllocatedBytesPerEvent: 4096, retainedBytes: 16777216 });
    assert.deepEqual(statistics([104, 100, 101, 102, 103]), { median: 102, spread: 4 / 102 });
    assert.deepEqual(statistics([10, 20, 30, 40, 50, 60]), { median: 35, spread: 50 / 35 });
    assert.deepEqual(statistics([0, 0, 0, 0, 0]), { median: 0, spread: null });
});

test('inclusive added-cost and storage bounds pass with five or more interleaved pairs', () => {
    for (const count of [5, 6]) {
        const records = samples(count);
        for (const workload of ['steady', 'burst']) {
            addedCosts(records, workload, 'elapsedMilliseconds', Array(count).fill(workload === 'steady' ? 500 : 50));
            addedCosts(records, workload, 'allocatedBytes', Array(count).fill(4096));
        }
        for (const record of records.filter(sample => sample.arm === 'enabled')) record.measurement.retainedStateUpperBoundBytes = 16 * 1024 * 1024;
        assert.equal(summarize(records).verdict, 'PASS');
    }
});

for (const workload of ['steady', 'burst']) {
    for (const metric of ['elapsedMilliseconds', 'allocatedBytes']) {
        test(`${workload} added ${metric} exceeding its L0 bound fails`, () => {
            const records = samples();
            const cost = metric === 'allocatedBytes' ? 4097 : (workload === 'steady' ? 501 : 51);
            addedCosts(records, workload, metric, Array(5).fill(cost));
            assert.equal(summarize(records).verdict, 'FAIL');
        });
        test(`${workload} added ${metric} spread above 25% is INCONCLUSIVE; equality is valid`, () => {
            const records = samples();
            addedCosts(records, workload, metric, [100, 100, 100, 100, 126]);
            assert.equal(summarize(records).verdict, 'INCONCLUSIVE');
            addedCosts(records, workload, metric, [100, 100, 100, 100, 125]);
            // Burst elapsed is valid but exceeds its cost bound, all other metrics pass.
            assert.equal(summarize(records).verdict, workload === 'burst' && metric === 'elapsedMilliseconds' ? 'FAIL' : 'PASS');
        });
    }
    test(`${workload} elapsed median below 1 microsecond is valid by floor despite spread (including zero median)`, () => {
        for (const costs of [[0.1, 0.2, 0.5, 0.8, 10], [-0.5, -0.1, 0, 0.2, 10]]) {
            const records = samples();
            addedCosts(records, workload, 'elapsedMilliseconds', costs);
            const result = summarize(records);
            assert.equal(result.verdict, 'PASS');
            assert.equal(result.addedCosts[workload].elapsedMicrosecondsPerEvent.validByFloor, true);
        }
        const records = samples();
        addedCosts(records, workload, 'elapsedMilliseconds', [0.1, 0.2, 1, 1.2, 10]);
        assert.equal(summarize(records).verdict, 'INCONCLUSIVE'); // Floor is strictly below 1.
    });
}

test('paired subtraction, not subtraction of medians or baseline ratios/spread, determines cost', () => {
    const records = samples();
    const before = [1, 2, 3, 4, 5], delta = [1, 1, 1, 1, -4];
    for (let pair = 0; pair < 5; pair++) {
        records[pair * 2].measurement.steady.elapsedMilliseconds = before[pair];
        records[pair * 2 + 1].measurement.steady.elapsedMilliseconds = before[pair] + delta[pair];
    }
    const result = summarize(records);
    assert.deepEqual(result.addedPairs.map(pair => pair.steady.elapsedMicrosecondsPerEvent), [10, 10, 10, 10, -40]);
    assert.equal(result.addedCosts.steady.elapsedMicrosecondsPerEvent.median, 10);
    assert.equal(result.arms.enabled.steady.elapsedMicrosecondsPerEvent.median - result.arms.disabled.steady.elapsedMicrosecondsPerEvent.median, 0);
    assert.equal(result.verdict, 'INCONCLUSIVE');
    addedCosts(records, 'steady', 'elapsedMilliseconds', Array(5).fill(100));
    assert.equal(summarize(records).verdict, 'PASS'); // Disabled spread is deliberately high.
});

test('allocation has no microsecond floor and storage maximum, not median, must satisfy its cap', () => {
    const records = samples();
    addedCosts(records, 'steady', 'allocatedBytes', [0, 0, 0, 0, 0]);
    assert.equal(summarize(records).verdict, 'INCONCLUSIVE');
    const storage = samples();
    storage[1].measurement.retainedStateUpperBoundBytes = 16 * 1024 * 1024 + 1;
    assert.equal(summarize(storage).verdict, 'FAIL');
});

test('incomplete, non-interleaved, wrong-arm and malformed evidence cannot pass', () => {
    assert.equal(summarize(samples().slice(0, 8)).verdict, 'INCONCLUSIVE');
    assert.equal(summarize(samples().slice(0, 9)).verdict, 'INCONCLUSIVE');
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

for (const verdict of ['PASS', 'FAIL', 'INCONCLUSIVE', 'exception', 'unavailable']) {
    test(`MOCK ${verdict} driver uses real Node exit code/annotation and exactly one summary, without timing execution`, async () => {
        const paths = await fixture();
        // Real default process.exitCode behavior, but dotnet execution is replaced entirely with synthetic samples.
        const bootstrap = `
            import assert from 'node:assert/strict';
            import { writeFile } from 'node:fs/promises';
            import { main } from './.github/scripts/measure-rolling-delivery-overhead.mjs';
            const paths = ${JSON.stringify(paths)}, mode = ${JSON.stringify(verdict)};
            const measurement = ${measurement.toString()};
            const arms = [];
            const result = await main(${JSON.stringify(argumentsFor(paths))}, { summaryPath: paths.summaryPath, execute: async (command, args, options) => {
                assert.equal(command, 'dotnet');
                assert.ok([paths.normal, paths.enabled].includes(args[0]));
                assert.ok(args[2].endsWith('should_write_the_overhead_sample_when_requested'));
                assert.equal(options.timeout, 30000);
                assert.equal(process.env.DOTNET_TieredCompilation, '0');
                if (mode === 'exception') throw new Error('Deadline exceeded');
                const enabled = args[0] === paths.enabled;
                arms.push(enabled ? 'B' : 'A');
                const sample = measurement(enabled);
                if (mode === 'FAIL' && enabled) sample.steady.elapsedMilliseconds = 100;
                if (mode === 'INCONCLUSIVE' && enabled && arms.length === 10) sample.steady.elapsedMilliseconds = 30;
                if (mode !== 'unavailable') await writeFile(process.env.CRATIS_ROLLING_OVERHEAD_SAMPLE, JSON.stringify(sample));
                await writeFile(args.at(-1), ${JSON.stringify(trx)});
                return { code: 0 };
            } });
            if (!['exception', 'unavailable'].includes(mode)) assert.equal(arms.join(''), 'ABABABABAB');
            assert.equal(result.verdict, ['exception', 'unavailable'].includes(mode) ? 'INCONCLUSIVE' : mode);
        `;
        const child = await runProcess('node', ['--input-type=module', '--eval', bootstrap], { cwd: process.cwd(), capture: true, timeout: 10000 });
        const expected = ['exception', 'unavailable'].includes(verdict) ? 'INCONCLUSIVE' : verdict;
        assert.equal(child.code, { PASS: 0, FAIL: 1, INCONCLUSIVE: 2 }[expected], child.stderr);
        if (expected === 'PASS') assert.ok(!child.stdout.includes('::error::') && !child.stdout.includes('::warning::'));
        else assert.match(child.stdout, new RegExp(`::${expected === 'FAIL' ? 'error' : 'warning'}::.*${expected}`));
        const report = JSON.parse(await readFile(path.join(paths.output, 'result.json'), 'utf8'));
        assert.equal(report.verdict, expected);
        assert.deepEqual(report.criteria, criteria);
        assert.equal(report.policy.decision, 'L0 decision 2026-11-02');
        assert.match(report.policy.meaning, /FAIL\/INCONCLUSIVE blocks enabled shipping.*not the disabled seam/);
        assert.match(report.policy.validityRule, /25%.*<1 µs\/event/);
        assert.match(report.policy.passBounds, /500 µs\/event.*50 µs\/event.*4096 B\/event.*16777216 bytes/);
        for (const file of [paths.summaryPath, path.join(paths.output, 'result.md')]) {
            const text = await readFile(file, 'utf8');
            assert.equal(text.match(/# Rolling delivery diagnostic overhead/g)?.length, 1);
            assert.ok(text.includes(`**Verdict: ${expected}**`));
            for (const value of Object.values(report.policy)) assert.ok(text.includes(value));
        }
        if (!['exception', 'unavailable'].includes(verdict)) {
            assert.equal(report.samples.length, 10);
            assert.equal(report.addedPairs.length, 5);
            assert.ok(report.arms.disabled.steady.elapsedMicrosecondsPerEvent);
            assert.ok(report.addedCosts.burst.allocatedBytesPerEvent);
        }
    });
}

test('unavailable artifact directory still produces one INCONCLUSIVE summary and nonzero exit', async () => {
    const paths = await fixture(), codes = [], annotations = [];
    const result = await main(['--normal', paths.normal, '--enabled', paths.enabled, '--output', paths.normal], {
        summaryPath: paths.summaryPath, setExitCode: code => codes.push(code), emit: text => annotations.push(text), execute: () => assert.fail('must not execute')
    });
    assert.equal(result.verdict, 'INCONCLUSIVE');
    assert.deepEqual(codes, [2]);
    assert.match(annotations[0], /^::warning::/);
    assert.equal((await readFile(paths.summaryPath, 'utf8')).match(/# Rolling delivery diagnostic overhead/g).length, 1);
});

test('workflow uploads immediately with always/continue-on-error; later diagnostics upload stays intact', async () => {
    const workflow = await readFile('.github/workflows/integration.yml', 'utf8');
    const start = workflow.indexOf('- name: Measure rolling delivery diagnostic overhead');
    const end = workflow.indexOf('- name: Upload rolling delivery overhead evidence immediately', start);
    const pull = workflow.indexOf('- name: Pre-pull images', end);
    assert.ok(start > 0 && end > start && pull > end);
    const step = workflow.slice(start, end), upload = workflow.slice(end, pull);
    assert.equal((step.match(/- name:/g) ?? []).length, 1);
    assert.match(step, /timeout-minutes: 8/);
    assert.match(step, /continue-on-error: true/);
    assert.match(step, /if: matrix.provider == 'RollingUpgrade'/);
    assert.ok(!step.includes('${{'));
    assert.match(step, /--normal Integration\/SqlClustering\/bin\/Release/);
    assert.match(step, /--enabled "\$RUNNER_TEMP\/rolling-delivery-diagnostics/);
    assert.match(upload, /if: always\(\)\n        continue-on-error: true\n        uses: actions\/upload-artifact@v4/);
    assert.match(upload, /name: rolling-delivery-overhead\n/);
    assert.match(workflow, /- name: Upload rolling delivery diagnostics evidence\n        if: always\(\) && matrix.provider == 'RollingUpgrade'\n        uses: actions\/upload-artifact@v4\n        with:\n          name: rolling-delivery-diagnostics/);
});
