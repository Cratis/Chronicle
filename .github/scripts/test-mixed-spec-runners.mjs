// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// MOCK contracts only: these do not prove the real Linux collector/child contract.
import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, readFile } from 'node:fs/promises';
import path from 'node:path';
import { v3Projects, v2Projects, shippingProjects, ordinaryFilter, childCommand, invocation,
    validateTrx, validateCoverage, validateSelection, parseXml, runProcess, runProject, main } from './run-mixed-specs.mjs';

const scratch = path.resolve('.ai-work/chronicle-spec5-runners/mock-contracts');
async function fixture() {
    await mkdir(scratch, { recursive: true });
    const root = await mkdtemp(path.join(scratch, 'case-'));
    await mkdir(path.join(root, 'Source', 'Library'), { recursive: true });
    await writeFile(path.join(root, 'Source', 'Library', 'Decision.cs'), '// mock production source');
    const directory = path.join(root, 'output');
    await mkdir(directory);
    const dll = path.join(root, 'Different.Assembly.dll');
    await writeFile(dll, 'mock, never executed');
    return { root, directory, filter: ordinaryFilter, timeout: 2000, dll };
}
const trx = (outcome = 'Passed', name = 'Specs.theory(value: &quot;a&amp;b&quot;)') => `<?xml version="1.0"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testId="case1" testName="${name}" outcome="${outcome}" /></Results><TestDefinitions><UnitTest id="case1"><TestMethod className="Specs.theory" name="case" /></UnitTest></TestDefinitions><ResultSummary><Counters total="1" executed="1" passed="${outcome === 'Passed' ? 1 : 0}" failed="${outcome === 'Failed' ? 1 : 0}" error="0" /></ResultSummary></TestRun>`;
const coverage = (filename = 'Source/Library/Decision.cs', hits = '1') => `<coverage><packages><package><classes><class filename="${filename}"><lines><line number="1" hits="${hits}" /></lines></class></classes></package></packages></coverage>`;
function graph(projectPath = v3Projects[0]) {
    const shipping = shippingProjects.includes(projectPath);
    const v3 = v3Projects.includes(projectPath);
    const packages = v3 ? ['xunit.v3.extensibility.core', 'xunit.v3.mtp-off', 'cratis.specifications.xunit.v3'] : ['xunit.extensibility.core', ...(projectPath === shippingProjects[0] ? [] : ['cratis.specifications.xunit'])];
    const libraries = Object.fromEntries(packages.map(id => [`${id}/1.2.3`, { type: 'package' }]));
    return {
        evaluated: { Properties: { IsTestProject: String(!shipping), TargetFramework: 'net10.0', TargetFrameworks: '', AssemblyName: 'Actual.Output', TargetPath: '/built/Actual.Output.dll' }, Items: { PackageReference: packages.map(Identity => ({ Identity })), PackageVersion: packages.map(Identity => ({ Identity, Version: '1.2.3' })) } },
        assets: { libraries }, deps: { libraries: structuredClone(libraries) }
    };
}

test('exact 18 v3 + 11 v2 + 2 shipping boundary, not a Storage prefix heuristic', () => {
    assert.equal(v3Projects.length, 18);
    assert.equal(v2Projects.length, 11);
    assert.equal(shippingProjects.length, 2);
    assert.equal(new Set([...v3Projects, ...v2Projects, ...shippingProjects]).size, 31);
    for (const name of ['Aspire', 'Storage.Vault', 'Storage.AzureKeyVault']) assert.ok(v3Projects.some(x => x.endsWith(`/${name}.Specs.csproj`)));
    assert.equal(v2Projects.filter(x => x.startsWith('Integration/')).length, 6);
    for (const p of [...v3Projects, ...v2Projects, ...shippingProjects]) {
        const g = graph(p);
        const selected = validateSelection(p, g.evaluated, g.assets, g.deps);
        assert.equal(selected.runner, v3Projects.includes(p) ? 'v3' : 'v2');
        assert.equal(selected.assemblyName, 'Actual.Output');
        assert.equal(selected.dll, '/built/Actual.Output.dll');
        assert.equal(selected.tfm, 'net10.0');
    }
});

test('selection rejects mixed cores, missing assets, stale versions and local flag ambiguity', () => {
    for (const mutate of [
        g => { g.assets.libraries['xunit.extensibility.core/2.0.0'] = {}; },
        g => { g.deps.libraries = {}; },
        g => { g.evaluated.Items.PackageReference = []; },
        g => { g.evaluated.Items.PackageVersion[0].Version = '9.0.0'; },
        g => { g.evaluated.Properties.IsTestProject = 'false'; },
        g => { g.evaluated.Properties.TargetFrameworks = 'net8.0;net10.0'; },
        g => { g.evaluated.Properties.TargetPath = ''; }
    ]) {
        const g = graph(); mutate(g);
        assert.throws(() => validateSelection(v3Projects[0], g.evaluated, g.assets, g.deps));
    }
    const g = graph();
    assert.throws(() => validateSelection('Source/New.Specs/New.Specs.csproj', g.evaluated, g.assets, g.deps));
});

test('documented collector grammar has one safely quoted literal child argument', () => {
    const info = { runner: 'v3', dll: '/a folder/Different.Assembly.dll' };
    const filter = 'FullyQualifiedName~a&FullyQualifiedName!~b|Name="quoted"';
    const call = invocation(info, { root: '/repo', directory: '/fresh output', filter, timeout: 123 });
    assert.deepEqual(call.args.slice(0, 9), ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', '/fresh output/coverage.cobertura.xml']);
    assert.equal(call.args.length, 10);
    assert.equal(call.args[9], '"dotnet" "/a folder/Different.Assembly.dll" "-filterVSTest" "FullyQualifiedName~a&FullyQualifiedName!~b|Name=\\"quoted\\"" "-result-trx" "/fresh output/results.trx"');
    assert.equal(childCommand(['a\\', 'a\\"b', '$(touch evil);&|']), '"a\\\\" "a\\\\\\"b" "$(touch evil);&|"');
    assert.throws(() => childCommand(['line\nbreak']));
    assert.equal(call.timeout, 123);
    assert.ok(!call.args.includes('--timeout'));
});

test('retained v2 preserves settings, collector, exact filter, evaluated TFM and no retry', () => {
    const filter = '(Name=a|Name=b)&Name!~c';
    const call = invocation({ runner: 'v2', project: v2Projects[1], tfm: 'net10.0' }, { root: '/repo', directory: '/unique', filter });
    assert.deepEqual(call.args, ['test', v2Projects[1], '--no-build', '--no-restore', '--configuration', 'Release', '--framework', 'net10.0', '--settings', '/repo/specs.runsettings', '--filter', filter, '--collect:XPlat Code Coverage', '--results-directory', '/unique', '--logger', 'trx;LogFileName=results.trx']);
});

test('TRX preserves escaped theory/display identities and definition linkage', () => {
    assert.deepEqual(validateTrx(trx()), ['Specs.theory(value: "a&b")']);
    for (const bad of [trx('Failed'), trx().replace('testId="case1"', 'testId="unknown"'), trx().replace('executed="1"', 'executed="0"'), trx().replace('total="1"', 'total="2"'), trx().replace('xmlns=', 'wrong='), trx().replace('className=', 'missing='), '<TestRun>', '']) assert.throws(() => validateTrx(bad));
    assert.throws(() => parseXml('<!DOCTYPE a><a />'));
    assert.throws(() => parseXml('<a><b></a>'));
    assert.throws(() => parseXml('<a id="&unknown;" />'));
    assert.throws(() => parseXml('<a /><'));
});

test('coverage fails closed for empty/malformed/missing/fixture-only/zero-hit reports', async () => {
    const { root } = await fixture();
    assert.equal(await validateCoverage(coverage(), root), 'Source/Library/Decision.cs');
    assert.equal(await validateCoverage(coverage('/agent/repo/Source/Library/Decision.cs'), root), 'Source/Library/Decision.cs');
    for (const bad of ['', '<coverage><packages /></coverage>', '<coverage>', coverage('Source/Library.Specs/Test.cs'), coverage('Integration/Api/Test.cs'), coverage('Source/Library/given/Test.cs'), coverage('Source/Library/Decision.cs', '0'), coverage('Source/../outside.cs'), coverage('Source/Library/Missing.cs')]) await assert.rejects(validateCoverage(bad, root));
});

test('MOCK collector nonzero 37 is exact; missing output cannot rescue or replace it', async () => {
    const options = await fixture();
    const info = { project: v3Projects[0], runner: 'v3', dll: options.dll };
    let calls = 0;
    await assert.rejects(runProject(info, options, async () => { calls++; return { code: 37 }; }), error => error.exitCode === 37);
    assert.equal(calls, 1);
    assert.equal((await runProcess(process.execPath, ['-e', 'process.exit(37)'], { timeout: 2000, capture: true })).code, 37);
});

test('MOCK success requires both fresh TRX and real source hits, once per invocation', async () => {
    for (const mode of ['valid', 'no-trx', 'no-coverage', 'empty-coverage', 'failed-trx']) {
        const options = await fixture();
        const info = { project: v3Projects[0], runner: 'v3', dll: options.dll };
        const execute = async () => {
            if (mode !== 'no-trx') await writeFile(path.join(options.directory, 'results.trx'), trx(mode === 'failed-trx' ? 'Failed' : 'Passed'));
            if (mode !== 'no-coverage') await writeFile(path.join(options.directory, 'coverage.cobertura.xml'), mode === 'empty-coverage' ? '<coverage><packages /></coverage>' : coverage());
            return { code: 0 };
        };
        if (mode === 'valid') await runProject(info, options, execute);
        else await assert.rejects(runProject(info, options, execute));
    }
});

test('MOCK stale results are rejected before launch', async () => {
    const options = await fixture();
    await writeFile(path.join(options.directory, 'results.trx'), trx());
    await assert.rejects(runProject({ project: v3Projects[0], runner: 'v3', dll: options.dll }, options, async () => assert.fail('must not launch')), /fresh and empty/);
});

test('MOCK whole ordinary dispatch validates all 31, launches 22 once, keeps SQL/integration separate', async () => {
    const { root } = await fixture();
    await mkdir(path.join(root, 'Integration'));
    await writeFile(path.join(root, 'specs.runsettings'), '<RunSettings><TestSessionTimeout>1234</TestSessionTimeout><TestCaseFilter>Name!~excluded&amp;Name!~also</TestCaseFilter></RunSettings>');
    const queried = [], launched = [];
    const execute = async (_, args, options) => {
        if (args[0] === 'msbuild') {
            queried.push(args[1]);
            const g = graph(args[1]), id = queried.length;
            const dll = path.join(root, `Assembly${id}.dll`);
            const assets = path.join(root, `assets${id}.json`);
            Object.assign(g.evaluated.Properties, { AssemblyName: `Assembly${id}`, TargetPath: dll, ProjectAssetsFile: assets });
            await writeFile(dll, 'mock');
            await writeFile(dll.replace('.dll', '.deps.json'), JSON.stringify(g.deps));
            await writeFile(assets, JSON.stringify(g.assets));
            return { code: 0, stdout: JSON.stringify(g.evaluated) };
        }
        assert.equal(queried.length, 31, 'validate every selection before any child');
        launched.push(args);
        assert.equal(options.timeout, 1234);
        const directory = args[0] === 'tool' ? path.dirname(args[8]) : args[args.indexOf('--results-directory') + 1];
        const filter = args[0] === 'tool' ? args[9] : args[args.indexOf('--filter') + 1];
        assert.ok(filter.includes(ordinaryFilter));
        assert.ok(filter.includes('Name!~excluded&Name!~also'));
        await writeFile(path.join(directory, 'results.trx'), trx());
        await writeFile(path.join(directory, 'coverage.cobertura.xml'), coverage());
        return { code: 0 };
    };
    await main([], { root, execute });
    assert.deepEqual(queried, [...v3Projects, ...v2Projects, ...shippingProjects]);
    assert.equal(launched.length, 22);
    assert.equal(launched.filter(args => args[0] === 'tool').length, 18);
    assert.equal(launched.filter(args => args[0] === 'test').length, 4);
    assert.equal(new Set(launched.map(args => args[0] === 'tool' ? args[8] : args[args.indexOf('--results-directory') + 1])).size, 22);
});

test('MOCK missing executable and missing DLL never succeed', async () => {
    await assert.rejects(runProcess('/nonexistent/mock-collector', [], { timeout: 2000 }), /ENOENT/);
    const options = await fixture();
    await assert.rejects(runProject({ project: v3Projects[0], runner: 'v3', dll: '/nonexistent/spec.dll' }, options, async () => assert.fail('must not launch')));
});

test('MOCK external deadline kills collector/child group and rejects incomplete coverage', async () => {
    const options = await fixture();
    const pidfile = path.join(options.root, 'descendant.pid');
    const script = `const {spawn}=require('node:child_process');const fs=require('node:fs');const p=spawn(process.execPath,['-e','setInterval(()=>{},1000)'],{stdio:'inherit'});fs.writeFileSync(${JSON.stringify(pidfile)},String(p.pid));setInterval(()=>{},1000);`;
    await assert.rejects(runProcess(process.execPath, ['-e', script], { timeout: 500, capture: true }), error => error.exitCode === 124);
    const pid = Number(await readFile(pidfile, 'utf8'));
    let live = true;
    for (let attempt = 0; attempt < 30 && live; attempt++) {
        await new Promise(resolve => setTimeout(resolve, 50));
        try { process.kill(pid, 0); } catch (error) { assert.equal(error.code, 'ESRCH'); live = false; }
    }
    assert.equal(live, false, `MOCK descendant ${pid} still live`);
    await assert.rejects(readFile(path.join(options.directory, 'coverage.cobertura.xml')));
});
