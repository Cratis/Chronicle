// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawn } from 'node:child_process';
import { readFile, readdir, appendFile, writeFile } from 'node:fs/promises';
import { basename, dirname, resolve } from 'node:path';
import { randomUUID } from 'node:crypto';
import { performance } from 'node:perf_hooks';
import { fileURLToPath } from 'node:url';
import { markdown, parseRecords, summarize } from './integration-timing-summary.mjs';

function options(args) {
    const values = new Map();
    for (let index = 0; index < args.length; index += 2) {
        if (!args[index].startsWith('--') || !args[index + 1]) throw new Error('Expected --option value');
        const key = args[index].slice(2);
        values.set(key, [...(values.get(key) ?? []), args[index + 1]]);
    }
    return values;
}

export async function replaySidecars(path, write) {
    if (!path) return;
    const directory = dirname(resolve(path));
    const name = basename(path);
    for (const entry of (await readdir(directory)).filter(entry => entry.startsWith(`${name}.`) && entry.endsWith('.jsonl')).sort()) {
        write(await readFile(resolve(directory, entry), 'utf8'));
    }
}

function waitForExit(child) {
    const forward = signal => child.kill(signal);
    const interrupt = () => forward('SIGINT');
    const terminate = () => forward('SIGTERM');
    process.on('SIGINT', interrupt);
    process.on('SIGTERM', terminate);
    return new Promise((resolveExit, reject) => {
        child.once('error', reject);
        child.once('close', (code, signal) => resolveExit({ code, signal }));
    }).finally(() => {
        process.off('SIGINT', interrupt);
        process.off('SIGTERM', terminate);
    });
}

export async function run(phase, command, args, env = process.env) {
    if (!['build', 'test'].includes(phase) || !command) throw new Error('run requires --phase build|test -- command arguments');
    const instance = randomUUID();
    const started = performance.now();
    const childEnv = { ...env, CHRONICLE_INTEGRATION_TIMINGS: '1',
        CHRONICLE_INTEGRATION_RUN_ID: env.CHRONICLE_INTEGRATION_RUN_ID ?? randomUUID() };
    // Caller supplies a unique prefix per invocation so retry/parallel logs cannot be replayed as new work.
    const child = spawn(command, args, { env: childEnv, stdio: ['inherit', 'pipe', 'pipe'] });
    child.stdout.pipe(process.stdout, { end: false });
    child.stderr.pipe(process.stderr, { end: false });
    const result = await waitForExit(child);
    let completeness = 'complete';
    try {
        await replaySidecars(childEnv.CHRONICLE_INTEGRATION_TIMINGS_FILE, value => process.stdout.write(value));
    } catch {
        completeness = 'partial';
        process.stderr.write('CHRONICLE_INTEGRATION_TIMING_WARNING sidecar replay unavailable\n');
    }
    const record = { schema: 1, run: childEnv.CHRONICLE_INTEGRATION_RUN_ID, sha: env.GITHUB_SHA ?? 'unavailable',
        process: process.pid, instance, sequence: 1, phase, elapsedMs: performance.now() - started,
        outcome: result.code === 0 ? 'success' : 'failure', completeness, exitCode: result.code, signal: result.signal,
        generation: phase === 'test' && args.includes('--no-build') ? 'not-applicable' : 'unavailable' };
    const line = `CHRONICLE_INTEGRATION_RUN ${JSON.stringify(record)}\n`;
    process.stdout.write(line);
    if (env.CHRONICLE_INTEGRATION_TIMINGS_FILE) {
        try { await appendFile(`${env.CHRONICLE_INTEGRATION_TIMINGS_FILE}.wrapper.jsonl`, line); }
        catch { process.stderr.write('CHRONICLE_INTEGRATION_TIMING_WARNING wrapper sidecar unavailable\n'); }
    }
    return result;
}

export function buildTargets(text) {
    // MSBuild's PerformanceSummary reports inclusive target worker time, not wall time.
    return [...text.matchAll(/^\s*([\d.]+) ms\s+(\S*(?:GenerateProtoFiles|GrpcClients|GenerateKernel)\S*)\s+(\d+) calls\s*$/gm)]
        .map(match => ({ target: match[2], elapsedMs: Number(match[1]), count: Number(match[3]), accounting: 'inclusive-worker-subset' }));
}

function parseTrxDuration(attributes) {
    const value = /\bduration=["']([^"']+)["']/.exec(attributes)?.[1];
    const parts = /^(?:(\d+)\.)?(\d+):([0-5]\d):([0-5]\d(?:\.\d+)?)$/.exec(value ?? '');
    if (!parts) return null;
    return ((Number(parts[1] ?? 0) * 24 + Number(parts[2])) * 3600 + Number(parts[3]) * 60 + Number(parts[4])) * 1000;
}

export function trxDurations(text) {
    const errors = [];
    const durations = [];
    for (const match of text.matchAll(/<(?:\w+:)?UnitTestResult\b([^>]*?)\/?\s*>/g)) {
        const duration = parseTrxDuration(match[1]);
        if (duration === null) errors.push('TRX result has no valid duration');
        else durations.push(duration);
    }
    durations.sort((a, b) => a - b);
    if (!durations.length) errors.push('No TRX test durations found');
    const totalInclusiveMs = durations.reduce((sum, duration) => sum + duration, 0);
    const percentile = fraction => durations[Math.max(0, Math.ceil(durations.length * fraction) - 1)] ?? null;
    return { errors, count: durations.length, totalInclusiveMs,
        meanMs: durations.length ? totalInclusiveMs / durations.length : null,
        p50Ms: percentile(0.5), p95Ms: percentile(0.95), maxMs: durations.at(-1) ?? null,
        accounting: 'inclusive-test-duration-not-wall' };
}

async function collectInputs(opts) {
    const inputs = [];
    const targets = [];
    const tests = [];
    for (const kind of ['log', 'build-log', 'trx', 'jsonl']) {
        for (const path of new Set(opts.get(kind) ?? [])) {
            const text = await readFile(path, 'utf8');
            inputs.push(parseRecords(text, kind === 'trx'));
            if (kind === 'build-log') targets.push(...buildTargets(text));
            if (kind === 'trx') tests.push({ source: basename(path), ...trxDurations(text) });
        }
    }
    return { inputs, targets, tests };
}

async function summarizeCommand(opts) {
    const { inputs, targets, tests } = await collectInputs(opts);
    const summary = { ...summarize(inputs, opts.get('require-phase') ?? []), buildTargets: targets, testDurations: tests };
    if (!opts.has('require-phase')) summary.errors.push('Required phase inventory unavailable; supply --require-phase for the selected producer');
    summary.errors.push(...tests.flatMap(test => test.errors));
    if (summary.errors.length) summary.completeness = 'partial';
    const table = markdown(summary) + tests.map(test => `TRX ${test.source}: ${test.count} tests; inclusive test time ${test.totalInclusiveMs.toFixed(1)} ms (not wall time).\n`).join('');
    process.stdout.write(table);
    if (opts.has('markdown')) await writeFile(opts.get('markdown')[0], table);
    if (opts.has('json')) await writeFile(opts.get('json')[0], `${JSON.stringify(summary, null, 2)}\n`);
    process.exitCode = summary.completeness === 'complete' ? 0 : 1;
}

async function runCommand(args) {
    const separator = args.indexOf('--');
    if (separator < 0) throw new Error('Missing command separator --');
    const opts = options(args.slice(0, separator));
    const result = await run(opts.get('phase')?.[0], args[separator + 1], args.slice(separator + 2));
    if (result.signal) process.kill(process.pid, result.signal);
    else process.exitCode = result.code ?? 1;
}

async function main([action, ...args]) {
    if (action === 'run') return runCommand(args);
    if (action === 'summarize') return summarizeCommand(options(args));
    throw new Error('Expected run or summarize');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    main(process.argv.slice(2)).catch(() => {
        // Do not print arguments, paths or child error messages: they may carry environment details.
        process.stderr.write('Integration timing command failed; report is incomplete\n');
        process.exitCode = 1;
    });
}
