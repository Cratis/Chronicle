// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Extraction, preflight and cleanup authoring for the #5518 released-admission proof closure.
// This file deliberately contains no package-smoke run logic, consumer inputs, builds or service wiring.

import { spawn } from 'node:child_process';
import { access, mkdir, readdir, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';

const marker = '.5518-extraction';
const manifestName = 'extraction-manifest.json';
const digestPattern = /^[a-z0-9][a-z0-9._\/-]*(:[A-Za-z0-9._-]+)?@sha256:[0-9a-f]{64}$/;
const platforms = new Set(['linux/amd64', 'linux/arm64']);
const frameworks = ['Microsoft.NETCore.App', 'Microsoft.AspNetCore.App'];
const maxCapture = 64 * 1024;
const caps = { tool: 60000, create: 60000, copy: 120000, remove: 60000, overall: 300000 };

function fail(message, exitCode = 1) {
    const error = new Error(message);
    error.exitCode = exitCode;
    return error;
}

/**
 * Runs one bounded child process. Output is capped; on timeout the child is killed and exit code 124 is reported.
 * A non-zero child exit code is preserved on the rejected error.
 */
export function runChild(command, args, timeoutMs) {
    return new Promise((resolve, reject) => {
        const child = spawn(command, args, { stdio: ['ignore', 'pipe', 'pipe'] });
        let stdout = '', stderr = '', timedOut = false, settled = false;
        const append = (current, chunk) => (current + chunk).slice(-maxCapture);
        child.stdout.on('data', chunk => { stdout = append(stdout, chunk); });
        child.stderr.on('data', chunk => { stderr = append(stderr, chunk); });
        const timer = setTimeout(() => {
            timedOut = true;
            child.kill('SIGKILL');
        }, timeoutMs);
        const finish = (action) => {
            if (settled) return;
            settled = true;
            clearTimeout(timer);
            action();
        };
        child.on('error', error => finish(() => reject(fail(`Could not start ${command}: ${error.message}`, 127))));
        child.on('close', (code, signal) => finish(() => {
            if (timedOut) return reject(fail(`${command} ${args[0] ?? ''} timed out after ${timeoutMs}ms`, 124));
            if (code !== 0) return reject(fail(`${command} ${args[0] ?? ''} failed (${code ?? signal}): ${stderr.trim()}`, code ?? 1));
            resolve({ stdout, stderr });
        }));
    });
}

function remaining(deadline, cap) {
    const left = deadline - Date.now();
    if (left <= 0) throw fail('Extraction deadline exceeded', 124);
    return Math.min(left, cap);
}

export function validateInputs({ image, platform, destination }) {
    if (!image || !digestPattern.test(image)) throw fail('--image must be a digest-pinned reference (name@sha256:<64 hex>)');
    if (!platforms.has(platform)) throw fail(`--platform must be one of ${[...platforms].join(', ')}`);
    validateDestination(destination);
}

export function validateDestination(destination) {
    if (!destination || !path.isAbsolute(destination)) throw fail('--destination must be an absolute path');
    if (path.parse(destination).root === destination || path.dirname(destination) === path.parse(destination).root) {
        throw fail('--destination is too close to the filesystem root');
    }
}

async function exists(target) {
    try {
        await access(target);
        return true;
    } catch {
        return false;
    }
}

/** Removes a destination only when this tool created it (marker present). Never touches anything else. */
export async function cleanup(destination) {
    validateDestination(destination);
    if (!(await exists(destination))) return;
    if (!(await exists(path.join(destination, marker)))) {
        throw fail(`Refusing to remove '${destination}': not created by this tool (no ${marker} marker)`);
    }
    await rm(destination, { recursive: true, force: true, maxRetries: 0 });
}

/**
 * Verifies an extracted closure: marker, flattened app publish, and exactly one selected version of each
 * shared framework. Ambiguous framework selection is a failure, never a pick-the-latest decision.
 */
export async function preflight(destination) {
    validateDestination(destination);
    if (!(await exists(path.join(destination, marker)))) throw fail('Extraction marker is missing');
    const app = path.join(destination, 'app');
    const entries = await readdir(app).catch(() => []);
    if (!entries.some(entry => entry.endsWith('.dll'))) throw fail('Extracted app directory has no managed assemblies');
    const selected = {};
    for (const framework of frameworks) {
        const directory = path.join(destination, 'shared', framework);
        const versions = await readdir(directory).catch(() => []);
        if (versions.length !== 1) {
            throw fail(`Framework selection for ${framework} is not exactly one version (found ${versions.length})`);
        }
        selected[framework] = versions[0];
    }
    return selected;
}

/** Extracts the app publish and shared framework directories of one digest-pinned image. */
export async function extract({ image, platform, destination, docker = 'docker' }) {
    validateInputs({ image, platform, destination });
    const deadline = Date.now() + caps.overall;
    if (await exists(destination)) throw fail(`Destination '${destination}' already exists`);
    await runChild(docker, ['version', '--format', '{{.Server.Version}}'], remaining(deadline, caps.tool));

    let container;
    let created = false;
    let failure;
    try {
        await mkdir(destination, { recursive: true });
        created = true;
        await writeFile(path.join(destination, marker), `${image}\n`);
        const { stdout } = await runChild(docker, ['create', '--platform', platform, image], remaining(deadline, caps.create));
        container = stdout.trim();
        if (!/^[0-9a-f]{12,64}$/.test(container)) throw fail('docker create did not return a container id');
        await runChild(docker, ['cp', `${container}:/app/.`, path.join(destination, 'app')], remaining(deadline, caps.copy));
        const root = (await runChild(docker, ['inspect', '--format', '{{range .Config.Env}}{{println .}}{{end}}', container], remaining(deadline, caps.tool))).stdout
            .split('\n').find(line => line.startsWith('DOTNET_ROOT='))?.slice('DOTNET_ROOT='.length) ?? '/usr/share/dotnet';
        await runChild(docker, ['cp', `${container}:${root}/shared/.`, path.join(destination, 'shared')], remaining(deadline, caps.copy));
        const selected = await preflight(destination);
        await writeFile(path.join(destination, manifestName), JSON.stringify({ image, platform, dotnetRoot: root, frameworks: selected }, null, 2));
        return selected;
    } catch (error) {
        failure = error;
        throw error;
    } finally {
        if (container) {
            try {
                await runChild(docker, ['rm', '-f', container], caps.remove);
            } catch (error) {
                failure ??= error;
                console.error(`Could not remove extraction container ${container}: ${error.message}`);
                process.exitCode = process.exitCode || error.exitCode || 1;
            }
        }
        if (failure && created) {
            // Early cleanup of the partial extraction; the original failure stays the reported one.
            await cleanup(destination).catch(error => console.error(`Partial extraction cleanup failed: ${error.message}`));
        }
    }
}

export async function main(args = process.argv.slice(2)) {
    const [command, ...rest] = args;
    const { values } = parseArgs({ args: rest, options: {
        image: { type: 'string' }, platform: { type: 'string' }, destination: { type: 'string' }
    } });
    switch (command) {
        case 'extract':
            console.log(JSON.stringify(await extract(values)));
            break;
        case 'preflight':
            console.log(JSON.stringify(await preflight(values.destination)));
            break;
        case 'cleanup':
            await cleanup(values.destination);
            break;
        default:
            throw fail('Usage: run-5518-package-smoke.mjs <extract|preflight|cleanup> --destination <abs> [--image <ref@sha256:..> --platform <linux/amd64|linux/arm64>]', 2);
    }
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = error.exitCode ?? 1; });
}
