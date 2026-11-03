// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { appendFile, mkdir, readFile, realpath, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';
import { runProcess } from './mixed-specs-process.mjs';
import { validateTrx } from './mixed-specs-results.mjs';

// L0 decision 2026-11-02: paired added costs, not ratios against the disabled arm's bare enqueue.
export const criteria = Object.freeze({ minimumPairs: 5, addedSpread: 0.25, elapsedFloorMicroseconds: 1,
    steadyAddedMicroseconds: 500, burstAddedMicroseconds: 50, addedAllocatedBytesPerEvent: 4096, retainedBytes: 16 * 1024 * 1024 });
const policy = Object.freeze({
    decision: 'L0 decision 2026-11-02',
    validityRule: `At least ${criteria.minimumPairs} interleaved pairs; paired added-cost spread (max−min)/median ≤${criteria.addedSpread * 100}% for each metric/workload, or elapsed median added cost <${criteria.elapsedFloorMicroseconds} µs/event (valid by floor). The time-unit floor does not apply to allocation bytes. Otherwise INCONCLUSIVE.`,
    passBounds: `For valid measurements, PASS iff median added steady ≤${criteria.steadyAddedMicroseconds} µs/event, burst ≤${criteria.burstAddedMicroseconds} µs/event, allocation ≤${criteria.addedAllocatedBytesPerEvent} B/event for each workload, and maximum retained diagnostic state ≤${criteria.retainedBytes} bytes (16 MiB). Exceeding any bound is FAIL; invalid measurements remain INCONCLUSIVE.`,
    meaning: 'FAIL/INCONCLUSIVE blocks enabled shipping and the “unperturbed” reading, not the disabled seam.'
});
const filter = 'FullyQualifiedName=Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics.when_capturing_the_enabled_transport.should_write_the_overhead_sample_when_requested';
const metrics = ['elapsedMicrosecondsPerEvent', 'allocatedBytesPerEvent'];
const workloads = ['steady', 'burst'];

export function statistics(values) {
    const sorted = [...values].sort((a, b) => a - b);
    const median = (sorted[Math.floor((sorted.length - 1) / 2)] + sorted[Math.floor(sorted.length / 2)]) / 2;
    const range = sorted.at(-1) - sorted[0];
    return { median, spread: median === 0 ? null : range / median };
}

export function validateSample(sample, arm) {
    if (sample.schemaVersion !== 1 || sample.enabled !== (arm === 'enabled')) throw new Error(`${arm}: wrong assembly arm/schema`);
    if (sample.steady?.events !== 100 || sample.steady.eventsPerSecond !== 20 || sample.burst?.events !== 10000) throw new Error(`${arm}: wrong workload`);
    if (!(sample.steady.windowMilliseconds >= 4750 && sample.steady.windowMilliseconds <= 5250)) throw new Error(`${arm}: missed the steady 20 events/s window`);
    for (const workload of workloads) {
        for (const metric of ['elapsedMilliseconds', 'allocatedBytes']) {
            const value = sample[workload][metric];
            if (!Number.isFinite(value) || value < 0) throw new Error(`${arm}: invalid ${workload} ${metric}`);
        }
        if (sample[workload].elapsedMilliseconds === 0 || !Number.isSafeInteger(sample[workload].allocatedBytes)) throw new Error(`${arm}: invalid measurement precision`);
    }
    const retained = sample.retainedStateUpperBoundBytes;
    if (!Number.isSafeInteger(retained) || retained < 0 || (arm === 'disabled' ? retained !== 0 : retained === 0)) throw new Error(`${arm}: invalid retained state bound`);
}

function perEvent(measurement, workload) {
    const sample = measurement[workload];
    return { elapsedMicrosecondsPerEvent: sample.elapsedMilliseconds * 1000 / sample.events, allocatedBytesPerEvent: sample.allocatedBytes / sample.events };
}

function workloadSummary(measurements, workload) {
    return Object.fromEntries(metrics.map(metric => [metric, statistics(measurements.map(measurement => measurement[workload][metric]))]));
}

function armSummary(samples, arm) {
    const selected = samples.filter(sample => sample.arm === arm).map(sample => sample.measurement);
    const normalized = selected.map(measurement => Object.fromEntries(workloads.map(workload => [workload, perEvent(measurement, workload)])));
    return {
        ...Object.fromEntries(workloads.map(workload => [workload, workloadSummary(normalized, workload)])),
        retainedStateUpperBoundBytes: { ...statistics(selected.map(sample => sample.retainedStateUpperBoundBytes)), max: Math.max(...selected.map(sample => sample.retainedStateUpperBoundBytes)) }
    };
}

function pairedCosts(samples) {
    const added = [];
    for (let index = 0; index < samples.length; index += 2) {
        const disabled = samples[index], enabled = samples[index + 1];
        added.push({ pair: disabled.pair, ...Object.fromEntries(workloads.map(workload => {
            const before = perEvent(disabled.measurement, workload), after = perEvent(enabled.measurement, workload);
            return [workload, Object.fromEntries(metrics.map(metric => [metric, after[metric] - before[metric]]))];
        })) });
    }
    return added;
}

function addedSummary(addedPairs) {
    return Object.fromEntries(workloads.map(workload => {
        const summary = workloadSummary(addedPairs, workload);
        for (const metric of metrics) {
            const cost = summary[metric];
            // The microsecond floor is dimensionally applicable only to elapsed costs, not bytes.
            cost.validByFloor = metric === 'elapsedMicrosecondsPerEvent' && cost.median < criteria.elapsedFloorMicroseconds;
            cost.valid = cost.validByFloor || (cost.spread !== null && cost.spread >= 0 && cost.spread <= criteria.addedSpread);
        }
        return [workload, summary];
    }));
}

function validatePairs(samples) {
    for (const [index, sample] of samples.entries()) {
        if (sample.pair !== Math.floor(index / 2) + 1 || sample.arm !== (index % 2 ? 'enabled' : 'disabled')) throw new Error('Expected interleaved A B pairs');
        validateSample(sample.measurement, sample.arm);
    }
}

export function summarize(samples) {
    const result = { verdict: 'INCONCLUSIVE', reason: 'Measurement incomplete', pairs: Math.floor(samples.length / 2), criteria, policy, arms: {}, addedPairs: [], addedCosts: {} };
    if (samples.length < criteria.minimumPairs * 2 || samples.length % 2) return result;
    try { validatePairs(samples); } catch (error) { return { ...result, reason: error.message }; }
    result.arms = { disabled: armSummary(samples, 'disabled'), enabled: armSummary(samples, 'enabled') };
    result.addedPairs = pairedCosts(samples);
    result.addedCosts = addedSummary(result.addedPairs);
    const valid = workloads.every(workload => metrics.every(metric => result.addedCosts[workload][metric].valid));
    if (!valid) return { ...result, reason: 'Paired added-cost spread is invalid for a metric without an applicable elapsed floor' };
    const withinCosts = workloads.every(workload => result.addedCosts[workload].elapsedMicrosecondsPerEvent.median <= criteria[`${workload}AddedMicroseconds`]
        && result.addedCosts[workload].allocatedBytesPerEvent.median <= criteria.addedAllocatedBytesPerEvent);
    const withinStorage = result.arms.enabled.retainedStateUpperBoundBytes.max <= criteria.retainedBytes;
    const pass = withinCosts && withinStorage;
    return { ...result, verdict: pass ? 'PASS' : 'FAIL', reason: pass ? 'Valid paired added costs; all L0 cost and retained-state bounds satisfied' : 'Valid paired added costs; an L0 cost or retained-state bound exceeded' };
}

function markdown(result) {
    const rows = ['# Rolling delivery diagnostic overhead', '', `**Verdict: ${result.verdict}** — ${result.reason}`, '',
        `${result.pairs} interleaved disabled/enabled pairs on this runner. Synthetic generated-log + callback + enqueue harness, not a network/production benchmark.`,
        'Steady: 100 events at 20/s; elapsed is summed synchronous event work, excluding pacing delays. Burst: 10000 events. Allocations are synchronous event-thread bytes; setup/JIT and pacing are excluded.',
        `**${policy.decision}**`, policy.validityRule, policy.passBounds, `**${policy.meaning}**`, '',
        '| Arm/cost | Workload | Elapsed median (µs/event) | Spread | Allocation median (B/event) | Spread | Valid by elapsed floor |', '|---|---|---:|---:|---:|---:|---|' ];
    const spread = value => value === null ? 'undefined' : `${(value * 100).toFixed(3)}%`;
    const summaries = { ...result.arms, ...(result.addedPairs.length ? { 'enabled − disabled (paired)': result.addedCosts } : {}) };
    for (const [arm, summary] of Object.entries(summaries)) {
        for (const workload of workloads) {
            const values = summary[workload], elapsed = values.elapsedMicrosecondsPerEvent, allocated = values.allocatedBytesPerEvent;
            rows.push(`| ${arm} | ${workload} | ${elapsed.median} | ${spread(elapsed.spread)} | ${allocated.median} | ${spread(allocated.spread)} | ${elapsed.validByFloor ?? 'n/a'} |`);
        }
    }
    for (const [arm, summary] of Object.entries(result.arms)) rows.push('', `${arm} retained diagnostic state upper bound: median ${summary.retainedStateUpperBoundBytes.median}, max ${summary.retainedStateUpperBoundBytes.max} bytes.`, '');
    rows.push('Non-PASS outcomes fail the measurement step. Workflow continue-on-error preserves RollingUpgrade, not enabled-shipping acceptance.', '');
    return rows.join('\n');
}

async function publish(directory, result, samples) {
    await writeFile(path.join(directory, 'result.json'), JSON.stringify({ schemaVersion: 2, ...result, samples }, null, 2) + '\n');
    await writeFile(path.join(directory, 'result.md'), markdown(result));
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

function failure(error, samples) {
    return { ...summarize(samples), verdict: 'INCONCLUSIVE', reason: `Measurement error: ${error.message}` };
}

function announce(result, emit, setExitCode) {
    const exitCode = { PASS: 0, FAIL: 1, INCONCLUSIVE: 2 }[result.verdict];
    setExitCode(exitCode);
    const message = `Rolling delivery diagnostic overhead verdict: ${result.verdict}: ${result.reason}`;
    const escaped = message.replaceAll('%', '%25').replaceAll('\r', '%0D').replaceAll('\n', '%0A');
    emit(result.verdict === 'PASS' ? message : `::${result.verdict === 'FAIL' ? 'error' : 'warning'}::${escaped}`);
}

export async function main(args = process.argv.slice(2), { execute = runProcess, summaryPath = process.env.GITHUB_STEP_SUMMARY, setExitCode = code => { process.exitCode = code; }, emit = console.log } = {}) {
    const samples = [];
    let directory, result;
    try {
        const { values } = parseArgs({ args, options: { normal: { type: 'string' }, enabled: { type: 'string' }, output: { type: 'string' } } });
        if (!values.normal || !values.enabled || !values.output) throw new Error('Expected --normal DLL --enabled DLL --output directory');
        directory = path.resolve(values.output);
        await mkdir(directory, { recursive: true });
        // Keep explicit incomplete artifacts on interruption; publish the step summary only once, at completion.
        await publish(directory, summarize(samples), samples);
        const normal = await realpath(values.normal), enabled = await realpath(values.enabled);
        if (normal === enabled) throw new Error('Normal and enabled outputs must be distinct');
        for (let pair = 1; pair <= criteria.minimumPairs; pair++) {
            for (const [arm, dll] of [['disabled', normal], ['enabled', enabled]]) {
                const measurement = await sampleArm(dll, path.join(directory, `${pair}-${arm}`), arm, execute);
                samples.push({ pair, arm, measurement });
                await publish(directory, summarize(samples), samples);
            }
        }
        result = summarize(samples);
    } catch (error) {
        result = failure(error, samples);
    }
    try {
        if (directory) await publish(directory, result, samples);
    } catch (error) {
        result = failure(error, samples);
    }
    try {
        if (summaryPath) await appendFile(summaryPath, markdown(result));
    } catch (error) {
        result = failure(error, samples);
        // Do not append the summary a second time, even if publication failed.
        if (directory) await publish(directory, result, samples).catch(() => {});
    }
    announce(result, emit, setExitCode);
    return result;
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(`::warning::INCONCLUSIVE: overhead evidence unavailable: ${String(error.message).replaceAll('\n', '%0A').replaceAll('\r', '%0D')}`); process.exitCode = 2; });
}
