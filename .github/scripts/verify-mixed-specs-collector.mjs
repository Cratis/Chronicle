// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Mandatory real-collector acceptance on the Linux ordinary gate: the pinned collector must
// propagate an exact child exit and an external deadline must leave no probe-owned process.
// The profiler attaches only to .NET: equivalence of this non-.NET child's launcher behavior
// remains unproven until Linux CI. Production coverage still comes only from the real specs.
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { runProcess } from './mixed-specs-process.mjs';
import { childCommand } from './run-mixed-specs.mjs';

export const expectedExit = 37;
const fail = message => { throw new Error(message); };
const probeScript = fileURLToPath(import.meta.url);
const failsafe = 120000;
const sleep = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

async function hang(pidFile) {
    const child = spawn(process.execPath, [probeScript, '--probe-child', 'sleep'], { stdio: 'inherit' });
    await once(child, 'spawn');
    const closed = once(child, 'close');
    try {
        await writeFile(pidFile, `${process.pid}\n${child.pid}\n`);
        await sleep(failsafe);
    } finally {
        child.kill('SIGKILL');
        await closed;
    }
}
async function probeChild(args) {
    switch (args[0]) {
        case 'exit':
            if (args.length !== 2) return 2;
            return args[1] === String(expectedExit) ? expectedExit : 2;
        case 'hang':
            if (args.length !== 2) return 2;
            await hang(args[1]);
            return 0;
        case 'sleep':
            if (args.length !== 1) return 2;
            await sleep(failsafe);
            return 0;
        default: return 2;
    }
}
export function collectorArgs(directory, probeArgs) {
    return ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', path.join(directory, 'probe.cobertura.xml'),
        childCommand([process.execPath, probeScript, '--probe-child', ...probeArgs])];
}
export function isAlive(pid) {
    try { process.kill(pid, 0); return true; } catch (error) { return error.code === 'EPERM'; }
}
export async function verifyExit(directory, execute = runProcess) {
    const result = await execute('dotnet', collectorArgs(directory, ['exit', String(expectedExit)]), { timeout: 60000, capture: true });
    if (result.code !== expectedExit) fail(`Collector did not propagate exact child exit ${expectedExit}; got ${result.code}`);
}
async function ownedPids(pidFile) {
    const pids = (await readFile(pidFile, 'utf8').catch(() => '')).split('\n').filter(Boolean).map(Number);
    if (pids.length !== 2 || !pids.every(Number.isSafeInteger)) fail('Deadline probe did not record both owned PIDs; execution is incomplete');
    return pids;
}
export async function verifyDeadline(directory, { execute = runProcess, alive = isAlive, deadline = 15000, grace = 10000 } = {}) {
    const pidFile = path.join(directory, 'owned.pids');
    const outcome = await execute('dotnet', collectorArgs(directory, ['hang', pidFile]), { timeout: deadline, capture: true }).then(() => 'completed', error => error.exitCode);
    if (outcome !== 124) fail(`Expected external deadline exit 124; got ${outcome}`);
    const pids = await ownedPids(pidFile);
    for (let waited = 0; pids.some(alive) && waited < grace; waited += 100) await new Promise(resolve => setTimeout(resolve, 100));
    const survivors = pids.filter(alive);
    for (const pid of survivors) try { process.kill(pid, 'SIGKILL'); } catch { /* already gone */ }
    if (survivors.length) fail(`Deadline left probe-owned processes alive: ${survivors.join(', ')}`);
}
export async function main(args = process.argv.slice(2), execute = runProcess) {
    if (args[0] === '--probe-child') { process.exitCode = await probeChild(args.slice(1)); return; }
    if (args.length) fail('Usage: node .github/scripts/verify-mixed-specs-collector.mjs');
    const directory = await mkdtemp(path.join(os.tmpdir(), 'spec-collector-probe-'));
    try {
        await verifyExit(directory, execute);
        await verifyDeadline(directory, { execute });
        console.log(`Collector acceptance passed: exact exit ${expectedExit}, deadline 124, no owned orphans`);
    } finally {
        await rm(directory, { recursive: true, force: true });
    }
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = error.exitCode ?? 1; });
}
