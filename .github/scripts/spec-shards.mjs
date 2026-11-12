// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, readdir, mkdir, writeFile, appendFile } from 'node:fs/promises';
import { readFileSync } from 'node:fs';
import { CORE_PROJECT, CORE_PARTS, coreUnitId, loadPartition, checkPartition, partitionUnits } from './core-spec-partition.mjs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { validateSourceProjects } from './mixed-specs-selection.mjs';
import { balanceJobs } from './spec-shard-balancing.mjs';

export const ORDINARY_SHARDS = 3;
export const ordinaryShardIds = () => Array.from({ length: ORDINARY_SHARDS }, (_, index) => index + 1);
const validShard = shard => ordinaryShardIds().includes(shard);
const fail = message => { throw new Error(message); };
const positive = value => typeof value === 'number' && Number.isFinite(value) && value > 0;
function projectIdentity(project) {
    if (typeof project !== 'string') return false;
    if (Array.from({ length: CORE_PARTS }, (_, index) => coreUnitId(index + 1)).includes(project)) return true;
    return /^(Source|Integration|Benchmarks)\/(?:[\w.-]+\/)*[\w.-]+\.csproj$/.test(project) && !project.split('/').includes('..');
}
function unique(projects, label) {
    if (!Array.isArray(projects)) fail(`${label}: expected project array`);
    const counts = new Map();
    for (const project of projects) {
        if (!projectIdentity(project)) fail(`${label}: invalid project ${project}`);
        counts.set(project, (counts.get(project) ?? 0) + 1);
        if (counts.get(project) !== 1) fail(`${label}: duplicate project ${project}`);
    }
    return counts;
}
export function sameProjects(actual, expected, label) {
    const counts = unique(actual, label);
    unique(expected, `${label} expected`);
    if (counts.size !== expected.length || expected.some(p => counts.get(p) !== 1)) fail(`${label}: project completion/partition mismatch`);
}
function validateTimingRecord(record, projects, weights) {
    if (!positive(record?.seconds)) fail(`Invalid timing seconds for ${record?.project}`);
    if (!projects.includes(record.project)) fail(`Stale or unknown timing project ${record.project}`);
    if (weights.has(record.project)) fail(`Duplicate timing project ${record.project}`);
}
export function validateTimings(data, projects) {
    const validSchema = [data?.version === 1, positive(data?.defaultSeconds), Array.isArray(data?.measurements)].every(Boolean);
    if (!validSchema) fail('Invalid timing schema/default seconds');
    const weights = new Map();
    for (const record of data.measurements) {
        validateTimingRecord(record, projects, weights);
        weights.set(record.project, record.seconds);
    }
    return weights;
}
export function expandCore(projects, timings, parts) {
    const weights = validateTimings(timings, projects);
    const expanded = projects.flatMap(project => project === CORE_PROJECT ? parts.map(part => coreUnitId(part.part)) : [project]);
    const defaults = new Set(projects.filter(project => project !== CORE_PROJECT && !weights.has(project)));
    if (projects.includes(CORE_PROJECT)) {
        if (!positive(timings.core?.fixedSeconds) || !positive(timings.core?.variableSeconds)) fail('Missing/invalid Core wall-time model');
        const total = parts.reduce((sum, part) => sum + part.weight, 0);
        for (const part of parts) {
            weights.set(coreUnitId(part.part), timings.core.fixedSeconds + timings.core.variableSeconds * part.weight / total);
        }
    }
    return { projects: expanded, weights, defaults };
}
export function planShards(projects, timings, parts = partitionUnits(JSON.parse(readFileSync(new URL('./core-spec-partition.json', import.meta.url), 'utf8')).units)) {
    unique(projects, 'Ordinary selection');
    if (projects.length < ORDINARY_SHARDS) fail(`Expected ${ORDINARY_SHARDS} nonempty ordinary shards`);
    const expanded = expandCore(projects, timings, parts);
    projects = expanded.projects;
    const { weights, defaults } = expanded;
    const weight = p => weights.get(p) ?? timings.defaultSeconds;
    const overhead = timings.shardOverheadSeconds ?? 0;
    if (!Number.isFinite(overhead) || overhead < 0) fail('Invalid shard overhead');
    const jobs = projects.map(project => ({ project, seconds: weight(project), core: project.includes('#core-part-') }));
    const shards = balanceJobs(jobs, ORDINARY_SHARDS).map(shard => ({ ...shard,
        seconds: shard.seconds + overhead, defaulted: shard.projects.filter(project => defaults.has(project))
    }));
    sameProjects(shards.flatMap(s => s.projects), projects, 'Shard union');
    if (shards.some(s => !s.projects.length)) fail(`Expected ${ORDINARY_SHARDS} nonempty ordinary shards`);
    return shards;
}
export async function sourcePlan(root, timings) {
    const source = await validateSourceProjects(root);
    const parts = await checkPartition(root, await loadPartition());
    return planShards(source.ordinary, timings ?? JSON.parse(await readFile(new URL('./spec-shard-timings.json', import.meta.url), 'utf8')), parts);
}
export function parseShardArgs(args) {
    if (!args.length) return null;
    const values = new Map();
    for (let i = 0; i < args.length; i += 2) {
        const key = args[i];
        const validPair = [['--shard', '--shards'].includes(key), !values.has(key), Boolean(args[i + 1])].every(Boolean);
        if (!validPair) fail(`Usage: --shard N --shards ${ORDINARY_SHARDS}`);
        values.set(key, args[i + 1]);
    }
    const shard = values.get('--shard');
    const validSelection = [values.size === 2, values.get('--shards') === String(ORDINARY_SHARDS),
        ordinaryShardIds().some(id => String(id) === shard)].every(Boolean);
    if (!validSelection) fail(`Expected --shard 1..${ORDINARY_SHARDS} and --shards ${ORDINARY_SHARDS}`);
    return Number(values.get('--shard'));
}
export function receiptContext(env = process.env) {
    const keys = ['GITHUB_SHA', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT'];
    if (env.GITHUB_ACTIONS === 'true' && keys.some(key => !env[key])) fail('Missing GitHub receipt context');
    return { sha: env.GITHUB_SHA ?? 'local', runId: env.GITHUB_RUN_ID ?? 'local', runAttempt: env.GITHUB_RUN_ATTEMPT ?? 'local' };
}
export function receiptFor(shard, completed, context = receiptContext()) {
    if (!validShard(shard.shard) || !shard.projects.length) fail('Invalid/empty receipt shard');
    sameProjects(completed, shard.projects, `Shard ${shard.shard} completion`);
    return { version: 1, shard: shard.shard, shards: ORDINARY_SHARDS, context, projects: [...shard.projects], completed: [...completed] };
}
function contextMatches(actual, expected) {
    if (!actual) return false;
    return ['sha', 'runId', 'runAttempt'].every(key => [typeof actual[key] === 'string', Boolean(actual[key]), actual[key] === expected[key]].every(Boolean));
}
function verifyReceipt(receipt, plan, context) {
    const validIdentity = [receipt?.version === 1, receipt?.shards === ORDINARY_SHARDS, validShard(receipt?.shard)].every(Boolean);
    if (!validIdentity) fail('Invalid shard receipt version/identity/count');
    if (!contextMatches(receipt.context, context)) fail(`Shard ${receipt.shard}: stale receipt context`);
    const expected = plan.find(s => s.shard === receipt.shard).projects;
    sameProjects(receipt.projects, expected, `Shard ${receipt.shard} receipt plan`);
    sameProjects(receipt.completed, expected, `Shard ${receipt.shard} receipt completion`);
}
function producerContexts(producers, context) {
    if (!Array.isArray(producers) || producers.length !== ORDINARY_SHARDS) fail(`Expected ${ORDINARY_SHARDS} successful producer attempts`);
    if (new Set(producers.map(p => p?.shard)).size !== ORDINARY_SHARDS) fail('Duplicate producer shard');
    return producers.map(producer => {
        const valid = [validShard(producer?.shard), /^[1-9]\d*$/.test(producer?.runAttempt ?? ''),
            Number(producer?.runAttempt) <= Number(context.runAttempt)].every(Boolean);
        if (!valid) fail('Invalid/stale producer attempt');
        return { shard: producer.shard, ...context, runAttempt: producer.runAttempt };
    });
}
const hasOneEntryPerShard = list => Array.isArray(list) && list.length === ORDINARY_SHARDS;
const validPlanShard = shard => [validShard(shard?.shard), Boolean(shard?.projects?.length)].every(Boolean);
function validatePlan(plan) {
    if (!hasOneEntryPerShard(plan)) fail('Invalid shard plan count');
    if (new Set(plan.map(s => s?.shard)).size !== ORDINARY_SHARDS) fail('Duplicate plan shard');
    if (!plan.every(validPlanShard)) fail('Invalid/empty plan shard');
    unique(plan.flatMap(s => s.projects), 'Shard plan');
}
export function verifyReceipts(receipts, plan, context = receiptContext(), producers) {
    validatePlan(plan);
    if (!Array.isArray(receipts) || receipts.length !== ORDINARY_SHARDS) fail(`Expected exactly ${ORDINARY_SHARDS} shard receipts`);
    const contexts = producers ? producerContexts(producers, context) : ordinaryShardIds().map(shard => ({ shard, ...context }));
    const seen = new Set();
    for (const receipt of receipts) {
        verifyReceipt(receipt, plan, contexts.find(c => c.shard === receipt?.shard) ?? context);
        if (seen.has(receipt.shard)) fail(`Duplicate shard receipt ${receipt.shard}`);
        seen.add(receipt.shard);
    }
    sameProjects(receipts.flatMap(r => r.completed), plan.flatMap(s => s.projects), 'Global receipt completion');
}
async function receiptFiles(directory) {
    const files = [];
    for (const entry of await readdir(directory, { withFileTypes: true })) {
        const file = path.join(directory, entry.name);
        if (entry.isSymbolicLink()) fail(`Unsupported receipt symlink ${file}`);
        if (entry.isDirectory()) files.push(...await receiptFiles(file));
        else if (entry.name === 'shard-receipt.json') files.push(file);
    }
    return files;
}
function verifyEvidencePaths(evidence, context, directory, files) {
    if (!contextMatches(evidence?.context, context)) fail('Stale producer evidence context');
    producerContexts(evidence.producers, context);
    const expected = evidence.producers.map(p => path.join(directory, p.artifactName, 'shard-receipt.json'));
    if (files.some(file => !expected.includes(file))) fail('Unexpected producer artifact receipt path');
}
export async function verifyResults(directory, plan, context = receiptContext(), evidence) {
    const files = await receiptFiles(directory);
    if (files.length !== ORDINARY_SHARDS) fail(`Expected exactly ${ORDINARY_SHARDS} shard receipt files; found ${files.length}`);
    if (evidence !== undefined) verifyEvidencePaths(evidence, context, directory, files);
    const receipts = await Promise.all(files.map(async file => JSON.parse(await readFile(file, 'utf8'))));
    verifyReceipts(receipts, plan, context, evidence?.producers);
    console.log(`Verified ${ORDINARY_SHARDS} completed shard receipts: ${receipts.flatMap(r => r.completed).length} ordinary projects exactly once`);
}
async function selectArtifacts(file, context) {
    // API evidence is fetched afresh by the aggregate, never accepted from worker artifacts.
    const { loadProducerEvidence } = await import('./run-mixed-specs.mjs');
    const evidence = await loadProducerEvidence(context);
    await mkdir(path.dirname(file), { recursive: true });
    await writeFile(file, JSON.stringify(evidence) + '\n');
    if (process.env.GITHUB_OUTPUT) await appendFile(process.env.GITHUB_OUTPUT, `artifact_ids=${evidence.producers.map(p => p.artifactId).join(',')}\n`);
}
async function readProducerEvidence(args, root) {
    if (args.length === 4 && args[2] !== '--producers') fail('Expected --producers FILE');
    if (process.env.GITHUB_ACTIONS === 'true' && args.length !== 4) fail('Missing successful producer evidence');
    return args.length === 4 ? JSON.parse(await readFile(path.resolve(root, args[3]), 'utf8')) : undefined;
}
async function verifyArtifactResults(args, root, plan, context) {
    const evidence = await readProducerEvidence(args, root);
    return verifyResults(path.resolve(root, args[1]), plan, context, evidence);
}
export async function main(args = process.argv.slice(2), { root = process.cwd() } = {}) {
    const context = receiptContext();
    if (args.length === 2 && args[0] === '--select-producers') return selectArtifacts(path.resolve(root, args[1]), context);
    const dryRun = args.length === 1 && args[0] === '--dry-run';
    const verify = args[0] === '--verify-results' && [2, 4].includes(args.length);
    if (!dryRun && !verify) fail('Usage: spec-shards.mjs --dry-run | --select-producers FILE | --verify-results DIRECTORY [--producers FILE]');
    const plan = await sourcePlan(root);
    if (verify) return verifyArtifactResults(args, root, plan, context);
    const source = await validateSourceProjects(root);
    console.log(`Discovered/classified executable=${source.executable.length}; ordinary=${source.ordinary.length}; SQL=${source.sql.length}; integration=${source.integration.length}; shipping=${source.shipping.length}`);
    for (const shard of plan) {
        console.log(`Shard ${shard.shard}: ${shard.projects.length} projects; estimated ${shard.seconds.toFixed(2)} seconds (hypothesis, not CI feedback)`);
        for (const project of shard.projects) console.log(`  ${project}`);
        console.log(`  Defaulted weights (60 seconds): ${shard.defaulted.join(', ') || 'none'}`);
    }
    console.log(`Exact-once partition validated: ${source.ordinary.length} ordinary projects, ${ORDINARY_SHARDS} nonempty shards`);
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
