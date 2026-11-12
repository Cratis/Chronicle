// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { CORE_PROJECT, CORE_PARTS, checkPartition } from './core-spec-partition.mjs';
import { validateSourceProjects } from './mixed-specs-selection.mjs';
import { balanceJobs, balancePrefixes, slowest } from './spec-shard-balancing.mjs';
import { ORDINARY_SHARDS } from './spec-shards.mjs';

const fail = message => { throw new Error(message); };
const positive = value => Number.isFinite(value) && value > 0;
const round = seconds => Number(seconds.toFixed(3));
const coreId = (part, parts) => `${CORE_PROJECT}#core-part-${part}-of-${parts}`;
export function wallRecords(logs) {
    const records = logs.flatMap(log => [...log.matchAll(/Spec project completed: (.+); ([\d.]+) seconds including validation/g)]
        .map(match => ({ project: match[1], seconds: Number(match[2]) })));
    const ids = new Set(records.map(record => record.project));
    if (!records.length || ids.size !== records.length) fail('Missing/duplicate project wall timings');
    if (records.some(record => !positive(record.seconds))) fail('Invalid project wall timing');
    return records;
}
function observedParts(samples, units) {
    const count = Number(samples[0]?.project.match(/-of-(\d+)$/)?.[1]);
    if (!Number.isInteger(count)) fail('Incomplete Core wall timings');
    if (count < 2 || count > units.length) fail('Incomplete Core wall timings');
    const parts = balancePrefixes(units, count);
    const expected = parts.map(part => coreId(part.part, count));
    if (samples.length !== count) fail('Incomplete Core wall timings');
    if (samples.some(sample => !expected.includes(sample.project))) fail('Incomplete Core wall timings');
    return parts;
}
function scalePart(part, samples, fixedSeconds, variableSeconds) {
    const seconds = samples.find(sample => sample.project === coreId(part.part, samples.length)).seconds - fixedSeconds;
    if (!positive(seconds)) fail('Core wall timing must exceed fixed overhead');
    return part.units.map(unit => ({ ...unit, weight: unit.weight / part.weight * seconds / variableSeconds }));
}
export function measuredCore(records, units, fixedSeconds) {
    const samples = records.filter(record => record.project.startsWith(`${CORE_PROJECT}#`));
    const parts = observedParts(samples, units);
    const variableSeconds = samples.reduce((sum, sample) => sum + sample.seconds - fixedSeconds, 0);
    const calibrated = parts.flatMap(part => scalePart(part, samples, fixedSeconds, variableSeconds))
        .sort((a, b) => a.prefix < b.prefix ? -1 : a.prefix > b.prefix ? 1 : 0);
    return { units: calibrated, model: { fixedSeconds, variableSeconds: round(variableSeconds), samples } };
}
export function candidatePlans(units, ordinary, model) {
    const candidates = [];
    const ordinarySeconds = ordinary.reduce((sum, record) => sum + record.seconds, 0);
    for (let count = 2; count <= units.length; count++) {
        // Every larger count has at least this average load, independent of placement.
        const lowerBound = (ordinarySeconds + model.variableSeconds + count * model.fixedSeconds) / ORDINARY_SHARDS;
        if (candidates.length && lowerBound >= Math.min(...candidates.map(candidate => slowest(candidate.shards)))) break;
        const parts = balancePrefixes(units, count);
        const jobs = [...ordinary, ...parts.map(part => ({ project: coreId(part.part, count), core: true,
            seconds: model.fixedSeconds + model.variableSeconds * part.weight }))];
        candidates.push({ count, shards: balanceJobs(jobs, ORDINARY_SHARDS) });
    }
    return candidates.sort((a, b) => slowest(a.shards) - slowest(b.shards) || a.count - b.count);
}
export function shardOverhead(jobs, logs) {
    const residuals = logs.map((log, index) => {
        const job = jobs.find(job => job.name === `specs-other (${index + 1})`);
        const step = job?.steps.find(step => step.name === 'Run tests with coverage');
        if (!step || step.conclusion !== 'success') fail('Missing successful coverage step');
        const seconds = (Date.parse(step.completedAt) - Date.parse(step.startedAt)) / 1000;
        const residual = seconds - wallRecords([log]).reduce((sum, record) => sum + record.seconds, 0);
        if (!positive(residual)) fail('Invalid shard overhead');
        return residual;
    });
    return round(residuals.reduce((sum, seconds) => sum + seconds, 0) / residuals.length);
}
export async function main(args = process.argv.slice(2), root = process.cwd()) {
    if (args.length !== 6) fail('Usage: refresh-spec-shard-timings.mjs <run> <previous-partition.json> <jobs.json> <shard-1.log> <shard-2.log> <shard-3.log>');
    const [run, previous, jobsFile, ...files] = args;
    const logs = await Promise.all(files.map(file => readFile(file, 'utf8')));
    const records = wallRecords(logs);
    const baseline = JSON.parse(await readFile(previous, 'utf8'));
    const calibrated = measuredCore(records, baseline.units, 55);
    const measurements = records.filter(record => !record.project.startsWith(`${CORE_PROJECT}#`));
    const source = await validateSourceProjects(root);
    const expected = source.ordinary.filter(project => project !== CORE_PROJECT);
    if (measurements.length !== expected.length || measurements.some(record => !expected.includes(record.project))) fail('Incomplete ordinary project wall timings');
    const partition = { version: 1, units: calibrated.units };
    const candidates = candidatePlans(partition.units, measurements, calibrated.model);
    const best = candidates[0];
    if (best.count !== CORE_PARTS) fail(`Set CORE_PARTS to model-selected ${best.count} before refreshing`);
    await checkPartition(root, partition);
    const { jobs } = JSON.parse(await readFile(jobsFile, 'utf8'));
    const timings = {
        version: 1, units: 'seconds', date: new Date(jobs[0].steps[0].startedAt).toISOString().slice(0, 10),
        method: `Train 8 run ${run}: explicit Spec project completed seconds including validation from all three ordinary shards. Core namespace weights are calibrated within each old part by (wall seconds - 55) / predicted weight, then normalized. Core variable time is conserved; each new part pays 55 seconds fixed overhead. Shard overhead is the mean coverage-step duration minus logged project wall times. Predictions are hypotheses, not observed savings.`,
        samples: files.map((_, index) => ({ run, shard: index + 1 })), defaultSeconds: 60,
        shardOverheadSeconds: shardOverhead(jobs, logs), core: calibrated.model,
        measurements: measurements.sort((a, b) => a.project < b.project ? -1 : a.project > b.project ? 1 : 0)
    };
    await writeFile(path.join(root, '.github/scripts/core-spec-partition.json'), JSON.stringify(partition, null, 4) + '\n');
    await writeFile(path.join(root, '.github/scripts/spec-shard-timings.json'), JSON.stringify(timings, null, 4) + '\n');
    for (const candidate of candidates) console.log(`${candidate.count} parts: ${candidate.shards.map(shard => (shard.seconds + timings.shardOverheadSeconds).toFixed(3)).join(' / ')} seconds including shard overhead`);
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
