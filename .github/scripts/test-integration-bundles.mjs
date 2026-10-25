// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { cp, mkdir, mkdtemp, readFile, rm, symlink, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { pack, verify } from './integration-bundles.mjs';

const sha = 'test-sha';
const project = 'Integration/Client/Client.csproj';
const output = 'Integration/Client/bin/Release/net10.0';
const dll = `${output}/Specs.dll`;
const manifestPath = '.integration-bundles/client.json';
const filter = 'FullyQualifiedName~Specs.Events.';
const cases = [
    { Class: 'Specs.Events.when_appending', Method: 'should_append', DisplayName: 'dynamic value', ID: 'dynamic-id' },
    { Class: 'Specs.EventStores.when_opening', Method: 'should_open' }
];
const listed = entries => ({ code: 0, stdout: JSON.stringify(entries), stderr: '' });
async function fixture(t) {
    const scratch = path.resolve('.ai-work/5050/bundle-tests');
    await mkdir(scratch, { recursive: true });
    const directory = await mkdtemp(path.join(scratch, 'case-'));
    t.after(() => rm(directory, { recursive: true, force: true }));
    const root = path.join(directory, 'producer');
    const consumer = path.join(directory, 'consumer');
    const out = path.join(directory, 'archives');
    const libraries = Object.fromEntries(['xunit.v3.extensibility.core', 'cratis.specifications.xunit.v3', 'xunit.v3.mtp-off']
        .map(name => [`${name}/1.0.0`, { type: 'package' }]));
    const files = { [dll]: 'executable', [`${output}/Specs.pdb`]: 'symbols',
        [`${output}/Specs.deps.json`]: JSON.stringify({ libraries }),
        [`${output}/runtimes/linux-x64/native/library.so`]: 'native dependency',
        'Integration/Client/obj/project.assets.json': JSON.stringify({ libraries }),
        'Integration/Client/obj/Client.csproj.nuget.g.props': '<Project />',
        'Integration/Client/obj/Client.csproj.nuget.g.targets': '<Project />' };
    for (const [file, bytes] of Object.entries(files)) {
        await mkdir(path.dirname(path.join(root, file)), { recursive: true });
        await writeFile(path.join(root, file), bytes);
    }
    await mkdir(consumer);
    const state = { entries: [], result: listed(cases), absolute: true };
    const alias = path.join(directory, 'alias');
    await symlink(root, alias);
    const execute = async (command, args, options) => {
        assert.ok(options.timeout > 0);
        if (command === 'tar') return executeTar(args, state, root, consumer);
        assert.equal(command, 'dotnet');
        if (args[0] === 'msbuild') {
            assert.equal(args[1], project);
            const evaluationRoot = options.cwd === root && state.alias ? alias : options.cwd;
            return listedEvaluation(evaluationRoot, libraries, state.absolute);
        }
        assert.deepEqual(args.slice(1), ['-nologo', '-list', 'full/json']);
        if (state.result instanceof Error) throw state.result;
        return state.result;
    };
    const common = { bundle: 'client', sha, execute };
    return { root, consumer, state,
        pack: () => pack({ ...common, root, out, prefix: 'local' }),
        verify: overrides => verify({ ...common, root: consumer, archive: 'mock.tar.zst', filter, ...overrides }),
        mutate: async change => {
            const file = path.join(root, manifestPath);
            const manifest = JSON.parse(await readFile(file, 'utf8'));
            change(manifest);
            await writeFile(file, JSON.stringify(manifest));
        } };
}
async function executeTar(args, state, root, consumer) {
    if (args[2] === '-tf') return { code: 0, stdout: state.entries.join('\n'), stderr: '' };
    if (args[2] === '-cf') state.entries = args.slice(4);
    else {
        assert.equal(args[2], '-xf');
        for (const file of state.entries) {
            await mkdir(path.dirname(path.join(consumer, file)), { recursive: true });
            await cp(path.join(root, file), path.join(consumer, file));
        }
    }
    return { code: 0, stdout: '', stderr: '' };
}
function listedEvaluation(root, libraries, absolute) {
    return { code: 0, stderr: '', stdout: JSON.stringify({
        Properties: { IsTestProject: 'true', OutputType: 'Exe', TargetFramework: 'net10.0', TargetFrameworks: '',
            AssemblyName: 'Specs', TargetPath: absolute ? path.join(root, dll) : dll,
            ProjectAssetsFile: path.join(root, 'Integration/Client/obj/project.assets.json') },
        Items: { PackageReference: Object.keys(libraries).map(key => ({ Identity: key.split('/')[0], Version: '1.0.0' })) }
    }) };
}

test('manifest round trip normalizes producer and consumer output paths, preserves all files and stable theory identities', async t => {
    const f = await fixture(t);
    const manifest = await f.pack();
    assert.equal(manifest.dll, dll);
    assert.equal(manifest.files.length, 7);
    assert.equal(manifest.discovered.count, 2);
    f.state.absolute = false;
    f.state.result = listed([...cases].reverse().map(item => ({ ...item, DisplayName: 'another random value', ID: 'different' })));
    assert.deepEqual(await f.verify({ namespaces: ['Specs.Events', 'Specs.EventStores'] }), manifest);
});
test('pack canonicalizes an evaluated executable reached through a symlinked checkout', async t => {
    const f = await fixture(t);
    f.state.alias = true;
    assert.equal((await f.pack()).dll, dll);
    await f.verify();
});
const invalidListings = {
    'positive partial output with failed exit': { ...listed(cases), code: 1 },
    'truncated JSON': { code: 0, stdout: JSON.stringify(cases).slice(0, -1), stderr: '' },
    'discovery error on stdout': { code: 0, stdout: `Discovery exception\n${JSON.stringify(cases)}`, stderr: '' },
    'discovery error on stderr': { ...listed(cases), stderr: 'Exception discovering test cases' },
    'error object inside list': listed([...cases, { error: 'Discovery failed' }]),
    'timeout': Object.assign(new Error('Deadline exceeded'), { exitCode: 124 }),
    'signal': { ...listed(cases), signal: 'SIGTERM' },
    'zero cases': listed([])
};
for (const [name, result] of Object.entries(invalidListings)) {
    for (const phase of ['pack', 'verify']) {
        test(`${phase} rejects ${name}`, async t => {
            const f = await fixture(t);
            if (phase === 'verify') await f.pack();
            f.state.result = result;
            await assert.rejects(() => f[phase]());
        });
    }
}
for (const [name, change] of Object.entries({
    'file count': manifest => manifest.files.pop(),
    'file size': manifest => manifest.files[0].size++,
    'file hash': manifest => { manifest.files[0].sha256 = 'wrong'; },
    'SHA': manifest => { manifest.sha = 'another-sha'; },
    'project': manifest => { manifest.project = 'another-project'; },
    'executable': manifest => { manifest.dll = `${output}/Other.dll`; },
    'list count': manifest => manifest.discovered.count++,
    'list identity hash': manifest => { manifest.discovered.idsSha256 = 'wrong'; }
})) {
    test(`verify rejects mismatched ${name}`, async t => {
        const f = await fixture(t);
        await f.pack();
        await f.mutate(change);
        await assert.rejects(() => f.verify());
    });
}
for (const [name, configure] of Object.entries({
    'missing dll': async f => { f.state.entries = f.state.entries.filter(file => file !== dll); },
    'extra bin file': async f => {
        await writeFile(path.join(f.root, output, 'Extra.dll'), 'extra');
        f.state.entries.push(`${output}/Extra.dll`);
    },
    'extra bin file outside Release': async f => {
        await mkdir(path.join(f.consumer, 'Integration/Client/bin/Debug'), { recursive: true });
    },
    'pre-existing bin': async f => { await mkdir(path.join(f.consumer, 'Integration/Client/bin'), { recursive: true }); },
    'pre-existing assets': async f => {
        await mkdir(path.join(f.consumer, 'Integration/Client/obj'), { recursive: true });
        await writeFile(path.join(f.consumer, 'Integration/Client/obj/project.assets.json'), '{}');
    },
    'missing manifest': async f => { f.state.entries = f.state.entries.filter(file => file !== manifestPath); },
    'archive traversal': async f => { f.state.entries.push('../outside'); }
})) {
    test(`verify rejects ${name}`, async t => {
        const f = await fixture(t);
        await f.pack();
        await configure(f);
        await assert.rejects(() => f.verify());
    });
}
test('verify rejects an empty filter and an empty constituent hidden by a nonempty batch', async t => {
    for (const overrides of [{ filter: 'FullyQualifiedName~Missing.' }, { namespaces: ['Specs.Events', 'Missing'] }]) {
        const f = await fixture(t);
        await f.pack();
        await assert.rejects(() => f.verify(overrides), /Empty/);
    }
});
test('verify detects lost cases and changed identities with unchanged count', async t => {
    for (const entries of [cases.slice(1), cases.map(item => ({ ...item, Method: 'changed' }))]) {
        const f = await fixture(t);
        await f.pack();
        f.state.result = listed(entries);
        await assert.rejects(() => f.verify(), /discovery mismatch/);
    }
});
