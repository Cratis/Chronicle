// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { appendFile, mkdir, readFile, realpath, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';
import { runProcess } from './mixed-specs-process.mjs';
import { validateTrx } from './mixed-specs-results.mjs';

const pairs = 5;
const retainedLimit = 16 * 1024 * 1024;
const filter = 'FullyQualifiedName=Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics.when_capturing_the_enabled_transport.should_write_the_overhead_sample_when_requested';
const metrics = ['elapsedMilliseconds', 'allocatedBytes'];
const workloads = ['steady', 'burst'];

export function statistics(values) {
    const sorted = [...values].sort((a, b) => a - b);
    const median = sorted[Math.floor(sorted.length / 2)];
    const range = sorted.at(-1) - sorted[0];
    return { median, spread: median === 0 ? null : range / median };
}

export function validateSample(sample, arm) {
    if (sample.schemaVersion !== 1 || sample.enabled !== (arm === 'enabled')) throw new Error(`${arm}: wrong assembly arm/schema`);
    if (sample.steady?.events !== 100 || sample.steady.eventsPerSecond !== 20 || sample.burst?.events !== 10000) throw new Error(`${arm}: wrong workload`);
    if (!(sample.steady.windowMilliseconds >= 4750 && sample.steady.windowMilliseconds <= 5250)) throw new Error(`${arm}: missed the steady 20 events/s window`);
    for (const workload of workloads) {
        for (const metric of metrics) {
            const value = sample[workload][metric];
            if (!Number.isFinite(value) || value < 0) throw new Error(`${arm}: invalid ${workload} ${metric}`);
        }
        if (sample[workload].elapsedMilliseconds === 0 || !Number.isSafeInteger(sample[workload].allocatedBytes)) throw new Error(`${arm}: invalid measurement precision`);
    }
    const retained = sample.retainedStateUpperBoundBytes;
    if (!Number.isSafeInteger(retained) || retained < 0 || (arm === 'disabled' ? retained !== 0 : retained === 0)) throw new Error(`${arm}: invalid retained state bound`);
}

function armSummary(samples, arm) {
    const selected = samples.filter(sample => sample.arm === arm).map(sample => sample.measurement);
    return {
        ...Object.fromEntries(workloads.map(workload => [workload, Object.fromEntries(metrics.map(metric => [metric, statistics(selected.map(sample => sample[workload][metric]))]))])),
        retainedStateUpperBoundBytes: { ...statistics(selected.map(sample => sample.retainedStateUpperBoundBytes)), max: Math.max(...selected.map(sample => sample.retainedStateUpperBoundBytes)) }
    };
}

export function summarize(samples) {
    const result = { verdict: 'INCONCLUSIVE', reason: 'Measurement incomplete', pairs, bounds: { disabledSpread: 0.05, steadyRatio: 1.10, burstRatio: 1.25, retainedBytes: retainedLimit }, arms: {} };
    if (samples.length !== pairs * 2) return result;
    for (const [index, sample] of samples.entries()) {
        if (sample.pair !== Math.floor(index / 2) + 1 || sample.arm !== (index % 2 ? 'enabled' : 'disabled')) return { ...result, reason: 'Expected five interleaved A B pairs' };
        try { validateSample(sample.measurement, sample.arm); } catch (error) { return { ...result, reason: error.message }; }
    }
    result.arms = { disabled: armSummary(samples, 'disabled'), enabled: armSummary(samples, 'enabled') };
    const valid = workloads.every(workload => metrics.every(metric => {
        const spread = result.arms.disabled[workload][metric].spread;
        return spread !== null && spread <= result.bounds.disabledSpread;
    }));
    if (!valid) return { ...result, reason: 'Disabled-arm elapsed/allocation spread exceeds 5% (or has an undefined zero-median spread)' };
    const withinRatios = workloads.every(workload => metrics.every(metric => result.arms.enabled[workload][metric].median <= result.arms.disabled[workload][metric].median * result.bounds[`${workload}Ratio`]));
    const withinStorage = result.arms.enabled.retainedStateUpperBoundBytes.max <= retainedLimit;
    return { ...result, verdict: withinRatios && withinStorage ? 'PASS' : 'FAIL', reason: withinRatios && withinStorage ? 'Valid baseline; all elapsed/allocation ratios and retained-state bounds satisfied' : 'Valid baseline; an elapsed/allocation ratio or retained-state bound exceeded' };
}

function markdown(result) {
    const rows = ['# Rolling delivery diagnostic overhead', '', `**Verdict: ${result.verdict}** — ${result.reason}`, '',
        'Five interleaved disabled/enabled pairs on this runner. Synthetic generated-log + callback + enqueue harness, not a network/production benchmark.',
        'Steady: 100 events at 20/s; elapsed is summed synchronous event work, excluding pacing delays. Burst: 10000 events. Allocations are synchronous event-thread bytes; setup/JIT and pacing are excluded.',
        'Validity requires disabled elapsed AND allocation spreads ≤5% for both workloads. PASS requires both metrics ≤1.10× steady / ≤1.25× burst and maximum retained diagnostic storage upper bound ≤16 MiB. A zero disabled median has undefined spread and is INCONCLUSIVE.', '',
        '| Arm | Workload | Elapsed median (ms) | Spread | Allocation median (bytes) | Spread |', '|---|---|---:|---:|---:|---:|' ];
    const spread = value => value === null ? 'undefined' : `${(value * 100).toFixed(3)}%`;
    for (const [arm, summary] of Object.entries(result.arms)) {
        for (const workload of workloads) {
            const values = summary[workload];
            rows.push(`| ${arm} | ${workload} | ${values.elapsedMilliseconds.median} | ${spread(values.elapsedMilliseconds.spread)} | ${values.allocatedBytes.median} | ${spread(values.allocatedBytes.spread)} |`);
        }
    }
    for (const [arm, summary] of Object.entries(result.arms)) rows.push('', `${arm} retained diagnostic state upper bound: median ${summary.retainedStateUpperBoundBytes.median}, max ${summary.retainedStateUpperBoundBytes.max} bytes.`, '');
    rows.push('This evidence does not gate or skip RollingUpgrade; shipping-enabled acceptance remains a human decision.', '');
    return rows.join('\n');
}

async function publish(directory, result, samples, summaryPath) {
    const report = { schemaVersion: 1, ...result, samples };
    await writeFile(path.join(directory, 'result.json'), JSON.stringify(report, null, 2) + '\n');
    const text = markdown(result);
    await writeFile(path.join(directory, 'result.md'), text);
    if (summaryPath) await appendFile(summaryPath, text);
    console.log(`Rolling delivery diagnostic overhead verdict: ${result.verdict}: ${result.reason}`);
}

async function sampleArm(dll, directory, arm, execute) {
    await mkdir(directory); // Reject reused/stale sample paths rather than accepting old evidence.
    const samplePath = path.join(directory, 'sample.json');
    const previous = process.env.CRATIS_ROLLING_OVERHEAD_SAMPLE;
    const previousTiering = process.env.DOTNET_TieredCompilation;
    try {
        process.env.CRATIS_ROLLING_OVERHEAD_SAMPLE = samplePath;
        process.env.DOTNET_TieredCompilation = '0';
        const child = await execute('dotnet', [dll, '-filterVSTest', filter, '-result-trx', path.join(directory, 'results.trx')], { cwd: process.cwd(), timeout: 30000 });
        if (child.code !== 0) throw new Error(`${arm}: sample process exited ${child.code}`);
        const identities = validateTrx(await readFile(path.join(directory, 'results.trx'), 'utf8'));
        if (identities.length !== 1) throw new Error(`${arm}: expected exactly one sample specification`);
        const measurement = JSON.parse(await readFile(samplePath, 'utf8'));
        validateSample(measurement, arm);
        return measurement;
    } finally {
        if (previous === undefined) delete process.env.CRATIS_ROLLING_OVERHEAD_SAMPLE;
        else process.env.CRATIS_ROLLING_OVERHEAD_SAMPLE = previous;
        if (previousTiering === undefined) delete process.env.DOTNET_TieredCompilation;
        else process.env.DOTNET_TieredCompilation = previousTiering;
    }
}

export async function main(args = process.argv.slice(2), { execute = runProcess, summaryPath = process.env.GITHUB_STEP_SUMMARY } = {}) {
    const { values } = parseArgs({ args, options: { normal: { type: 'string' }, enabled: { type: 'string' }, output: { type: 'string' } } });
    if (!values.normal || !values.enabled || !values.output) throw new Error('Expected --normal DLL --enabled DLL --output directory');
    const directory = path.resolve(values.output), samples = [];
    await mkdir(directory, { recursive: true });
    // Leave an explicit incomplete verdict even if the step is interrupted before final publication.
    await publish(directory, summarize(samples), samples, summaryPath);
    let result;
    try {
        const normal = await realpath(values.normal), enabled = await realpath(values.enabled);
        if (normal === enabled) throw new Error('Normal and enabled outputs must be distinct');
        for (let pair = 1; pair <= pairs; pair++) {
            for (const [arm, dll] of [['disabled', normal], ['enabled', enabled]]) {
                const measurement = await sampleArm(dll, path.join(directory, `${pair}-${arm}`), arm, execute);
                samples.push({ pair, arm, measurement });
                await publish(directory, summarize(samples), samples); // Preserve partial evidence on deadline/interruption.
            }
        }
        result = summarize(samples);
    } catch (error) {
        result = { ...summarize(samples), verdict: 'INCONCLUSIVE', reason: `Measurement error: ${error.message}` };
    }
    await publish(directory, result, samples, summaryPath);
    return result; // Evidence only: FAIL/INCONCLUSIVE deliberately do not fail the integration job.
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(`INCONCLUSIVE: overhead evidence unavailable: ${error.message}`); process.exitCode = 1; });
}
