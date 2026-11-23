// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { isAbsolute } from 'node:path';
import { fileURLToPath } from 'node:url';

const repositoryRoot = fileURLToPath(new URL('../../../', import.meta.url));
const features = 'Source/Workbench/Features';
const regenerate = 'dotnet build Source/Kernel/Core/Core.csproj -c Debug -p:DisableProxyGenerator=false';

function run(command, args, { stdio = 'pipe', allowedStatuses = [0] } = {}) {
    const result = spawnSync(command, args, { cwd: repositoryRoot, encoding: 'utf8', stdio });
    if (result.error) throw result.error;
    if (!allowedStatuses.includes(result.status)) {
        throw new Error(`${command} ${args.join(' ')} failed (exit ${result.status}, signal ${result.signal}).\n${result.stderr ?? ''}`);
    }
    return result.stdout ?? '';
}

function changedFiles(...statuses) {
    // The index is the expected output, so staged-only changes are not drift.
    return [...new Set(statuses.flatMap(status => status.split('\n').filter(line => line.length > 2 && line[1] !== ' ')))];
}

function selfTest() {
    const modified = ` M ${features}/AppendResult.ts`;
    const untracked = `?? ${features}/NewResult.ts`;
    const staged = `M  ${features}/AppendResult.ts`;
    const cases = [
        [['', ''], []],
        [[modified, ''], [modified]],
        [['', modified], [modified]],
        [[modified, modified], [modified]],
        [['', untracked], [untracked]],
        [[staged, ''], []],
        [[staged, untracked], [untracked]],
        [[`MM ${features}/AppendResult.ts`, ''], [`MM ${features}/AppendResult.ts`]],
    ];
    for (const [statuses, expected] of cases) {
        assert.deepEqual(changedFiles(...statuses), expected);
    }
    console.log(`Checked ${cases.length} drift-detection cases, including unstaged, staged-only and untracked files.`);
}

function checkProxies() {
    if (isAbsolute(process.env.OutputPath ?? '')) {
        throw new Error('An absolute OutputPath disables proxy generation. Unset OutputPath and run the check again.');
    }
    const tracked = run('git', ['ls-files', '--', features]).trim().split('\n').filter(Boolean);
    if (tracked.length === 0) throw new Error(`No tracked files found under ${features}.`);

    const statusArgs = ['status', '--porcelain=v1', '--untracked-files=all', '--', features];
    // Remember existing edits even if regeneration repairs them back to the staged version.
    const before = run('git', statusArgs);
    run('dotnet', ['build', 'Source/Kernel/Core/Core.csproj', '-c', 'Debug', '-p:DisableProxyGenerator=false'], { stdio: 'inherit' });
    run('git', ['diff', '--exit-code', '--', features], { stdio: 'inherit', allowedStatuses: [0, 1] });
    const changed = changedFiles(before, run('git', statusArgs));
    if (changed.length > 0) {
        console.error(`Workbench proxy drift or unstaged feature changes (${changed.length} entries):\n${changed.join('\n')}`);
        console.error(`From the repository root, run:\n  ${regenerate}\nReview and stage the generated proxies, then run this check again.`);
        return 1;
    }
    console.log(`Checked ${tracked.length} tracked Workbench feature files; no proxy drift.`);
    return 0;
}

try {
    const args = process.argv.slice(2);
    if (args.length === 1 && args[0] === '--self-test') {
        selfTest();
    } else if (args.length === 0) {
        process.exitCode = checkProxies();
    } else {
        throw new Error('Usage: node Source/Workbench/scripts/check-proxies.mjs [--self-test]');
    }
} catch (error) {
    console.error(`Could not check Workbench proxies: ${error.message}`);
    process.exitCode = 2;
}
