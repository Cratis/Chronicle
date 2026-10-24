// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Mock collector contracts plus real local Node probe plumbing, NOT Linux collector proof.
import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, readFile } from 'node:fs/promises';
import path from 'node:path';
import { v3Projects, shippingProjects, ordinaryFilter, childCommand, invocation,
    validateTrx, validateCoverage, validateSelection, parseXml, runProcess, runProject, main } from './run-mixed-specs.mjs';
import { collectorArgs, verifyExit, verifyDeadline, main as verifyCollector } from './verify-mixed-specs-collector.mjs';
import './spec-project-contracts.mjs';
import { benchmarkProject, inventory, ordinaryProjects, validateSolutionProjects } from './mixed-specs-selection.mjs';

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
    const testing = projectPath === shippingProjects[0];
    const packages = [...(testing ? [] : ['xunit.v3.extensibility.core']), ...(!shipping ? ['xunit.v3.mtp-off'] : []), ...(testing ? [] : ['cratis.specifications.xunit.v3'])];
    const libraries = Object.fromEntries(packages.map(id => [`${id}/1.2.3`, { type: 'package' }]));
    return {
        evaluated: { Properties: { IsTestProject: String(!shipping), OutputType: shipping ? 'Library' : 'Exe', TargetFramework: 'net10.0', TargetFrameworks: '', AssemblyName: 'Actual.Output', TargetPath: '/built/Actual.Output.dll' }, Items: { PackageReference: packages.map(Identity => ({ Identity })), PackageVersion: packages.map(Identity => ({ Identity, Version: '1.2.3' })) } },
        assets: { libraries }, deps: { libraries: structuredClone(libraries) }
    };
}

test('exact 31 executable v3 + 2 shipping boundary, not a Storage prefix heuristic', () => {
    assert.equal(v3Projects.length, 31);
    assert.equal(shippingProjects.length, 2);
    assert.equal(new Set([...v3Projects, ...shippingProjects]).size, 33);
    assert.ok(v3Projects.includes(benchmarkProject));
    for (const name of ['Aspire', 'Storage.Vault', 'Storage.AzureKeyVault']) assert.ok(v3Projects.some(x => x.endsWith(`/${name}.Specs.csproj`)));
    assert.equal(v3Projects.filter(x => x.startsWith('Integration/')).length, 6);
    for (const p of [...v3Projects, ...shippingProjects]) {
        const g = graph(p);
        const selected = validateSelection(p, g.evaluated, g.assets, g.deps);
        assert.equal(selected.runner, 'v3');
        assert.equal(selected.assemblyName, 'Actual.Output');
        assert.equal(selected.dll, '/built/Actual.Output.dll');
        assert.equal(selected.tfm, 'net10.0');
        assert.deepEqual(selected.coverageRoots, p === benchmarkProject ? ['Source', 'Benchmarks/Chronicle.Benchmarks.Clustering'] : ['Source']);
    }
});

test('xunit is required for spec projects and optional but consistent for shipping packages', () => {
    const [testing, integration] = shippingProjects;
    const plain = graph(testing);
    assert.equal(validateSelection(testing, plain.evaluated, plain.assets, plain.deps).project, testing);
    const withXunit = graph(integration);
    assert.equal(validateSelection(integration, withXunit.evaluated, withXunit.assets, withXunit.deps).project, integration);
    const halfXunit = graph(testing);
    halfXunit.assets.libraries['xunit.v3.extensibility.core/1.2.3'] = { type: 'package' };
    assert.throws(() => validateSelection(testing, halfXunit.evaluated, halfXunit.assets, halfXunit.deps), /resolved\/runtime mismatch for xunit.v3.extensibility.core/);
    const missingRunner = graph(v3Projects[0]);
    for (const libraries of [missingRunner.assets.libraries, missingRunner.deps.libraries]) delete libraries['xunit.v3.extensibility.core/1.2.3'];
    missingRunner.evaluated.Items.PackageReference = missingRunner.evaluated.Items.PackageReference.filter(x => x.Identity !== 'xunit.v3.extensibility.core');
    missingRunner.evaluated.Items.PackageVersion = missingRunner.evaluated.Items.PackageVersion.filter(x => x.Identity !== 'xunit.v3.extensibility.core');
    assert.throws(() => validateSelection(v3Projects[0], missingRunner.evaluated, missingRunner.assets, missingRunner.deps), /resolved\/runtime mismatch for xunit.v3.extensibility.core/);
});

test('solution completeness rejects an unclassified spec before any evaluation', async () => {
    const { root } = await fixture();
    for (const project of ['Future/Unclassified.Specs/Unclassified.Specs.csproj', 'Integration/Unclassified/Unclassified.csproj']) {
        await writeFile(path.join(root, 'Chronicle.slnx'), `<Solution><Folder Name="/Future/"><Project Path="${project}" /></Folder></Solution>`);
        await assert.rejects(inventory(root, () => assert.fail('must reject before evaluation')), error => error.message === `Unclassified test project ${project}`);
    }
    assert.throws(() => validateSolutionProjects('<Solution/>'), /no spec projects/);
});

test('classified Azure Key Vault and Vault stay ordinary with or without solution membership', async () => {
    const xml = await readFile(path.resolve('Chronicle.slnx'), 'utf8');
    const azure = 'Source/Kernel/Storage.AzureKeyVault.Specs/Storage.AzureKeyVault.Specs.csproj';
    const withoutAzure = xml.replace(new RegExp(`<Project Path="${azure.replaceAll('.', '\\.')}"\\s*/>`), '');
    for (const solution of [withoutAzure, withoutAzure.replace('</Solution>', `<Project Path="${azure}" /></Solution>`)]) {
        validateSolutionProjects(solution);
        const selected = ordinaryProjects(v3Projects.map(project => ({ project })));
        assert.ok(selected.some(info => info.project === azure));
        assert.ok(selected.some(info => info.project === 'Source/Kernel/Storage.Vault.Specs/Storage.Vault.Specs.csproj'));
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
        g => { g.evaluated.Properties.TargetPath = ''; },
        g => { g.evaluated.Properties.OutputType = 'Library'; },
        g => { delete g.evaluated.Properties.OutputType; }
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

test('dispatch rejects any non-v3 runner and can run without a collector', () => {
    assert.throws(() => invocation({ runner: 'v2' }, {}), /executable v3/);
    const call = invocation({ runner: 'v3', dll: '/built/SqlClustering.dll' }, { directory: '/unique', filter: 'Name=a', coverage: false });
    assert.deepEqual(call.args, ['/built/SqlClustering.dll', '-filterVSTest', 'Name=a', '-result-trx', '/unique/results.trx']);
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
    for (const segment of ['obj', 'bin']) {
        await mkdir(path.join(root, 'Source/Library', segment, 'Release/net10.0'), { recursive: true });
        await writeFile(path.join(root, 'Source/Library', segment, 'Release/net10.0/Generated.cs'), '// generated mock source');
    }
    assert.equal(await validateCoverage(coverage(), root), 'Source/Library/Decision.cs');
    assert.equal(await validateCoverage(coverage('/agent/repo/Source/Library/Decision.cs'), root), 'Source/Library/Decision.cs');
    for (const bad of ['', '<coverage><packages /></coverage>', '<coverage>', coverage('Source/Library.Specs/Test.cs'), coverage('Integration/Api/Test.cs'), coverage('Source/Library/given/Test.cs'), coverage('Source/Library/Decision.cs', '0'), coverage('Source/../outside.cs'), coverage('Source/Library/Missing.cs'), coverage('Source/Library/obj/Release/net10.0/Generated.cs'), coverage('/agent/repo/Source/Library/bin/Release/net10.0/Generated.cs'), coverage('Source\\Library\\obj\\Release\\net10.0\\Generated.cs')]) await assert.rejects(validateCoverage(bad, root));
});

test('benchmark production hits are allowed only for the benchmark selection, never generated/spec hits', async () => {
    const { root } = await fixture();
    const directory = 'Benchmarks/Chronicle.Benchmarks.Clustering';
    const roots = ['Source', directory];
    for (const source of [`${directory}/RebuildBaselineMath.cs`, `${directory}/obj/Generated.cs`, `${directory}/bin/Generated.cs`, `${directory}.Specs/Spec.cs`, 'Benchmarks/Other/Program.cs']) {
        await mkdir(path.dirname(path.join(root, source)), { recursive: true });
        await writeFile(path.join(root, source), '// mock source');
    }
    const source = `${directory}/RebuildBaselineMath.cs`;
    assert.equal(await validateCoverage(coverage(source), root, roots), source);
    assert.equal(await validateCoverage(coverage(`/agent/repo/${source}`), root, roots), source);
    await assert.rejects(validateCoverage(coverage(source), root), /no actual production-source hits/);
    for (const rejected of [`${directory}/obj/Generated.cs`, `${directory}/bin/Generated.cs`, `${directory}.Specs/Spec.cs`, 'Benchmarks/Other/Program.cs']) {
        await assert.rejects(validateCoverage(coverage(rejected), root, roots), /no actual production-source hits/);
    }
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

test('MOCK whole ordinary dispatch validates all 33, launches 24 once, keeps SQL/integration separate', async () => {
    const { root } = await fixture();
    await writeFile(path.join(root, 'Chronicle.slnx'), `<Solution>${v3Projects.filter(project => !project.includes('Storage.AzureKeyVault.Specs/')).map(project => `<Project Path="${project}" />`).join('')}</Solution>`);
    await mkdir(path.join(root, 'Benchmarks/Chronicle.Benchmarks.Clustering'), { recursive: true });
    await writeFile(path.join(root, 'Benchmarks/Chronicle.Benchmarks.Clustering/RebuildBaselineMath.cs'), '// mock benchmark production source');
    await mkdir(path.join(root, 'Integration'));
    await writeFile(path.join(root, 'specs.runsettings'), '<RunSettings><TestSessionTimeout>1234</TestSessionTimeout><TestCaseFilter>Name!~excluded&amp;Name!~also</TestCaseFilter></RunSettings>');
    const queried = [], launched = [];
    let benchmarkDll;
    const execute = async (_, args, options) => {
        if (args[0] === 'msbuild') {
            queried.push(args[1]);
            const g = graph(args[1]), id = queried.length;
            const dll = path.join(root, `Assembly${id}.dll`);
            const assets = path.join(root, `assets${id}.json`);
            Object.assign(g.evaluated.Properties, { AssemblyName: `Assembly${id}`, TargetPath: dll, ProjectAssetsFile: assets });
            if (args[1] === benchmarkProject) benchmarkDll = dll;
            await writeFile(dll, 'mock');
            await writeFile(dll.replace('.dll', '.deps.json'), JSON.stringify(g.deps));
            await writeFile(assets, JSON.stringify(g.assets));
            return { code: 0, stdout: JSON.stringify(g.evaluated) };
        }
        assert.equal(queried.length, 33, 'validate every selection before any child');
        launched.push(args);
        assert.equal(options.timeout, 1234);
        const directory = path.dirname(args[8]);
        const filter = args[9];
        assert.ok(filter.includes(ordinaryFilter));
        assert.ok(filter.includes('Name!~excluded&Name!~also'));
        await writeFile(path.join(directory, 'results.trx'), trx());
        const source = args[9].includes(benchmarkDll) ? 'Benchmarks/Chronicle.Benchmarks.Clustering/RebuildBaselineMath.cs' : 'Source/Library/Decision.cs';
        await writeFile(path.join(directory, 'coverage.cobertura.xml'), coverage(source));
        return { code: 0 };
    };
    await main([], { root, execute });
    assert.deepEqual(queried, [...v3Projects, ...shippingProjects]);
    assert.equal(launched.length, 24);
    assert.equal(launched.filter(args => args[0] === 'tool').length, 24);
    assert.equal(new Set(launched.map(args => args[8])).size, 24);
    assert.ok(launched.some(args => args[9].includes(benchmarkDll)));
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

const probeScript = path.resolve('.github/scripts/verify-mixed-specs-collector.mjs');
test('MOCK collector acceptance: exact 37 over the documented single-child grammar, wrong/zero exit rejected', async () => {
    const args = collectorArgs('/out', ['exit', '37']);
    assert.deepEqual(args.slice(0, 9), ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', '/out/probe.cobertura.xml']);
    assert.equal(args.length, 10);
    assert.equal(args.at(-1), childCommand([process.execPath, probeScript, '--probe-child', 'exit', '37']));
    await verifyExit('/out', async (command, actual, options) => {
        assert.equal(command, 'dotnet');
        assert.deepEqual(actual, args);
        assert.deepEqual(options, { timeout: 60000, capture: true });
        return { code: 37 };
    });
    for (const code of [0, 1, 36]) await assert.rejects(verifyExit('/out', async () => ({ code })), /exact child exit 37/);
});

const deadlineExecute = exitCode => async () => { throw Object.assign(new Error('deadline'), { exitCode }); };
async function attemptDeadline(pids, { alive = () => false, exitCode = 124 } = {}) {
    const { directory } = await fixture();
    await writeFile(path.join(directory, 'owned.pids'), pids);
    return verifyDeadline(directory, { execute: deadlineExecute(exitCode), alive, grace: 200 });
}

test('MOCK collector acceptance: deadline needs 124, both owned PIDs recorded and none alive', async () => {
    await attemptDeadline('111\n222\n');
    await assert.rejects(attemptDeadline('111\n222\n', { exitCode: 1 }), /exit 124/);
    await assert.rejects(attemptDeadline('111\n'), /both owned PIDs/);
    await assert.rejects(attemptDeadline(''), /both owned PIDs/);
    await assert.rejects(attemptDeadline('999999991\n999999992\n', { alive: pid => pid === 999999992 }), /alive: 999999992/);
    const { directory } = await fixture();
    await assert.rejects(verifyDeadline(directory, { execute: async () => ({ code: 0 }) }), /got completed/);
    await assert.rejects(verifyDeadline(directory, { execute: deadlineExecute(124) }), /both owned PIDs.*incomplete/);
    await assert.rejects(verifyCollector(['--probe-dll', '/old.dll']), /Usage/);
});

test('LOCAL probe child returns exact exit 37 without a collector', async () => {
    const result = await runProcess(process.execPath, [probeScript, '--probe-child', 'exit', '37'], { timeout: 2000, capture: true });
    assert.equal(result.code, 37);
});

test('LOCAL probe child records both owned PIDs and the external deadline cleans them up without a collector', async () => {
    const { directory } = await fixture();
    const pidFile = path.join(directory, 'owned.pids');
    await verifyDeadline(directory, {
        deadline: 1000,
        grace: 2000,
        execute: (command, args, options) => {
            assert.equal(command, 'dotnet');
            assert.deepEqual(args, collectorArgs(directory, ['hang', pidFile]));
            return runProcess(process.execPath, [probeScript, '--probe-child', 'hang', pidFile], options);
        }
    });
    const pids = (await readFile(pidFile, 'utf8')).trim().split('\n').map(Number);
    assert.equal(pids.length, 2);
    assert.ok(pids.every(pid => Number.isSafeInteger(pid) && pid > 0 && pid !== process.pid));
    assert.notEqual(pids[0], pids[1]);
});
