// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Contracts for the per-project integration bundles and the restore-free selection path.
import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { layout, selectionRecord, readSelection, pack } from './pack-integration-bundles.mjs';
import { main } from './run-spec-project.mjs';

const scratch = path.resolve('.ai-work/integration-bundles-specs');
async function fixture() {
    await mkdir(scratch, { recursive: true });
    return mkdtemp(path.join(scratch, 'case-'));
}
const info = (root, project = 'Integration/Client/Client.csproj', dll = 'Integration/Client/bin/Release/net10.0/Cratis.Chronicle.Integration.dll') =>
    ({ project, runner: 'v3', tfm: 'net10.0', dll: path.join(root, dll), assemblyName: 'Cratis.Chronicle.Integration', coverageRoots: ['Source'] });

test('layout names the project, output, selection and archive for each bundle', () => {
    for (const [name, label] of [['client', 'Client'], ['kernel', 'Kernel'], ['clustering', 'Clustering']]) {
        assert.deepEqual(layout(name), {
            project: `Integration/${label}/${label}.csproj`,
            outputDirectory: `Integration/${label}/bin/Release/net10.0`,
            binDirectory: `Integration/${label}/bin/Release`,
            selection: `.integration-bundles/${name}.selection.json`,
            archive: `.integration-bundles/integration-bundle-${name}.tar.zst`
        });
    }
    assert.throws(() => layout('sqlclustering'), /Unknown integration bundle/);
});

test('selectionRecord makes the dll relative and rejects one outside bin/Release', async () => {
    const root = await fixture();
    assert.equal(selectionRecord(root, info(root)).dll, 'Integration/Client/bin/Release/net10.0/Cratis.Chronicle.Integration.dll');
    assert.throws(() => selectionRecord(root, info(root, undefined, 'Integration/Client/obj/Other.dll')), /outside/);
    assert.throws(() => selectionRecord(root, { ...info(root), dll: '/elsewhere/Other.dll' }), /outside/);
});

async function writeBundle(root, record) {
    const dll = 'Integration/Client/bin/Release/net10.0/Cratis.Chronicle.Integration.dll';
    await mkdir(path.join(root, path.dirname(dll)), { recursive: true });
    await writeFile(path.join(root, dll), 'mock, never executed');
    await mkdir(path.join(root, '.integration-bundles'), { recursive: true });
    await writeFile(path.join(root, '.integration-bundles/client.selection.json'), JSON.stringify({ ...selectionRecord(root, info(root)), ...record }));
}
const selectionFile = '.integration-bundles/client.selection.json';

test('readSelection returns a valid record and rejects every unsafe one', async () => {
    const project = 'Integration/Client/Client.csproj';
    const root = await fixture();
    await writeBundle(root, {});
    assert.equal((await readSelection(root, project, selectionFile)).runner, 'v3');
    await assert.rejects(readSelection(root, 'Integration/Kernel/Kernel.csproj', selectionFile), /not Integration\/Kernel/);
    for (const [change, message] of [[{ runner: 'v2' }, /v3 runner/], [{ dll: '/abs/x.dll' }, /relative/], [{ dll: '../x.dll' }, /relative/], [{ dll: 'Integration/Client/bin/Release/Missing.dll' }, /missing/]]) {
        await writeBundle(root, change);
        await assert.rejects(readSelection(root, project, selectionFile), message);
    }
});

test('pack evaluates each bundle and tars exactly its output directory and selection once', async () => {
    const root = await fixture();
    const calls = [];
    const evaluate = async (cwd, project) => info(cwd, project, `${path.posix.dirname(project)}/bin/Release/net10.0/Spec.dll`);
    const execute = async (command, args) => { calls.push([command, ...args]); return { code: 0 }; };
    await pack(root, '.integration-bundles', ['client', 'kernel', 'clustering'], { execute, evaluate });
    assert.deepEqual(calls, ['Client', 'Kernel', 'Clustering'].map(label => ['tar', '-I', 'zstd -T0', '-cf',
        `.integration-bundles/integration-bundle-${label.toLowerCase()}.tar.zst`, `Integration/${label}/bin/Release/net10.0`, `.integration-bundles/${label.toLowerCase()}.selection.json`]));
    const record = JSON.parse(await readFile(path.join(root, '.integration-bundles/kernel.selection.json'), 'utf8'));
    assert.equal(record.dll, 'Integration/Kernel/bin/Release/net10.0/Spec.dll');
    await assert.rejects(pack(root, '.integration-bundles', ['client'], { execute: async () => ({ code: 2 }), evaluate }), /tar exited 2/);
});

test('run-spec-project with --selection never evaluates and runs the stored dll', async () => {
    const root = await fixture();
    await writeBundle(root, {});
    const calls = [];
    const evaluate = async () => { throw new Error('evaluate must not be called'); };
    const execute = async (command, args) => {
        calls.push([command, ...args]);
        const directory = path.join(root, 'results');
        await writeFile(path.join(directory, 'results.trx'), '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testId="c" testName="A.b" outcome="Passed" /></Results><TestDefinitions><UnitTest id="c"><TestMethod className="A" name="b" /></UnitTest></TestDefinitions><ResultSummary><Counters total="1" executed="1" passed="1" failed="0" error="0" /></ResultSummary></TestRun>');
        return { code: 0 };
    };
    await main(['--project', 'Integration/Client/Client.csproj', '--results-directory', 'results', '--selection', selectionFile], { root, execute, evaluate });
    assert.equal(calls.length, 1);
    assert.equal(calls[0][0], 'dotnet');
    assert.equal(calls[0][1], 'Integration/Client/bin/Release/net10.0/Cratis.Chronicle.Integration.dll');
});

test('run-spec-project without --selection still evaluates the project', async () => {
    const root = await fixture();
    let evaluated = 0;
    const evaluate = async () => { evaluated++; throw Object.assign(new Error('stop'), { exitCode: 3 }); };
    await assert.rejects(main(['--project', 'Integration/Client/Client.csproj', '--results-directory', 'results'], { root, evaluate }), /stop/);
    assert.equal(evaluated, 1);
});
