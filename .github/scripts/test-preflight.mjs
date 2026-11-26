// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';
import { affectsDotnet, pushNeedsBuild } from './preflight.mjs';

const root = fileURLToPath(new URL('../../', import.meta.url));
const script = fileURLToPath(new URL('./preflight.mjs', import.meta.url));
const local = 'a'.repeat(40);
const remote = 'b'.repeat(40);
const zero = '0'.repeat(40);
const update = (from = remote, to = local) => `refs/heads/x ${to} refs/heads/x ${from}\n`;

test('every .NET build input triggers analysis, including nested configuration', () => {
    const inputs = ['src/Foo.cs', 'src/Foo.csproj', 'Directory.Build.props', 'Directory.Build.targets',
        'Chronicle.slnx', 'Clients.sln', '.editorconfig', 'src/.editorconfig', '.globalconfig',
        'src/.globalconfig', 'Directory.Packages.props', 'global.json', 'src/stylecop.json',
        'Source/Kernel/Core/OrleansContracts.txt', 'Source/Kernel/Core/GrainPayloadContracts.txt',
        'NuGet.config', 'nuget.config', 'src/rules.ruleset', 'src/Analyzers.globalconfig',
        '.config/dotnet-tools.json', 'src/packages.lock.json', 'Source/Kernel/Protobuf/chronicle.desc',
        'Source/Clients/Workbench/SwaggerDark.css', 'Source/Clients/Assets/logo.png', 'test.mjs', 'Source/file.ts'];
    for (const file of inputs) assert.equal(affectsDotnet(file), true, file);
    for (const file of ['Documentation/index.md', 'Documentation/images/x.png', 'src/Foo.cs.md',
        'Source/Workbench/Features/View.tsx', 'Source/Workbench/Styles/theme.css', 'README.md',
        '.github/workflows/dotnet-build.yml', '.cratis/ai.json']) {
        assert.equal(affectsDotnet(file), false, file);
    }
});

test('compares pushed endpoints and skips only non-.NET changes', () => {
    assert.equal(pushNeedsBuild(update(), (from, to) => {
        assert.equal(from, remote);
        assert.equal(to, local);
        return ['Documentation/index.md'];
    }), false);
    assert.equal(pushNeedsBuild(update(), () => ['Source/deleted.cs']), true);
    // A deleted or renamed embedded resource appears by its old path with --no-renames.
    assert.equal(pushNeedsBuild(update(), () => ['Documentation/a.md', 'Source/Clients/Workbench/SwaggerDark.css']), true);
    assert.equal(pushNeedsBuild(update(), () => []), false);
});

test('new refs build, deleted refs and empty pushes skip without diffing', () => {
    const noDiff = () => assert.fail('Must not diff a missing endpoint');
    assert.equal(pushNeedsBuild(update(zero), noDiff), true);
    assert.equal(pushNeedsBuild(update(remote, zero), noDiff), false);
    assert.equal(pushNeedsBuild('', noDiff), false);
});

test('checks every pushed ref, not just the first one', () => {
    const another = 'c'.repeat(40);
    const diff = (from, to) => to === another ? ['Source/Core.cs'] : ['README.md'];
    assert.equal(pushNeedsBuild(update() + update(remote, another), diff), true);
});

test('invalid input or unavailable comparisons cannot silently skip', () => {
    assert.throws(() => pushNeedsBuild('bad input'), /Invalid pre-push input/);
    assert.throws(() => pushNeedsBuild(update().replace(local, '--bad')), /Invalid pre-push input/);
    assert.throws(() => pushNeedsBuild(update(), () => { throw new Error('missing object'); }), /missing object/);
});

test('manual and hook entrypoints retain command, working directory and exit status', () => {
    const work = join(root, '.ai-work', 'chronicle-6024');
    mkdirSync(work, { recursive: true });
    const fixture = mkdtempSync(join(work, 'preflight-test-'));
    writeFileSync(join(fixture, 'dotnet'), `#!${process.execPath}
console.log(JSON.stringify({ args: process.argv.slice(2), cwd: process.cwd() }));
process.exitCode = Number(process.env.PREFLIGHT_TEST_EXIT ?? 0);
`, { mode: 0o755 });
    const run = (args = [], input = '', extraEnv = {}) => spawnSync(process.execPath, [script, ...args], {
        cwd: fixture, input, encoding: 'utf8',
        env: { ...process.env, PATH: `${fixture}:${process.env.PATH}`, ...extraEnv }
    });
    const manual = run();
    assert.ifError(manual.error);
    assert.equal(manual.status, 0, manual.stderr);
    const command = JSON.parse(manual.stdout.split('\n').find(line => line.startsWith('{')));
    assert.deepEqual(command.args, ['build', '--configuration', 'Release', '-p:DisableProxyGenerator=true',
        '-p:DisableDockerBuild=true', '-p:ForceCodeGeneration=true', '--no-incremental']);
    assert.equal(command.cwd, root.replace(/\/$/, ''));
    assert.match(manual.stdout, /Preflight elapsed: \d+\.\d seconds\./);
    assert.equal(run([], '', { PREFLIGHT_TEST_EXIT: '47' }).status, 47);
    const skipped = run(['--hook']);
    assert.equal(skipped.status, 0, skipped.stderr);
    assert.match(skipped.stdout, /Preflight skipped/);
    assert.doesNotMatch(skipped.stdout, /Preflight elapsed/);
    assert.equal(run(['--hook'], 'bad input').status, 2);
    assert.equal(run(['--unknown']).status, 2);
    assert.equal(run(['--hook'], update(zero), { PREFLIGHT_TEST_EXIT: '31' }).status, 31);
});
