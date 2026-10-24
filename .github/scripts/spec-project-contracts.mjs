// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, symlink, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { assertExecutableSpecRunners } from './assert-executable-spec-runners.mjs';
import { main } from './run-spec-project.mjs';
import { runProcess } from './mixed-specs-process.mjs';

const root = process.cwd();
async function fixture() {
    await mkdir(path.join(root, '.ai-work'), { recursive: true });
    const directory = await mkdtemp(path.join(root, '.ai-work', 'spec-project-'));
    await mkdir(path.join(directory, 'Source/Library'), { recursive: true });
    await writeFile(path.join(directory, 'Source/Library/Decision.cs'), '// mock production source');
    await writeFile(path.join(directory, 'Spec.dll'), 'mock, never executed');
    return directory;
}
const trx = (outcome = 'Passed') => `<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testId="case1" testName="Example.should_pass" outcome="${outcome}" /></Results><TestDefinitions><UnitTest id="case1"><TestMethod className="Example" name="should_pass" /></UnitTest></TestDefinitions><ResultSummary><Counters total="1" executed="1" passed="${outcome === 'Passed' ? 1 : 0}" failed="0" error="0" /></ResultSummary></TestRun>`;
const coverage = '<coverage><packages><package><classes><class filename="Source/Library/Decision.cs"><lines><line hits="1"/></lines></class></classes></package></packages></coverage>';
function evaluatedProject(directory, project) {
    return async (cwd, selected) => {
        assert.equal(cwd, directory);
        assert.equal(selected, project);
        return { project, runner: 'v3', dll: path.join(directory, 'Spec.dll') };
    };
}

test('CI workflows, scripts and actions forbid test-host dispatch without spec-name carve-outs', async () => {
    await assertExecutableSpecRunners(root);
    const directory = await fixture();
    await mkdir(path.join(directory, '.github/workflows'), { recursive: true });
    await mkdir(path.join(directory, '.github/scripts'), { recursive: true });
    await mkdir(path.join(directory, '.github/actions/nested'), { recursive: true });
    const action = path.join(directory, '.github/actions/nested/action.yml');
    await writeFile(action, 'runs:\n  using: composite\n  steps: []');
    const workflow = path.join(directory, '.github/workflows/negative.yml');
    const script = path.join(directory, '.github/scripts/negative.sh');
    await writeFile(script, '#!/bin/bash\necho valid');
    for (const suffix of ['Some.Specs.csproj', 'Chronicle.slnx', '']) {
        await writeFile(workflow, `run: ${['dotnet', 'test', suffix].join(' ')}`);
        await assert.rejects(assertExecutableSpecRunners(directory), /negative.yml:1/);
    }
    await writeFile(workflow, 'run: echo valid');
    await writeFile(script, ['dotnet', '\\\n', 'test'].join(' '));
    await assert.rejects(assertExecutableSpecRunners(directory), /negative.sh:1/);
    await writeFile(script, '#!/bin/bash\necho valid');
    await writeFile(action, `runs:\n  using: composite\n  steps:\n    - run: ${['dotnet', 'test', 'Some.Specs.csproj'].join(' ')}\n      shell: bash`);
    await assert.rejects(assertExecutableSpecRunners(directory), /actions\/nested\/action.yml:4/);
    await writeFile(action, 'runs:\n  using: composite\n  steps: []');
    const actionScript = path.join(directory, '.github/actions/nested/runner.py');
    const command = ['dotnet', 'test'].join(' ');
    await writeFile(actionScript, `"""Mentions the command ${command} as documentation."""\nimport subprocess\nsubprocess.run('${command}', shell=True)`);
    await assert.rejects(assertExecutableSpecRunners(directory), /actions\/nested\/runner.py:3/);
    await writeFile(actionScript, `"""Mentions the command ${command} as documentation."""\nprint('valid')`);
    await assertExecutableSpecRunners(directory);
});

test('Azure Key Vault workflow builds only when absent from the solution, using a mock builder', async () => {
    const workflow = await readFile(path.join(root, '.github/workflows/dotnet-build.yml'), 'utf8');
    const script = workflow.match(/- name: Build Azure Key Vault specs when absent from solution\n        run: \|\n([\s\S]*?)(?=\n      - name:)/)[1].replace(/^          /gm, '');
    const directory = await fixture();
    await mkdir(path.join(directory, '.github'));
    await symlink(path.join(root, '.github/scripts'), path.join(directory, '.github/scripts'), 'dir');
    const bin = path.join(directory, 'bin');
    await mkdir(bin);
    await writeFile(path.join(bin, 'dotnet'), '#!/bin/bash\nprintf "%s\\n" "$*"\n', { mode: 0o755 });
    const vault = 'Source/Kernel/Storage.Vault.Specs/Storage.Vault.Specs.csproj';
    const azure = 'Source/Kernel/Storage.AzureKeyVault.Specs/Storage.AzureKeyVault.Specs.csproj';
    for (const listed of [false, true]) {
        await writeFile(path.join(directory, 'Chronicle.slnx'), `<Solution><Project Path="${vault}" />${listed ? `<Project Path="${azure}" />` : ''}</Solution>`);
        const result = await runProcess('env', [`PATH=${bin}:${process.env.PATH}`, 'bash', '-c', script], { cwd: directory, capture: true, timeout: 10000 });
        assert.equal(result.code, 0, result.stderr);
        assert.equal(result.stdout.includes(`build ${azure} --configuration Release`), !listed);
    }
});

test('single project runner preserves filter composition, deadline, coverage and Release evaluation', async () => {
    const directory = await fixture();
    await writeFile(path.join(directory, 'settings'), '<TestSessionTimeout>1000</TestSessionTimeout><TestCaseFilter>Name!~excluded&amp;Name!~other</TestCaseFilter>');
    const project = 'Source/Kernel/Storage.Sql.Specs/Storage.Sql.Specs.csproj';
    const filter = '(FullyQualifiedName~A|FullyQualifiedName~B)&FullyQualifiedName!~C';
    await main(['--project', project, '--filter', filter, '--settings', 'settings', '--coverage', '--results-directory', 'results'], {
        root: directory,
        evaluate: evaluatedProject(directory, project),
        execute: async (command, args, options) => {
            assert.equal(command, 'dotnet');
            assert.equal(options.timeout, 1000);
            assert.deepEqual(args.slice(0, 9), ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', path.join(directory, 'results/coverage.cobertura.xml')]);
            assert.match(args[9], /dotnet.*Spec\.dll.*-filterVSTest/);
            assert.ok(args[9].includes(`(${filter})&(Name!~excluded&Name!~other)`));
            await writeFile(path.join(directory, 'results/results.trx'), trx());
            await writeFile(path.join(directory, 'results/coverage.cobertura.xml'), coverage);
            return { code: 0 };
        }
    });
});

test('single project runner rejects unknown projects before evaluating', async () => {
    await assert.rejects(main(['--project', 'unknown.csproj', '--results-directory', 'results'], {
        evaluate: () => assert.fail('must not evaluate')
    }), /known spec project/);
});

test('single project runner rejects zero tests and propagates child failure', async () => {
    for (const code of [0, 37]) {
        const directory = await fixture();
        await assert.rejects(main(['--project', 'Integration/SqlClustering/SqlClustering.csproj', '--results-directory', 'results'], {
            root: directory,
            evaluate: async () => ({ runner: 'v3', dll: path.join(directory, 'Spec.dll') }),
            execute: async (_command, args) => {
                assert.deepEqual(args, [path.join(directory, 'Spec.dll'), '-result-trx', path.join(directory, 'results/results.trx')]);
                await writeFile(path.join(directory, 'results/results.trx'), '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"/>');
                return { code };
            }
        }), error => code ? error.exitCode === code : /zero|missing/i.test(error.message));
    }
});

test('retry wrapper retains failed identities and narrows each fresh attempt', async () => {
    const directory = await fixture();
    const bin = path.join(directory, 'bin');
    await mkdir(bin);
    await writeFile(path.join(bin, 'sleep'), '#!/bin/bash\nexit 0\n', { mode: 0o755 });
    await writeFile(path.join(bin, 'node'), `#!${process.execPath}
const fs = require('node:fs');
const args = process.argv.slice(2);
if (args[0] !== '.github/scripts/run-spec-project.mjs' || !args.includes('--coverage')) process.exit(91);
const dir = args[args.indexOf('--results-directory') + 1];
const attempt = Number(dir.match(/attempt-(\\d+)$/)[1]);
fs.appendFileSync(${JSON.stringify(path.join(directory, 'calls'))}, JSON.stringify(args) + '\\n');
fs.mkdirSync(dir, { recursive: true });
const result = (name, outcome) => '<UnitTestResult testName="Example.' + name + '" outcome="' + outcome + '" />';
const results = attempt === 1 ? result('A', 'Failed') + result('B', 'Failed') : attempt === 2 ? result('A', 'Failed') + result('B', 'Passed') : result('A', 'Passed');
fs.writeFileSync(dir + '/results.trx', '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>' + results + '</Results></TestRun>');
process.exit(attempt < 3 ? 1 : 0);
`, { mode: 0o755 });
    const result = await runProcess('env', [`PATH=${bin}:${process.env.PATH}`, `TEST_RESULTS_DIR=${path.join(directory, 'results')}`, 'TEST_MAX_ATTEMPTS=3', 'bash', '.github/scripts/run-tests-with-retry.sh', 'Integration/Client/Client.csproj', 'FullyQualifiedName~Original', '--coverage'], {
        cwd: root, capture: true, timeout: 10000
    });
    assert.equal(result.code, 0, result.stdout + result.stderr);
    const calls = (await readFile(path.join(directory, 'calls'), 'utf8')).trim().split('\n').map(line => JSON.parse(line));
    assert.equal(calls.length, 3);
    assert.deepEqual(calls.map(args => args[args.indexOf('--filter') + 1]), [
        'FullyQualifiedName~Original', 'FullyQualifiedName=Example.A|FullyQualifiedName=Example.B', 'FullyQualifiedName=Example.A'
    ]);
    assert.equal(new Set(calls.map(args => args[args.indexOf('--results-directory') + 1])).size, 3);
});
