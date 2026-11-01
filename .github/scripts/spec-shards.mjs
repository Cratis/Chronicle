// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { validateSourceProjects } from './mixed-specs-selection.mjs';

const fail = message => { throw new Error(message); };
const ordinal = (a, b) => a < b ? -1 : a > b ? 1 : 0;
const positive = value => typeof value === 'number' && Number.isFinite(value) && value > 0;
function projectIdentity(project) {
    if (typeof project !== 'string') return false;
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
export function planShards(projects, timings) {
    unique(projects, 'Ordinary selection');
    if (projects.length < 2) fail('Expected two nonempty ordinary shards');
    const weights = validateTimings(timings, projects);
    const weight = p => weights.get(p) ?? timings.defaultSeconds;
    const sorted = [...projects].sort((a, b) => weight(b) - weight(a) || ordinal(a, b));
    const shards = [1, 2].map(shard => ({ shard, projects: [], seconds: 0, defaulted: [] }));
    for (const project of sorted) {
        const shard = shards[0].seconds <= shards[1].seconds ? shards[0] : shards[1];
        shard.projects.push(project);
        shard.seconds += weight(project);
        if (!weights.has(project)) shard.defaulted.push(project);
    }
    sameProjects(shards.flatMap(s => s.projects), projects, 'Shard union');
    if (shards.some(s => !s.projects.length)) fail('Expected two nonempty ordinary shards');
    return shards;
}
export async function sourcePlan(root, timings) {
    const source = await validateSourceProjects(root);
    return planShards(source.ordinary, timings ?? JSON.parse(await readFile(new URL('./spec-shard-timings.json', import.meta.url), 'utf8')));
}
export function parseShardArgs(args) {
    if (!args.length) return null;
    const values = new Map();
    for (let i = 0; i < args.length; i += 2) {
        const key = args[i];
        const validPair = [['--shard', '--shards'].includes(key), !values.has(key), Boolean(args[i + 1])].every(Boolean);
        if (!validPair) fail('Usage: --shard N --shards 2');
        values.set(key, args[i + 1]);
    }
    const validSelection = [values.size === 2, values.get('--shards') === '2', /^[12]$/.test(values.get('--shard') ?? '')].every(Boolean);
    if (!validSelection) fail('Expected --shard 1 or 2 and --shards 2');
    return Number(values.get('--shard'));
}
export function receiptContext(env = process.env) {
    const keys = ['GITHUB_SHA', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT'];
    if (env.GITHUB_ACTIONS === 'true' && keys.some(key => !env[key])) fail('Missing GitHub receipt context');
    return { sha: env.GITHUB_SHA ?? 'local', runId: env.GITHUB_RUN_ID ?? 'local', runAttempt: env.GITHUB_RUN_ATTEMPT ?? 'local' };
}
export function receiptFor(shard, completed, context = receiptContext()) {
    sameProjects(completed, shard.projects, `Shard ${shard.shard} completion`);
    return { version: 1, shard: shard.shard, shards: 2, context, projects: [...shard.projects], completed: [...completed] };
}
function contextMatches(actual, expected) {
    if (!actual) return false;
    return ['sha', 'runId', 'runAttempt'].every(key => [typeof actual[key] === 'string', Boolean(actual[key]), actual[key] === expected[key]].every(Boolean));
}
function verifyReceipt(receipt, plan, context) {
    const validIdentity = [receipt?.version === 1, receipt?.shards === 2, [1, 2].includes(receipt?.shard)].every(Boolean);
    if (!validIdentity) fail('Invalid shard receipt version/identity/count');
    if (!contextMatches(receipt.context, context)) fail(`Shard ${receipt.shard}: stale receipt context`);
    const expected = plan.find(s => s.shard === receipt.shard).projects;
    sameProjects(receipt.projects, expected, `Shard ${receipt.shard} receipt plan`);
    sameProjects(receipt.completed, expected, `Shard ${receipt.shard} receipt completion`);
}
export function verifyReceipts(receipts, plan, context = receiptContext()) {
    if (!Array.isArray(receipts) || receipts.length !== 2) fail('Expected exactly two shard receipts');
    const seen = new Set();
    for (const receipt of receipts) {
        verifyReceipt(receipt, plan, context);
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
export async function verifyResults(directory, plan, context = receiptContext()) {
    const files = await receiptFiles(directory);
    if (files.length !== 2) fail(`Expected exactly two shard receipt files; found ${files.length}`);
    const receipts = await Promise.all(files.map(async file => JSON.parse(await readFile(file, 'utf8'))));
    verifyReceipts(receipts, plan, context);
    console.log(`Verified 2 completed shard receipts: ${receipts.flatMap(r => r.completed).length} ordinary projects exactly once`);
}
export async function main(args = process.argv.slice(2), { root = process.cwd() } = {}) {
    if (!(args.length === 1 && args[0] === '--dry-run') && !(args.length === 2 && args[0] === '--verify-results' && args[1])) fail('Usage: spec-shards.mjs --dry-run | --verify-results DIRECTORY');
    const plan = await sourcePlan(root);
    if (args[0] === '--verify-results') return verifyResults(path.resolve(root, args[1]), plan);
    const source = await validateSourceProjects(root);
    console.log(`Discovered/classified executable=${source.executable.length}; ordinary=${source.ordinary.length}; SQL=${source.sql.length}; integration=${source.integration.length}; shipping=${source.shipping.length}`);
    for (const shard of plan) {
        console.log(`Shard ${shard.shard}: ${shard.projects.length} projects; estimated ${shard.seconds.toFixed(2)} seconds (hypothesis, not CI feedback)`);
        for (const project of shard.projects) console.log(`  ${project}`);
        console.log(`  Defaulted weights (60 seconds): ${shard.defaulted.join(', ') || 'none'}`);
    }
    console.log(`Exact-once partition validated: ${source.ordinary.length} ordinary projects, 2 nonempty shards`);
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
