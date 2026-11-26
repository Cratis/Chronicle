// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';

const root = fileURLToPath(new URL('../../', import.meta.url));
const zeroOid = /^0+$/;
const objectId = /^(?:[a-f0-9]{40}|[a-f0-9]{64})$/;

// The .NET build reads more than source and project files (for example OrleansContracts.txt, rulesets,
// NuGet configuration, lock files and embedded resources such as Source/Clients/Workbench/SwaggerDark.css),
// so only files known not to feed it are allowed to skip the build. The Workbench front-end source in
// Source/Workbench has no project of its own; the Release build embeds only its generated wwwroot output.
const nonBuildPath = /^(?:Documentation|\.github|\.cratis|\.githooks|Source\/Workbench)\//;
const nonBuildExtension = /\.(?:md|mdx)$/i;

export function affectsDotnet(file) {
    return !nonBuildPath.test(file) && !nonBuildExtension.test(file);
}

function pushedUpdates(input) {
    return input.split('\n').filter(line => line.trim()).map(line => {
        const fields = line.trim().split(/\s+/);
        if (fields.length !== 4 || !objectId.test(fields[1]) || !objectId.test(fields[3])) {
            throw new Error('Invalid pre-push input; expected local ref, local OID, remote ref, remote OID.');
        }
        return { local: fields[1], remote: fields[3] };
    });
}

function changedFiles(remote, local) {
    // Compare the actual pushed endpoints, not HEAD or a merge base. Include deleted and renamed inputs.
    const result = spawnSync('git', ['diff', '--name-only', '--no-renames', '-z', remote, local, '--'], {
        cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'inherit']
    });
    if (result.error) throw result.error;
    if (result.status !== 0) throw new Error('Cannot compare pushed commits; fetch the remote ref and retry.');
    return result.stdout.split('\0').filter(Boolean);
}

export function pushNeedsBuild(input, diff = changedFiles) {
    const updates = pushedUpdates(input);
    return updates.some(({ local, remote }) => {
        if (zeroOid.test(local)) return false; // Deleting a ref does not introduce build inputs.
        if (zeroOid.test(remote)) return true; // A new remote ref has no baseline: build conservatively.
        return diff(remote, local).some(affectsDotnet);
    });
}

function build() {
    // Mirror .github/workflows/dotnet-build.yml:351-352 (analyzers), with implicit restore.
    // Source/Directory.Build.props:55-61 makes Release warnings errors; Directory.Build.props:15-21
    // enables analyzers and code style. --no-incremental prevents reuse of an unanalyzed local build,
    // matching CI's fresh checkout without an obj cache (.github/workflows/dotnet-build.yml:344-345).
    const args = ['build', '--configuration', 'Release', '-p:DisableProxyGenerator=true',
        '-p:DisableDockerBuild=true', '-p:ForceCodeGeneration=true', '--no-incremental'];
    console.log(`Preflight: dotnet ${args.join(' ')} (Chronicle.slnx)`);
    const started = performance.now();
    try {
        const result = spawnSync('dotnet', args, { cwd: root, stdio: ['ignore', 'inherit', 'inherit'] });
        if (result.error) throw result.error;
        if (result.signal) process.kill(process.pid, result.signal);
        return result.status ?? 2;
    } finally {
        console.log(`Preflight elapsed: ${((performance.now() - started) / 1000).toFixed(1)} seconds.`);
    }
}

export function main() {
    const { values } = parseArgs({ options: { hook: { type: 'boolean', default: false } } });
    if (values.hook && !pushNeedsBuild(readFileSync(0, 'utf8'))) {
        console.log('Preflight skipped: pushed refs contain no .NET build-affecting changes.');
        return 0;
    }
    return build();
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    try {
        process.exitCode = main();
    } catch (error) {
        console.error(`Preflight could not run: ${error.message}`);
        process.exitCode = 2;
    }
}
