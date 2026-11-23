// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, rm, readFile } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { verifyResults } from './spec-shards.mjs';

const workflow = await readFile(new URL('../workflows/dotnet-build.yml', import.meta.url), 'utf8');

function shellStep(name) {
    const step = workflow.split(/^ {6}- name: /m).find(value => value.startsWith(`${name}\n`));
    assert.ok(step, `Missing workflow step ${name}`);
    const script = step.match(/^ {8}run: \|\n((?: {10}[^\n]*\n|\n)+)/m)?.[1];
    assert.ok(script, `Missing shell script for ${name}`);
    return script.replace(/^ {10}/gm, '').replaceAll('${{ matrix.group }}', 'fixture');
}

async function download(t, directory, filename, matches) {
    await mkdir('.ai-work/r1', { recursive: true });
    const root = await mkdtemp(path.resolve('.ai-work/r1/artifact-handoff-'));
    t.after(() => rm(root, { recursive: true, force: true }));
    for (let index = 0; index < matches; index++) {
        // v8 flattens one match; multiple unmerged matches retain artifact directories.
        const destination = matches === 1 ? directory : path.join(directory, `artifact-${index}`);
        await mkdir(path.join(root, destination), { recursive: true });
        await writeFile(path.join(root, destination, filename), '<coverage/>');
    }
    return root;
}

const coverageSteps = [
    { name: 'Find coverage files', directory: 'coverage-input', filename: 'coverage.cobertura.xml', output: 'coverage files' },
    { name: 'List the merged coverage files', directory: 'coverage-partials', filename: 'Cobertura.xml', output: 'partial coverage files' }
];

for (const step of coverageSteps) for (const matches of [1, 2]) {
    test(`${step.name} consumes ${matches} matching artifact(s) with v8 extraction layout`, async t => {
        const root = await download(t, step.directory, step.filename, matches);
        const stdout = execFileSync('bash', ['-euo', 'pipefail', '-c', shellStep(step.name)], {
            cwd: root, encoding: 'utf8', env: { ...process.env, GITHUB_OUTPUT: path.join(root, 'output') }
        });
        assert.ok(stdout.includes(`${matches} ${step.output}`), stdout);
    });
}

test('one flattened shard receipt cannot satisfy the four-producer handoff', async t => {
    const root = await download(t, 'receipts', 'shard-receipt.json', 1);
    await assert.rejects(verifyResults(path.join(root, 'receipts'), []), /Expected exactly 4 shard receipt files; found 1/);
});
