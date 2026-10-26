// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { chmod, cp, link, lstat, mkdir, mkdtemp, readFile, readdir, rm, symlink, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { discover, pack, verify } from './integration-bundles.mjs';
import { runProcess } from './mixed-specs-process.mjs';

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
    await writeFile(path.join(consumer, 'mock.tar.zst'), 'mock archive');
    const state = { entries: [], result: listed(cases), absolute: true, discovery: [] };
    const alias = path.join(directory, 'alias');
    await symlink(root, alias);
    const execute = async (command, args, options) => {
        assert.ok(options.timeout > 0);
        if (command === 'tar') return executeTar(args, state, root, options.cwd);
        assert.equal(command, 'dotnet');
        if (args[0] === 'msbuild') {
            assert.equal(args[1], project);
            const outputOverride = args.find(value => value.startsWith('-p:BaseOutputPath='));
            const evaluationRoot = outputOverride ? path.resolve(outputOverride.slice('-p:BaseOutputPath='.length), '../../..') : options.cwd;
            return listedEvaluation(evaluationRoot === root && state.alias ? alias : evaluationRoot, libraries, state.absolute);
        }
        assert.deepEqual(args.slice(1), ['-nologo', '-noColor', '-list', 'full/json']);
        state.discovery.push({ dll: args[0], cwd: options.cwd });
        if (state.result instanceof Error) throw state.result;
        return state.result;
    };
    const common = { bundle: 'client', sha, execute };
    return { root, consumer, state, directory, execute, archive: path.join(out, 'integration-bundle-client.tar.zst'),
        pack: overrides => pack({ ...common, root, out, prefix: 'local', ...overrides }),
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
    if (args[2] === '-tvf') return { code: 0, stdout: state.entries.map(file => `-rw------- ${file}`).join('\n'), stderr: '' };
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
test('discovery disables the terminal reset emitted after the complete xunit JSON list', async () => {
    // Retained Client output: a complete 1906-case array, newline, then ESC[0m.
    const json = `${JSON.stringify(cases)}\n`;
    const observed = `${json}\u001b[0m`;
    assert.throws(() => JSON.parse(observed), SyntaxError);
    const execute = async (command, args) => {
        assert.equal(command, 'dotnet');
        assert.equal(args[0], dll);
        return { code: 0, stdout: args.includes('-noColor') ? json : observed, stderr: '' };
    };
    assert.deepEqual(await discover('.', dll, execute), cases.map(item => `${item.Class}.${item.Method}`).sort());
});
const invalidListings = {
    'positive partial output with failed exit': { ...listed(cases), code: 1 },
    'truncated JSON': { code: 0, stdout: JSON.stringify(cases).slice(0, -1), stderr: '' },
    'discovery error on stdout': { code: 0, stdout: `Discovery exception\n${JSON.stringify(cases)}`, stderr: '' },
    'discovery error on stderr': { ...listed(cases), stderr: 'Exception discovering test cases' },
    'trailing discovery diagnostic': { code: 0, stdout: `${JSON.stringify(cases)}\nDiscovery exception`, stderr: '' },
    'unexpected terminal reset': { code: 0, stdout: `${JSON.stringify(cases)}\n\u001b[0m`, stderr: '' },
    'malformed JSON': { code: 0, stdout: '[{"Class": invalid}]\n', stderr: '' },
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

function realTar(f) {
    const calls = [];
    const execute = async (program, args, options) => {
        if (program !== 'tar') return f.execute(program, args, options);
        calls.push({ args, cwd: options.cwd });
        const result = await runProcess(program, args, options);
        assert.equal(result.code, 0, result.stderr);
        return result;
    };
    return { calls, execute };
}
function tarMember({ name, type = '0', target = '', content = '' }) {
    const data = Buffer.from(content);
    const header = Buffer.alloc(512);
    const octal = (value, offset, width) => header.write(`${value.toString(8).padStart(width - 1, '0')}\0`, offset, width);
    assert.ok(Buffer.byteLength(name) <= 100);
    assert.ok(Buffer.byteLength(target) <= 100);
    header.write(name, 0, 100);
    octal(0o600, 100, 8);
    octal(0, 108, 8);
    octal(0, 116, 8);
    octal(data.length, 124, 12);
    octal(0, 136, 12);
    header.fill(' ', 148, 156);
    header.write(type, 156, 1);
    header.write(target, 157, 100);
    header.write('ustar\0', 257, 6);
    header.write('00', 263, 2);
    const checksum = header.reduce((sum, byte) => sum + byte, 0);
    header.write(`${checksum.toString(8).padStart(6, '0')}\0 `, 148, 8);
    return Buffer.concat([header, data, Buffer.alloc((512 - data.length % 512) % 512)]);
}
async function craftedArchive(f, entries) {
    const tar = path.join(f.directory, 'crafted.tar');
    const archive = `${tar}.zst`;
    await writeFile(tar, Buffer.concat([...entries.map(tarMember), Buffer.alloc(1024)]));
    const result = await runProcess('zstd', ['-q', '-f', tar, '-o', archive], { capture: true, timeout: 10000 });
    assert.equal(result.code, 0, result.stderr);
    return archive;
}
async function assertNoConsumerFiles(f) {
    await assert.rejects(lstat(path.join(f.consumer, 'Integration')), { code: 'ENOENT' });
    await assert.rejects(lstat(path.join(f.consumer, '.integration-bundles')), { code: 'ENOENT' });
    assert.deepEqual(await readdir(path.join(f.consumer, '.ai-work/integration-bundles')), []);
}
const maliciousMembers = {
    symlink: [{ name: dll, type: '2', target: '../../../../../../outside' }],
    hardlink: [{ name: dll, type: '1', target: '../outside' }],
    fifo: [{ name: dll, type: '6' }],
    'character device': [{ name: dll, type: '3' }],
    'block device': [{ name: dll, type: '4' }],
    directory: [{ name: `${output}/directory`, type: '5' }],
    'parent symlink redirection': [
        { name: `${output}/redirect`, type: '2', target: '../../../../../../outside-directory' },
        { name: `${output}/redirect/sentinel`, content: 'changed' }
    ]
};
for (const [name, entries] of Object.entries(maliciousMembers)) {
    test(`REAL archive rejects ${name} before any extraction or checkout writes`, async t => {
        const f = await fixture(t);
        const sentinel = path.join(f.directory, 'outside');
        await writeFile(sentinel, 'unchanged');
        await mkdir(path.join(f.directory, 'outside-directory'));
        await writeFile(path.join(f.directory, 'outside-directory/sentinel'), 'unchanged');
        const archive = await craftedArchive(f, [...entries, { name: manifestPath, content: '{}' }]);
        const real = realTar(f);
        await assert.rejects(() => f.verify({ archive, execute: real.execute }), /Non-regular bundle archive member|listing wrote diagnostics/);
        assert.equal(real.calls.filter(call => call.args[2] === '-xf').length, 0);
        assert.equal(await readFile(sentinel, 'utf8'), 'unchanged');
        assert.equal((await lstat(sentinel)).nlink, 1);
        assert.equal(await readFile(path.join(f.directory, 'outside-directory/sentinel'), 'utf8'), 'unchanged');
        await assertNoConsumerFiles(f);
    });
}
test('REAL archive rejects traversal before extraction', async t => {
    const f = await fixture(t);
    const sentinel = path.join(f.directory, 'outside');
    await writeFile(sentinel, 'unchanged');
    const archive = await craftedArchive(f, [
        { name: `${output}/../../../../../../outside`, content: 'changed' },
        { name: manifestPath, content: '{}' }
    ]);
    const real = realTar(f);
    await assert.rejects(() => f.verify({ archive, execute: real.execute }), /Invalid bundle archive paths|listing wrote diagnostics/);
    assert.equal(real.calls.filter(call => call.args[2] === '-xf').length, 0);
    assert.equal(await readFile(sentinel, 'utf8'), 'unchanged');
    await assertNoConsumerFiles(f);
});
test('REAL archive round trip snapshots and stages extraction, preserving owner executable permissions', async t => {
    const f = await fixture(t);
    await chmod(path.join(f.root, dll), 0o755);
    const real = realTar(f);
    const manifest = await f.pack({ execute: real.execute });
    real.calls.length = 0;
    f.state.discovery.length = 0;
    assert.deepEqual(await f.verify({ archive: f.archive, execute: real.execute }), manifest);
    const extraction = real.calls.find(call => call.args[2] === '-xf');
    assert.ok(extraction.cwd.startsWith(path.join(f.consumer, '.ai-work/integration-bundles/verify-')));
    assert.ok(real.calls.every(call => call.args[3] === extraction.args[3]));
    assert.notEqual(extraction.args[3], f.archive);
    assert.deepEqual(f.state.discovery, [{ dll: path.join(extraction.cwd, dll), cwd: f.consumer }]);
    assert.equal((await lstat(path.join(f.consumer, dll))).mode & 0o700, 0o700);
    assert.deepEqual(await readdir(path.join(f.consumer, '.ai-work/integration-bundles')), []);
});
for (const parent of ['Integration', 'Integration/Client/obj', '.integration-bundles', '.ai-work']) {
    test(`REAL valid archive cannot copy through a symlinked checkout parent ${parent}`, async t => {
        const f = await fixture(t);
        const real = realTar(f);
        await f.pack({ execute: real.execute });
        const outside = path.join(f.directory, 'outside-parent');
        await mkdir(outside);
        await mkdir(path.dirname(path.join(f.consumer, parent)), { recursive: true });
        await symlink(outside, path.join(f.consumer, parent));
        await assert.rejects(() => f.verify({ archive: f.archive, execute: real.execute }), /Unsafe bundle parent/);
        assert.deepEqual(await readdir(outside), []);
        await assert.rejects(lstat(path.join(f.consumer, '.integration-bundles/client.json')), { code: 'ENOENT' });
    });
}
for (const kind of ['symlink', 'hardlink']) {
    test(`REAL valid archive cannot overwrite a checkout destination ${kind}`, async t => {
        const f = await fixture(t);
        const real = realTar(f);
        await f.pack({ execute: real.execute });
        const sentinel = path.join(f.directory, 'outside');
        await writeFile(sentinel, 'unchanged');
        await mkdir(path.join(f.consumer, '.integration-bundles'));
        const destination = path.join(f.consumer, manifestPath);
        if (kind === 'symlink') await symlink(sentinel, destination);
        else await link(sentinel, destination);
        await assert.rejects(() => f.verify({ archive: f.archive, execute: real.execute }), /Pre-existing bundle destination/);
        assert.equal(await readFile(sentinel, 'utf8'), 'unchanged');
        await assert.rejects(lstat(path.join(f.consumer, dll)), { code: 'ENOENT' });
    });
}
test('REAL archive manifest rejection leaves checkout outputs absent', async t => {
    const f = await fixture(t);
    const real = realTar(f);
    await f.pack({ execute: real.execute });
    await assert.rejects(() => f.verify({ archive: f.archive, execute: real.execute, sha: 'wrong' }), /SHA mismatch/);
    await assertNoConsumerFiles(f);
});
