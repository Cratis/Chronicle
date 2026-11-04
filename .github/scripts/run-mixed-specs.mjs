// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Ordinary dispatch and the shared executable invocation used by SQL/integration shards.
// Source contracts are mocks, NOT collector acceptance: Linux production hits and real
// negative/deadline/process-tree proof remain required in the coherent owner candidate.
import { readFile, readdir, mkdir, mkdtemp, access, writeFile, unlink } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { runProcess } from './mixed-specs-process.mjs';
import { decode, validateTrx, validateCoverage, coverageFiles } from './mixed-specs-results.mjs';
import { ordinaryProjects, ordinaryFilter, inventory } from './mixed-specs-selection.mjs';
import { parseShardArgs, sourcePlan, sameProjects, receiptFor } from './spec-shards.mjs';

// Preserve the existing import surface for contract consumers.
export { runProcess } from './mixed-specs-process.mjs';
export { parseXml, validateTrx, validateCoverage } from './mixed-specs-results.mjs';
export { v3Projects, shippingProjects, shippingRunners, ordinaryFilter, validateSelection, inventory } from './mixed-specs-selection.mjs';
const fail = message => { throw new Error(message); };

// dotnet-coverage documents ONE child command string. Quote each argument for
// .NET command-line parsing, never for a shell; reject control characters.
// dotnet-coverage takes the first token of the child command literally, quotes included, so the
// executable stays unquoted and must be a plain path. Every argument is quoted.
export function childCommand(executable, args) {
    if (!/^[\w./+-]+$/.test(executable)) fail(`Child executable must be a plain path without spaces or quotes: ${executable}`);
    return [executable, ...args.map(value => {
        if (/[\x00-\x1f\x7f]/.test(value)) fail('Control character in child argument');
        return `"${value.replace(/(\\*)"/g, '$1$1\\"').replace(/(\\+)$/g, '$1$1')}"`;
    })].join(' ');
}
export function invocation(info, { directory, filter, timeout, coverage = true }) {
    if (info.runner !== 'v3') fail(`${info.project}: executable v3 runner required`);
    const args = [info.dll, ...(filter ? ['-filterVSTest', filter] : []), '-result-trx', path.join(directory, 'results.trx')];
    if (!coverage) return { command: 'dotnet', args, timeout };
    return { command: 'dotnet', args: ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', path.join(directory, 'coverage.cobertura.xml'), childCommand('dotnet', args)], timeout };
}
export async function runProject(info, options, execute = runProcess) {
    const call = invocation(info, options);
    await access(path.resolve(options.root, info.dll));
    if ((await readdir(options.directory)).length) fail(`${info.project}: results directory must be fresh and empty`);
    const child = await execute(call.command, call.args, { cwd: options.root, timeout: call.timeout });
    if (child.code !== 0) throw Object.assign(new Error(`${info.project}: child exited ${child.code}`), { exitCode: child.code });
    const identities = validateTrx(await readFile(path.join(options.directory, 'results.trx'), 'utf8'));
    if (options.coverage === false) {
        console.log(`${info.project}: ${identities.length} results`);
        return;
    }
    const files = await coverageFiles(options.directory);
    if (files.length !== 1) fail(`${info.project}: expected one fresh Cobertura file`);
    const production = await validateCoverage(await readFile(files[0], 'utf8'), options.root, info.coverageRoots);
    console.log(`${info.project}: ${identities.length} results; production hit ${production}`);
}
function positiveId(value) { return Number.isSafeInteger(value) && value > 0; }
function apiPageRecords(page, key) {
    if (!Array.isArray(page?.[key])) fail(`Malformed producer API page: ${key}`);
    return page[key];
}
function apiRecords(pages, key) {
    if (!Array.isArray(pages) || !pages.length) fail(`Missing producer API pages: ${key}`);
    const records = pages.flatMap(page => apiPageRecords(page, key));
    if (pages.some(page => page.total_count !== records.length)) fail(`Incomplete producer API pagination: ${key}`);
    const ids = records.map(record => record?.id);
    const validIds = [ids.every(positiveId), new Set(ids).size === ids.length].every(Boolean);
    if (!validIds) fail(`Invalid/duplicate producer API identities: ${key}`);
    return records;
}
function producerCandidates(jobs, shard) {
    const base = `specs-other (${shard})`;
    const candidates = jobs.filter(job => typeof job.name === 'string' && (job.name === base || job.name.endsWith(` / ${base}`)));
    // With the same base suffix, distinct full names identify distinct caller prefixes.
    if (new Set(candidates.map(job => job.name)).size > 1) fail(`Ambiguous producer caller: shard ${shard}`);
    return candidates;
}
function producerJob(jobs, shard, context) {
    const candidates = producerCandidates(jobs, shard);
    for (const job of candidates) {
        const valid = [job.run_id === Number(context.runId), positiveId(job.run_attempt),
            job.run_attempt <= Number(context.runAttempt), typeof job.head_sha === 'string', Boolean(job.head_sha)].every(Boolean);
        if (!valid) fail(`Invalid producer job context: shard ${shard}`);
    }
    const attempts = candidates.map(job => job.run_attempt);
    if (new Set(attempts).size !== attempts.length) fail(`Duplicate producer attempts: shard ${shard}`);
    const latest = candidates.find(job => job.run_attempt === Math.max(...attempts));
    if (latest?.status !== 'completed' || latest.conclusion !== 'success') fail(`Missing successful latest producer: shard ${shard}`);
    return latest;
}
function producerArtifact(artifacts, job, shard, context) {
    const artifactName = `coverage-results-specs-other-${shard}`;
    const matches = artifacts.filter(artifact => artifact.name === artifactName);
    if (matches.length !== 1) fail(`Missing/duplicate producer artifact: ${artifactName}`);
    const artifact = matches[0];
    const created = Date.parse(artifact.created_at);
    const valid = [artifact.expired === false, artifact.workflow_run?.id === Number(context.runId),
        artifact.workflow_run?.head_sha === job.head_sha, Number.isFinite(created),
        created >= Date.parse(job.started_at), created <= Date.parse(job.completed_at)].every(Boolean);
    if (!valid) fail(`Stale or invalid producer artifact: ${artifactName}`);
    return { shard, runAttempt: String(job.run_attempt), artifactId: artifact.id, artifactName };
}
export function selectProducerArtifacts(jobPages, artifactPages, context) {
    const jobs = apiRecords(jobPages, 'jobs');
    const artifacts = apiRecords(artifactPages, 'artifacts');
    return { context, producers: [1, 2].map(shard => producerArtifact(artifacts, producerJob(jobs, shard, context), shard, context)) };
}
async function producerApiPages(endpoint, context, env, execute) {
    const result = await execute('gh', ['api', '--paginate', '--slurp', `repos/${env.GITHUB_REPOSITORY}/actions/runs/${context.runId}/${endpoint}`], { capture: true, timeout: 60000 });
    if (result.code !== 0) fail(`Producer evidence API failed (${result.code})`);
    return JSON.parse(result.stdout);
}
export async function loadProducerEvidence(context, { env = process.env, execute = runProcess } = {}) {
    if (!/^[\w.-]+\/[\w.-]+$/.test(env.GITHUB_REPOSITORY ?? '')) fail('Missing/invalid producer repository');
    if (!/^[1-9]\d*$/.test(context.runId) || !/^[1-9]\d*$/.test(context.runAttempt)) fail('Invalid producer run/attempt');
    const pages = [];
    for (const endpoint of ['jobs?filter=all&per_page=100', 'artifacts?per_page=100']) {
        pages.push(await producerApiPages(endpoint, context, env, execute));
    }
    return selectProducerArtifacts(...pages, context);
}
export async function main(args = process.argv.slice(2), { root = process.cwd(), execute = runProcess } = {}) {
    const shardId = parseShardArgs(args);
    const receiptPath = path.join(root, 'coverage-results', 'shard-receipt.json');
    if (shardId) await unlink(receiptPath).catch(error => { if (error.code !== 'ENOENT') throw error; });
    const settings = await readFile(path.join(root, 'specs.runsettings'), 'utf8');
    const timeouts = [...settings.matchAll(/<TestSessionTimeout>(\d+)<\/TestSessionTimeout>/g)];
    const filters = [...settings.matchAll(/<TestCaseFilter>([^<]+)<\/TestCaseFilter>/g)];
    if (timeouts.length !== 1 || filters.length !== 1 || Number(timeouts[0][1]) <= 0) fail('Unsupported runsettings: expected one deadline and filter');
    const filter = `(${ordinaryFilter})&(${decode(filters[0][1])})`;
    const infos = await inventory(root, execute); // All 31 spec projects + 3 shipping packages, before launching any test.
    await mkdir(path.join(root, 'coverage-results'), { recursive: true });
    const output = await mkdtemp(path.join(root, 'coverage-results', 'mixed-'));
    const ordinary = ordinaryProjects(infos);
    const shard = shardId ? (await sourcePlan(root)).find(s => s.shard === shardId) : null;
    const selected = shard ? shard.projects.map(project => ordinary.find(info => info.project === project)) : ordinary;
    if (shard) sameProjects(selected.map(info => info?.project), shard.projects, 'Evaluated shard');
    const completed = [];
    for (const info of selected) {
        const directory = path.join(output, `${info.assemblyName}-${info.tfm}`);
        await mkdir(directory); // No reuse, caching or duplicate output assembly identities.
        const started = performance.now();
        await runProject(info, { root, directory, filter, timeout: Number(timeouts[0][1]) }, execute);
        completed.push(info.project);
        console.log(`Spec project completed: ${info.project}; ${((performance.now() - started) / 1000).toFixed(3)} seconds including validation`);
    }
    if (shard) await writeFile(receiptPath, JSON.stringify(receiptFor(shard, completed), null, 2) + '\n', { flag: 'wx' });
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = error.exitCode ?? 1; });
}
