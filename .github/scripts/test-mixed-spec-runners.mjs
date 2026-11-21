// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Mock collector contracts plus real local Node probe plumbing, NOT Linux collector proof.
import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, writeFile, readFile } from 'node:fs/promises';
import path from 'node:path';
import { v3Projects, shippingProjects, shippingRunners, ordinaryFilter, childCommand, invocation,
    validateTrx, validateCoverage, validateSelection, parseXml, runProcess, runProject, main } from './run-mixed-specs.mjs';
import { collectorArgs, verifyExit, verifyDeadline, main as verifyCollector } from './verify-mixed-specs-collector.mjs';
import './spec-project-contracts.mjs';
import { benchmarkProject, inventory, ordinaryProjects, validateSolutionProjects } from './mixed-specs-selection.mjs';
import { ORDINARY_SHARDS, sourcePlan } from './spec-shards.mjs';
import { coreSpecFixture } from './core-spec-fixture.mjs';
import { coreUnitId } from './core-spec-partition.mjs';
import { selectProducerArtifacts, loadProducerEvidence } from './run-mixed-specs.mjs';
import { receiptFor, verifyReceipts } from './spec-shards.mjs';
import { SpecHangWatchdog } from './spec-hang-watchdog.mjs';

const scratch = path.resolve('.ai-work/chronicle-spec5-runners/mock-contracts');
async function fixture() {
    await mkdir(scratch, { recursive: true });
    const root = await mkdtemp(path.join(scratch, 'case-'));
    for (const project of [...v3Projects, ...shippingProjects]) {
        await mkdir(path.dirname(path.join(root, project)), { recursive: true });
        await writeFile(path.join(root, project), '<Project/>');
    }
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
    // Mirrors the real references: spec projects and XUnit.Integration.v3 are v3, XUnit.Integration is v2, Testing has none.
    const shipping = shippingProjects.includes(projectPath);
    const runner = shippingRunners[projectPath] ?? 'v3';
    const runnerPackages = { none: [], v2: ['xunit.extensibility.core', 'cratis.specifications.xunit'], v3: ['xunit.v3.extensibility.core', 'cratis.specifications.xunit.v3'] }[runner];
    const packages = [...runnerPackages, ...(!shipping ? ['xunit.v3.mtp-off'] : [])];
    const libraries = Object.fromEntries(packages.map(id => [`${id}/1.2.3`, { type: 'package' }]));
    return {
        evaluated: { Properties: { IsTestProject: String(!shipping), OutputType: shipping ? 'Library' : 'Exe', TargetFramework: 'net11.0', TargetFrameworks: '', AssemblyName: 'Actual.Output', TargetPath: '/built/Actual.Output.dll' }, Items: { PackageReference: packages.map(Identity => ({ Identity })), PackageVersion: packages.map(Identity => ({ Identity, Version: '1.2.3' })) } },
        assets: { libraries }, deps: { libraries: structuredClone(libraries) }
    };
}

test('exact 35 executable v3 + 3 shipping boundary, not a Storage prefix heuristic', () => {
    assert.equal(v3Projects.length, 35);
    assert.deepEqual(Object.values(shippingRunners), ['none', 'v2', 'v3']);
    assert.equal(new Set([...v3Projects, ...shippingProjects]).size, 38);
    assert.ok(v3Projects.includes(benchmarkProject));
    for (const name of ['Aspire', 'Storage.Vault', 'Storage.AzureKeyVault']) assert.ok(v3Projects.some(x => x.endsWith(`/${name}.Specs.csproj`)));
    assert.equal(v3Projects.filter(x => x.startsWith('Integration/')).length, 7);
    for (const p of [...v3Projects, ...shippingProjects]) {
        const g = graph(p);
        const selected = validateSelection(p, g.evaluated, g.assets, g.deps);
        assert.equal(selected.runner, 'v3');
        assert.equal(selected.assemblyName, 'Actual.Output');
        assert.equal(selected.dll, '/built/Actual.Output.dll');
        assert.equal(selected.tfm, 'net11.0');
        assert.deepEqual(selected.coverageRoots, p === benchmarkProject ? ['Source', 'Benchmarks/Chronicle.Benchmarks.Clustering'] : ['Source']);
    }
});

test('spec projects need xunit v3 and each shipping package resolves exactly its declared generation', () => {
    const [testing, integration, integrationV3] = shippingProjects;
    for (const p of shippingProjects) {
        const g = graph(p);
        assert.equal(validateSelection(p, g.evaluated, g.assets, g.deps).project, p);
    }
    const halfXunit = graph(testing);
    halfXunit.assets.libraries['xunit.v3.extensibility.core/1.2.3'] = { type: 'package' };
    assert.throws(() => validateSelection(testing, halfXunit.evaluated, halfXunit.assets, halfXunit.deps), /resolved\/runtime mismatch for xunit.v3.extensibility.core/);
    const bothGenerations = graph(integration);
    for (const libraries of [bothGenerations.assets.libraries, bothGenerations.deps.libraries]) libraries['xunit.v3.extensibility.core/1.2.3'] = { type: 'package' };
    assert.throws(() => validateSelection(integration, bothGenerations.evaluated, bothGenerations.assets, bothGenerations.deps), /mismatch for xunit.v3.extensibility.core/);
    const v2InV3Package = graph(integrationV3);
    for (const libraries of [v2InV3Package.assets.libraries, v2InV3Package.deps.libraries]) libraries['xunit.extensibility.core/2.9.3'] = { type: 'package' };
    assert.throws(() => validateSelection(integrationV3, v2InV3Package.evaluated, v2InV3Package.assets, v2InV3Package.deps), /mismatch for xunit.extensibility.core/);
    const missingV2Specs = graph(integration);
    missingV2Specs.evaluated.Items.PackageReference = missingV2Specs.evaluated.Items.PackageReference.filter(x => x.Identity !== 'cratis.specifications.xunit');
    assert.throws(() => validateSelection(integration, missingV2Specs.evaluated, missingV2Specs.assets, missingV2Specs.deps), /v2 specifications reference/);
    const missingRunner = graph(v3Projects[0]);
    for (const libraries of [missingRunner.assets.libraries, missingRunner.deps.libraries]) delete libraries['xunit.v3.extensibility.core/1.2.3'];
    missingRunner.evaluated.Items.PackageReference = missingRunner.evaluated.Items.PackageReference.filter(x => x.Identity !== 'xunit.v3.extensibility.core');
    missingRunner.evaluated.Items.PackageVersion = missingRunner.evaluated.Items.PackageVersion.filter(x => x.Identity !== 'xunit.v3.extensibility.core');
    assert.throws(() => validateSelection(v3Projects[0], missingRunner.evaluated, missingRunner.assets, missingRunner.deps), /resolved\/runtime mismatch for xunit.v3.extensibility.core/);
});

test('declared shipping generations match the package references in the real csproj files', async () => {
    for (const [projectPath, declared] of Object.entries(shippingRunners)) {
        const xml = await readFile(path.resolve(projectPath), 'utf8');
        const ids = [...xml.replace(/<!--[\s\S]*?-->/g, '').matchAll(/<PackageReference\s+Include="([^"]+)"/g)].map(match => match[1].toLowerCase());
        const v3 = ids.some(id => id === 'cratis.specifications.xunit.v3' || id.startsWith('xunit.v3'));
        const v2 = ids.some(id => id === 'cratis.specifications.xunit' || id === 'xunit' || id === 'xunit.extensibility.core');
        assert.ok(!(v2 && v3), `${projectPath} references both xunit generations`);
        assert.equal(v3 ? 'v3' : v2 ? 'v2' : 'none', declared, `${projectPath} csproj does not match its declared generation`);
    }
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
        g => { g.evaluated.Properties.TargetFrameworks = 'net10.0;net11.0'; },
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

test('documented collector grammar has one child argument with a bare executable and quoted arguments', () => {
    const info = { runner: 'v3', dll: '/a folder/Different.Assembly.dll' };
    const filter = 'FullyQualifiedName~a&FullyQualifiedName!~b|Name="quoted"';
    const call = invocation(info, { root: '/repo', directory: '/fresh output', filter, timeout: 123, classWatchdog: true });
    assert.deepEqual(call.args.slice(0, 9), ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', '/fresh output/coverage.cobertura.xml']);
    assert.equal(call.args.length, 10);
    assert.equal(call.args[9], 'dotnet "/a folder/Different.Assembly.dll" "-filterVSTest" "FullyQualifiedName~a&FullyQualifiedName!~b|Name=\\"quoted\\"" "-result-trx" "/fresh output/results.trx" "-noAutoReporters" "-noColor" "-reporter" "json" "-diagnostics" "-longRunning" "120"');
    assert.equal(childCommand('dotnet', ['a\\', 'a\\"b', '$(touch evil);&|']), 'dotnet "a\\\\" "a\\\\\\"b" "$(touch evil);&|"');
    assert.throws(() => childCommand('dotnet', ['line\nbreak']));
    for (const executable of ['"dotnet"', '/a folder/dotnet', 'dotnet;rm', '']) assert.throws(() => childCommand(executable, []), /plain path/);
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
        await mkdir(path.join(root, 'Source/Library', segment, 'Release/net11.0'), { recursive: true });
        await writeFile(path.join(root, 'Source/Library', segment, 'Release/net11.0/Generated.cs'), '// generated mock source');
    }
    assert.equal(await validateCoverage(coverage(), root), 'Source/Library/Decision.cs');
    assert.equal(await validateCoverage(coverage('/agent/repo/Source/Library/Decision.cs'), root), 'Source/Library/Decision.cs');
    for (const bad of ['', '<coverage><packages /></coverage>', '<coverage>', coverage('Source/Library.Specs/Test.cs'), coverage('Integration/Api/Test.cs'), coverage('Source/Library/given/Test.cs'), coverage('Source/Library/Decision.cs', '0'), coverage('Source/../outside.cs'), coverage('Source/Library/Missing.cs'), coverage('Source/Library/obj/Release/net11.0/Generated.cs'), coverage('/agent/repo/Source/Library/bin/Release/net11.0/Generated.cs'), coverage('Source\\Library\\obj\\Release\\net11.0\\Generated.cs')]) await assert.rejects(validateCoverage(bad, root));
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

test('MOCK collector nonzero 37 is exact and project progress is streamed before launch and after exit', async t => {
    const options = await fixture();
    const info = { project: v3Projects[0], runner: 'v3', dll: options.dll };
    const lines = [];
    t.mock.method(console, 'log', line => lines.push(line));
    let calls = 0;
    await assert.rejects(runProject(info, options, async (command, args, execution) => {
        assert.deepEqual(execution, { cwd: options.root, timeout: options.timeout });
        calls++;
        assert.deepEqual(lines, [`[specs] start project=${info.project} elapsed=0ms exit=pending`]);
        return { code: 37 };
    }), error => error.exitCode === 37);
    assert.equal(calls, 1);
    assert.equal(lines.length, 2);
    assert.ok(lines[1].startsWith(`[specs] finish project=${info.project} elapsed=`));
    assert.match(lines[1], /elapsed=\d+ms exit=37$/);
    assert.equal((await runProcess(process.execPath, ['-e', 'process.exit(37)'], { timeout: 2000, capture: true })).code, 37);
});

test('MOCK success requires both fresh TRX and real source hits, once per invocation', async t => {
    let lines;
    t.mock.method(console, 'log', line => lines.push(line));
    for (const mode of ['valid', 'no-trx', 'no-coverage', 'empty-coverage', 'failed-trx']) {
        const options = await fixture();
        const info = { project: v3Projects[0], runner: 'v3', dll: options.dll };
        lines = [];
        const execute = async () => {
            if (mode !== 'no-trx') await writeFile(path.join(options.directory, 'results.trx'), trx(mode === 'failed-trx' ? 'Failed' : 'Passed'));
            if (mode !== 'no-coverage') await writeFile(path.join(options.directory, 'coverage.cobertura.xml'), mode === 'empty-coverage' ? '<coverage><packages /></coverage>' : coverage());
            return { code: 0 };
        };
        if (mode === 'valid') await runProject(info, options, execute);
        else await assert.rejects(runProject(info, options, execute));
        const progress = lines.filter(line => line.startsWith('[specs]'));
        assert.equal(progress.length, 2);
        assert.ok(progress[1].endsWith(`exit=${mode === 'valid' ? 0 : 1}`));
    }
});

test('MOCK stale results are rejected before launch', async () => {
    const options = await fixture();
    await writeFile(path.join(options.directory, 'results.trx'), trx());
    await assert.rejects(runProject({ project: v3Projects[0], runner: 'v3', dll: options.dll }, options, async () => assert.fail('must not launch')), /fresh and empty/);
});

const retryContext = { sha: 'merge-sha', runId: '123', runAttempt: '2' };
function producerFixture(attempts = Array(ORDINARY_SHARDS).fill(1)) {
    const jobs = attempts.flatMap((attempt, index) => Array.from({ length: attempt }, (_, i) => ({
        id: (index + 1) * 10 + i, name: `specs-other (${index + 1})`, run_id: 123, run_attempt: i + 1,
        head_sha: 'head-sha', status: 'completed', conclusion: 'success',
        started_at: `2026-11-01T00:0${i + 1}:00Z`, completed_at: `2026-11-01T00:0${i + 1}:30Z`
    })));
    const artifacts = attempts.map((attempt, index) => ({
        id: index + 100, name: `coverage-results-specs-other-${index + 1}`, expired: false,
        created_at: `2026-11-01T00:0${attempt}:15Z`, workflow_run: { id: 123, head_sha: 'head-sha' }
    }));
    return [[{ jobs, total_count: jobs.length }], [{ artifacts, total_count: artifacts.length }]];
}
test('API evidence selects retained and mixed successful producer attempts, not aggregate attempt', () => {
    for (const attempts of [[1, 1, 1, 1], [1, 2, 1, 2], [2, 1, 2, 1], [2, 2, 2, 2]]) {
        const pages = producerFixture(attempts);
        for (const job of pages[0][0].jobs) {
            if (job.run_attempt < attempts[Number(job.name.match(/\d/)[0]) - 1]) job.conclusion = 'failure';
        }
        const evidence = selectProducerArtifacts(...pages, retryContext);
        assert.deepEqual(evidence.producers.map(p => p.runAttempt), attempts.map(String));
        assert.deepEqual(evidence.producers.map(p => p.artifactId), [100, 101, 102, 103]);
    }
});
test('API evidence accepts reusable workflow caller names, including retained and mixed attempts', () => {
    for (const attempts of [[1, 1, 1, 1], [1, 2, 2, 1]]) {
        const [jobs, artifacts] = producerFixture(attempts);
        for (const job of jobs[0].jobs) job.name = `requested-integration-5501 / ${job.name}`;
        assert.deepEqual(selectProducerArtifacts(jobs, artifacts, retryContext).producers.map(p => p.runAttempt), attempts.map(String));
    }
});
test('API evidence rejects ambiguous caller prefixes, including direct/reusable and disjoint attempts', () => {
    for (const prefix of ['', 'other-caller / ']) {
        const [jobs, artifacts] = producerFixture([1, 2, 1, 1]);
        const duplicate = { ...jobs[0].jobs[0], id: 999, name: `${prefix}specs-other (1)`, run_attempt: 2 };
        for (const job of jobs[0].jobs) job.name = `requested-integration-5501 / ${job.name}`;
        jobs[0].jobs.push(duplicate);
        jobs[0].total_count++;
        assert.throws(() => selectProducerArtifacts(jobs, artifacts, retryContext), /Ambiguous producer caller/);
    }
});
test('API evidence does not match unrelated job-name suffixes', () => {
    const [jobs, artifacts] = producerFixture();
    jobs[0].jobs[0].name = 'caller / xspecs-other (1)';
    assert.throws(() => selectProducerArtifacts(jobs, artifacts, retryContext), /Missing successful latest producer/);
});
test('API evidence rejects unsuccessful latest jobs and stale, wrong-run/SHA, incomplete or duplicate provenance', () => {
    const reject = mutate => {
        const [jobs, artifacts] = producerFixture([1, 2, 2, 1]);
        mutate(jobs[0], artifacts[0]);
        assert.throws(() => selectProducerArtifacts(jobs, artifacts, retryContext), /producer/i);
    };
    for (const conclusion of ['failure', 'cancelled', 'skipped', null]) reject(j => { j.jobs[2].conclusion = conclusion; });
    reject(j => { j.jobs[2].status = 'in_progress'; });
    for (const conclusion of ['failure', 'cancelled', 'skipped', null]) reject(j => { j.jobs[4].conclusion = conclusion; });
    reject((j, a) => { a.artifacts[2].created_at = '2026-11-01T00:01:15Z'; });
    reject(j => { j.jobs = j.jobs.filter(job => job.name !== 'specs-other (4)'); j.total_count = j.jobs.length; });
    reject((j, a) => { a.artifacts.push({ ...a.artifacts[2], name: `coverage-results-specs-other-${ORDINARY_SHARDS + 1}` }); a.total_count++; });
    reject(j => { j.jobs[2].run_attempt = 3; });
    reject(j => { j.jobs[0].run_id = 999; });
    reject((j, a) => { a.artifacts[0].workflow_run.id = 999; });
    reject((j, a) => { a.artifacts[0].workflow_run.head_sha = 'wrong-sha'; });
    reject((j, a) => { a.artifacts[1].created_at = '2026-11-01T00:01:15Z'; });
    reject((j, a) => { a.artifacts[0].expired = true; });
    reject((j, a) => { a.artifacts[0].created_at = 'invalid'; });
    reject((j, a) => { a.artifacts[0].created_at = '2026-11-01T00:03:00Z'; });
    reject(j => { j.jobs = j.jobs.filter(job => job.name !== 'specs-other (1)'); j.total_count = j.jobs.length; });
    reject((j, a) => { a.artifacts.pop(); a.total_count--; });
    reject(j => { j.jobs.pop(); });
    reject(j => { j.jobs.push({ ...j.jobs[0], id: 999 }); j.total_count++; });
    reject((j, a) => { a.artifacts.push({ ...a.artifacts[0], id: 999 }); a.total_count++; });
});
test('API identity failures name the offending ids, records and attempts', () => {
    const [jobs, artifacts] = producerFixture();
    const twin = { ...artifacts[0].artifacts[0], run_attempt: 2 };
    artifacts[0].artifacts.push(twin);
    artifacts[0].total_count++;
    assert.throws(() => selectProducerArtifacts(jobs, artifacts, retryContext),
        /Invalid\/duplicate producer API identities: artifacts .*duplicate id 100 \(coverage-results-specs-other-1 attempt n\/a, coverage-results-specs-other-1 attempt 2\)/);
    const [badJobs, goodArtifacts] = producerFixture();
    badJobs[0].jobs[0].id = -5;
    assert.throws(() => selectProducerArtifacts(badJobs, goodArtifacts, retryContext),
        /jobs .*invalid id -5 \(specs-other \(1\) attempt 1\)/);
});
test('API identity failures format malformed JSON values safely and cap the list', () => {
    for (const mutate of [job => { job.id = -1; job.name = { toString: null }; }, job => { job.id = { toString: null }; }]) {
        const [jobs, artifacts] = producerFixture();
        mutate(jobs[0].jobs[0]);
        assert.throws(() => selectProducerArtifacts(jobs, artifacts, retryContext),
            error => !(error instanceof TypeError) && /^Invalid\/duplicate producer API identities: jobs/.test(error.message));
    }
    const [jobs, artifacts] = producerFixture();
    jobs[0].jobs.forEach(job => { job.id = -job.id; });
    for (let i = jobs[0].jobs.length; i < 12; i++) jobs[0].jobs.push({ id: -(i + 1000), name: `extra-${i}`, run_attempt: 1 });
    jobs[0].total_count = jobs[0].jobs.length;
    assert.throws(() => selectProducerArtifacts(jobs, artifacts, retryContext), error => {
        assert.equal(error.message.split('; ').length, 11);
        return error.message.endsWith('; +2 more') && (error.message.match(/invalid id/g) ?? []).length === 10;
    });
});
test('API identity validation accepts failed-job re-run artifacts with distinct ids', () => {
    const [jobs, artifacts] = producerFixture([1, 2, 1, 1]);
    artifacts[0].artifacts.push({ ...artifacts[0].artifacts[1], id: 900, created_at: '2026-11-01T00:01:15Z' });
    artifacts[0].total_count++;
    // Identity validation passes; the later per-shard artifact rules still reject the extra same-name artifact.
    assert.throws(() => selectProducerArtifacts(jobs, artifacts, retryContext), error => !/identities/.test(error.message) && /producer artifact/i.test(error.message));
});
test('producer API acquisition paginates latest attempts and propagates unavailable/malformed evidence', async () => {
    const context = { ...retryContext, runAttempt: '1' };
    const pages = producerFixture();
    const jobs = pages[0][0].jobs;
    pages[0] = [{ jobs: jobs.slice(0, 1), total_count: jobs.length }, { jobs: jobs.slice(1), total_count: jobs.length }];
    let calls = 0;
    const options = { env: { GITHUB_REPOSITORY: 'Cratis/Chronicle' }, execute: async (command, args) => {
        assert.equal(command, 'gh');
        assert.deepEqual(args.slice(0, 3), ['api', '--paginate', '--slurp']);
        if (calls === 0) assert.ok(args[3].endsWith('/123/jobs?filter=latest&per_page=30'));
        return { code: 0, stdout: JSON.stringify(pages[calls++]) };
    } };
    assert.equal((await loadProducerEvidence(context, options)).producers[1].runAttempt, '1');
    assert.equal(calls, 2);
    await assert.rejects(loadProducerEvidence(context, { ...options, execute: async () => ({ code: 1 }) }), /API failed/);
    await assert.rejects(loadProducerEvidence(context, { ...options, execute: async () => ({ code: 0, stdout: '{}' }) }), /pages/);
});
test('producer API retries a server error on a page, then reports the captured response', async () => {
    const context = { ...retryContext, runAttempt: '1' };
    const pages = producerFixture();
    const serverError = { code: 1, stdout: '', stderr: 'gh: Server Error (HTTP 502)' };
    const env = { GITHUB_REPOSITORY: 'Cratis/Chronicle' };
    let calls = 0;
    const waits = [];
    const wait = async milliseconds => { waits.push(milliseconds); };
    const recovering = async () => (calls++ === 0 ? serverError : { code: 0, stdout: JSON.stringify(pages[calls - 2]) });
    assert.equal((await loadProducerEvidence(context, { env, wait, execute: recovering })).producers[1].runAttempt, '1');
    assert.equal(calls, 3);
    assert.deepEqual(waits, [5000]);

    let failing = 0;
    waits.length = 0;
    await assert.rejects(loadProducerEvidence(context, { env, wait, execute: async () => { failing++; return serverError; } }),
        /API failed \(1\): gh: Server Error \(HTTP 502\)/);
    assert.equal(failing, 3);
    assert.deepEqual(waits, [5000, 10000]);

    const long = `gh: Server Error (HTTP 503) ${'x'.repeat(3000)}`;
    await assert.rejects(loadProducerEvidence(context, { env, wait, execute: async () => ({ code: 1, stderr: long }) }),
        error => error.message.endsWith(long.slice(-2000)) && !error.message.includes('HTTP 503'));

    let clock = 0;
    const timeouts = [];
    let slow = 0;
    const slowServer = async (command, args, options) => { slow++; timeouts.push(options.timeout); clock += 145000; return serverError; };
    await assert.rejects(loadProducerEvidence(context, { env, wait, now: () => clock, execute: slowServer }), /HTTP 502/);
    assert.equal(slow, 1, 'no retry once the shared acquisition budget is spent');
    assert.deepEqual(timeouts, [60000]);

    let forbidden = 0;
    await assert.rejects(loadProducerEvidence(context, { env, wait, execute: async () => { forbidden++; return { code: 1, stderr: 'gh: Forbidden (HTTP 403)' }; } }),
        /API failed \(1\): gh: Forbidden \(HTTP 403\)/);
    assert.equal(forbidden, 1);
});

// Projected from run 44301164153 attempts 1/2: retained jobs have new ids and
// run_attempt values, but preserve BOTH execution timestamps (unlike producerFixture).
function carriedProducerFixture({ sqlOnly = false, allCarried = false } = {}) {
    const context = { sha: '6fdcc813d9242b82a5a4c4127f5706c1bf894ab9', runId: '44301164153', runAttempt: allCarried ? '3' : '2' };
    const executions = [
        [134793518341, '12:13:49', '12:20:18'], [134793518352, '12:13:48', '13:01:49'],
        [134793518329, '12:13:48', '12:18:01'], [134793518350, '12:13:49', '12:19:30']
    ];
    const first = executions.map(([id, start, end], index) => ({
        id, name: `specs-other (${index + 1})`, run_id: Number(context.runId), run_attempt: 1,
        head_sha: context.sha, status: 'completed', conclusion: index === 1 && !sqlOnly ? 'failure' : 'success',
        started_at: `2026-11-18T${start}Z`, completed_at: `2026-11-18T${end}Z`
    }));
    const copiedIds = [134822580502, 134822578447, 134822581075, 134822582522];
    const second = first.map((job, index) => ({ ...job, id: copiedIds[index], run_attempt: 2 }));
    if (!sqlOnly) Object.assign(second[1], { started_at: '2026-11-18T13:06:32Z', completed_at: '2026-11-18T13:12:45Z', conclusion: 'success' });
    // Model a further failed-jobs/SQL-only rerun retaining every ordinary execution.
    const latest = allCarried ? second.map((job, index) => ({ ...job, id: 134830278864 + index, run_attempt: 3 })) : second;
    const page = (key, records) => [{ [key]: records, total_count: records.length }];
    const artifacts = second.map((job, index) => ({
        id: index + 100, name: `coverage-results-specs-other-${index + 1}`, expired: false,
        created_at: new Date(Date.parse(job.completed_at) - 1000).toISOString(),
        workflow_run: { id: Number(context.runId), head_sha: context.sha }
    }));
    const endpoints = {
        'jobs?filter=latest&per_page=30': page('jobs', latest), 'artifacts?per_page=30': page('artifacts', artifacts),
        'attempts/1/jobs?per_page=30': page('jobs', first), 'attempts/2/jobs?per_page=30': page('jobs', second)
    };
    return { context, endpoints };
}
function carriedProducerOptions(fixture) {
    const calls = [];
    return { calls, env: { GITHUB_REPOSITORY: 'Cratis/Chronicle' }, execute: async (command, args) => {
        assert.equal(command, 'gh');
        const endpoint = args[3].split(`/runs/${fixture.context.runId}/`)[1];
        calls.push(endpoint);
        assert.ok(fixture.endpoints[endpoint], `Unexpected endpoint: ${endpoint}`);
        return { code: 0, stdout: JSON.stringify(fixture.endpoints[endpoint]) };
    } };
}
for (const [name, settings, expected] of [
    ['mixed failed-jobs retry', {}, ['1', '2', '1', '1']],
    ['all ordinary jobs carried forward again', { allCarried: true }, ['1', '2', '1', '1']],
    ['SQL-only retry', { sqlOnly: true }, ['1', '1', '1', '1']]
]) {
    test(`producer acquisition resolves actual execution attempts: ${name}`, async () => {
        const fixture = carriedProducerFixture(settings);
        const options = carriedProducerOptions(fixture);
        const evidence = await loadProducerEvidence(fixture.context, options);
        assert.deepEqual(evidence.producers.map(producer => producer.runAttempt), expected);
        assert.deepEqual(evidence.producers.map(producer => producer.artifactId), [100, 101, 102, 103]);
        assert.ok(options.calls.includes('artifacts?per_page=30'), 'artifacts are listed across the whole run, not just the retry');
        assert.deepEqual(options.calls.slice(2), settings.allCarried
            ? ['attempts/2/jobs?per_page=30', 'attempts/1/jobs?per_page=30'] : ['attempts/1/jobs?per_page=30']);
        const plan = evidence.producers.map(({ shard }) => ({ shard, projects: [v3Projects[shard - 1]] }));
        const receipts = plan.map((shard, index) => receiptFor(shard, shard.projects, { ...fixture.context, runAttempt: expected[index] }));
        assert.doesNotThrow(() => verifyReceipts(receipts, plan, fixture.context, evidence.producers));
        const wrongAttempt = structuredClone(receipts);
        wrongAttempt[0].context.runAttempt = fixture.context.runAttempt;
        assert.throws(() => verifyReceipts(wrongAttempt, plan, fixture.context, evidence.producers), /context/);
    });
}
test('producer acquisition accepts a rerun after an upstream build left the matrix unexpanded', async () => {
    const fixture = carriedProducerFixture({ allCarried: true });
    const page = fixture.endpoints['attempts/2/jobs?per_page=30'][0];
    const job = page.jobs[0];
    page.jobs = [
        { ...job, name: 'build', conclusion: 'failure' },
        { ...job, id: 999, name: 'specs-other', conclusion: 'skipped', started_at: null, completed_at: null }
    ];
    page.total_count = page.jobs.length;
    const options = carriedProducerOptions(fixture);
    const evidence = await loadProducerEvidence(fixture.context, options);
    assert.deepEqual(evidence.producers.map(producer => producer.runAttempt), ['3', '3', '3', '3']);
    assert.deepEqual(options.calls.slice(2), ['attempts/2/jobs?per_page=30']);
});
test('producer acquisition resolves an attempt-2 execution carried into 3 but absent from 1', async () => {
    const fixture = carriedProducerFixture({ allCarried: true });
    const page = fixture.endpoints['attempts/1/jobs?per_page=30'][0];
    page.jobs.splice(1, 1);
    page.total_count--;
    const evidence = await loadProducerEvidence(fixture.context, carriedProducerOptions(fixture));
    assert.deepEqual(evidence.producers.map(producer => producer.runAttempt), ['1', '2', '1', '1']);
    assert.equal(evidence.producers[1].artifactId, 101);
});
test('producer acquisition stops fetching history once every shard has a different execution', async () => {
    const fixture = carriedProducerFixture({ allCarried: true });
    for (const job of fixture.endpoints['attempts/2/jobs?per_page=30'][0].jobs) {
        job.started_at = new Date(Date.parse(job.started_at) + 1000).toISOString();
    }
    const options = carriedProducerOptions(fixture);
    const evidence = await loadProducerEvidence(fixture.context, options);
    assert.deepEqual(evidence.producers.map(producer => producer.runAttempt), ['3', '3', '3', '3']);
    assert.deepEqual(options.calls.slice(2), ['attempts/2/jobs?per_page=30']);
});
test('producer acquisition does not inspect older records for an already resolved shard', async () => {
    const fixture = carriedProducerFixture({ allCarried: true });
    fixture.endpoints['attempts/2/jobs?per_page=30'][0].jobs[1].started_at = '2026-11-18T13:06:33Z';
    const page = fixture.endpoints['attempts/1/jobs?per_page=30'][0];
    page.jobs[1].run_id = 999;
    page.jobs.push({ ...page.jobs[1], id: 999 });
    page.total_count++;
    const evidence = await loadProducerEvidence(fixture.context, carriedProducerOptions(fixture));
    assert.deepEqual(evidence.producers.map(producer => producer.runAttempt), ['1', '3', '1', '1']);
});
test('producer acquisition fails closed for unavailable, incomplete or inconsistent attempt history', async () => {
    const reject = async (mutate, pattern = /producing attempt|producer/i) => {
        const fixture = carriedProducerFixture();
        mutate(fixture.endpoints['attempts/1/jobs?per_page=30'][0]);
        await assert.rejects(loadProducerEvidence(fixture.context, carriedProducerOptions(fixture)), pattern);
    };
    await reject(page => { page.jobs.pop(); }, /pagination/);
    await reject(page => { page.jobs.push({ ...page.jobs[0], id: 999 }); page.total_count++; }, /duplicate job/);
    for (const patch of [{ run_id: 999 }, { run_attempt: 2 }, { head_sha: 'wrong-sha' }, { name: 'caller / specs-other (1)' },
        { status: 'in_progress' }, { conclusion: 'failure' }]) {
        await reject(page => { Object.assign(page.jobs[0], patch); });
    }
    const fixture = carriedProducerFixture();
    const options = carriedProducerOptions(fixture);
    const execute = async (command, args) => args[3].includes('/attempts/')
        ? { code: 1, stderr: 'gh: Not Found (HTTP 404)' } : options.execute(command, args);
    await assert.rejects(loadProducerEvidence(fixture.context, { ...options, execute }), /API failed.*404/);
});
test('producer history cannot run past the shared acquisition budget', async () => {
    const fixture = carriedProducerFixture();
    const options = carriedProducerOptions(fixture);
    let clock = 0;
    const execute = async (...args) => {
        const result = await options.execute(...args);
        clock += 75000;
        return result;
    };
    await assert.rejects(loadProducerEvidence(fixture.context, { ...options, execute, now: () => clock }), /budget exhausted/);
    assert.equal(options.calls.length, 2);
});

async function ordinaryDispatch(mode) {
    const { root } = await fixture();
    await coreSpecFixture(root);
    await writeFile(path.join(root, 'Chronicle.slnx'), `<Solution>${v3Projects.filter(project => !project.includes('Storage.AzureKeyVault.Specs/')).map(project => `<Project Path="${project}" />`).join('')}</Solution>`);
    await mkdir(path.join(root, 'Benchmarks/Chronicle.Benchmarks.Clustering'), { recursive: true });
    await writeFile(path.join(root, 'Benchmarks/Chronicle.Benchmarks.Clustering/RebuildBaselineMath.cs'), '// mock benchmark production source');
    await mkdir(path.join(root, 'Integration'), { recursive: true });
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
        assert.equal(queried.length, 38, 'validate every selection before any child');
        launched.push(args);
        const directory = path.dirname(args[8]);
        assert.deepEqual(options.specProgress, { directory });
        assert.ok(args[9].endsWith('"-noAutoReporters" "-noColor" "-reporter" "json" "-diagnostics" "-longRunning" "120"'));
        if (mode === 'failed-shard') return { code: 7 };
        assert.equal(options.timeout, 1234);
        const filter = args[9];
        assert.ok(filter.includes(ordinaryFilter));
        assert.ok(filter.includes('Name!~excluded&Name!~also'));
        await writeFile(path.join(directory, 'results.trx'), trx());
        const source = args[9].includes(benchmarkDll) ? 'Benchmarks/Chronicle.Benchmarks.Clustering/RebuildBaselineMath.cs' : 'Source/Library/Decision.cs';
        await writeFile(path.join(directory, 'coverage.cobertura.xml'), coverage(source));
        return { code: 0 };
    };
    const shardId = mode.startsWith('shard-') ? Number(mode.slice(-1)) : 1;
    const args = mode === 'whole' ? [] : ['--shard', String(shardId), '--shards', String(ORDINARY_SHARDS)];
    const receiptPath = path.join(root, 'coverage-results', 'shard-receipt.json');
    if (mode === 'failed-shard') {
        await mkdir(path.dirname(receiptPath), { recursive: true });
        await writeFile(receiptPath, 'stale receipt must not survive failure');
        await assert.rejects(main(args, { root, execute }), error => error.exitCode === 7);
        assert.deepEqual(queried, [...v3Projects, ...shippingProjects]);
        await assert.rejects(readFile(receiptPath), error => error.code === 'ENOENT');
        return;
    }
    await main(args, { root, execute });
    assert.deepEqual(queried, [...v3Projects, ...shippingProjects]);
    const shard = (await sourcePlan(root)).find(s => s.shard === shardId);
    const expectedProjects = mode === 'whole' ? ordinaryProjects(queried.map(project => ({ project }))).map(x => x.project) : shard.projects;
    const actualProjects = launched.map(args => {
        const project = queried.find((project, index) => args[9].includes(path.join(root, `Assembly${index + 1}.dll`)));
        const part = args[8].match(/-core-part-(\d)\//)?.[1];
        if (part) {
            assert.ok(args[9].includes('FullyQualifiedName~'));
            assert.ok(args[9].includes('&('));
        }
        return part ? coreUnitId(Number(part)) : project;
    });
    assert.deepEqual([...actualProjects].sort(), [...expectedProjects].sort());
    const expected = expectedProjects.length;
    assert.equal(launched.length, expected);
    assert.equal(launched.filter(args => args[0] === 'tool').length, expected);
    assert.equal(new Set(launched.map(args => args[8])).size, expected);
    if (mode === 'whole') assert.ok(launched.some(args => args[9].includes(benchmarkDll)));
    else {
        const receipt = JSON.parse(await readFile(receiptPath, 'utf8'));
        assert.deepEqual(receipt.completed, shard.projects);
        assert.deepEqual(receipt.projects, shard.projects);
    }
}
for (const mode of ['whole', 'shard-1', 'shard-2', 'shard-3', 'shard-4', 'failed-shard']) {
    test(`MOCK ${mode} dispatch validates all 38 before children and preserves coverage guards`, () => ordinaryDispatch(mode));
}

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

// Native xUnit JSON uses $type and PascalCase metadata; finish messages only
// carry IDs. In particular a shared fixture can hang before any test-starting.
const classStarting = (id = 'class-1', name = 'Specs.when_shared_fixture_hangs') => ({
    $type: 'test-class-starting', TestClassUniqueID: id, TestClassName: name
});
const classFinished = (id = 'class-1') => ({ $type: 'test-class-finished', TestClassUniqueID: id });
const testStarting = { $type: 'test-starting', TestClassUniqueID: 'class-1', TestUniqueID: 'test-1', TestDisplayName: 'Specs.should_finish' };
const testFinished = { $type: 'test-finished', TestClassUniqueID: 'class-1', TestUniqueID: 'test-1' };

async function watchdogFixture(env = {}) {
    const options = await fixture();
    const lines = [];
    const specProgress = { directory: options.directory, env, report: line => lines.push(line) };
    return { ...options, lines, specProgress };
}
async function fakeRunner(options, messages, ending = 'process.exit(0);') {
    const script = path.join(options.root, 'fake-runner.mjs');
    await writeFile(script, `for (const line of ${JSON.stringify(messages)}) console.log(line);\n${ending}\n`);
    return runProcess(process.execPath, [script], { timeout: options.timeout, specProgress: options.specProgress });
}

test('watchdog matches concurrent classes/tests by ID, warns once, and ignores completed classes', async () => {
    const options = await watchdogFixture();
    let now = 0;
    const watcher = new SpecHangWatchdog({ ...options.specProgress, now: () => now });
    const consume = message => watcher.consume(JSON.stringify(message));
    consume(classStarting());
    consume(testStarting);
    consume(classStarting('class-2', 'Specs.other'));
    now = 119999;
    watcher.check(() => assert.fail('too early'));
    assert.equal(options.lines.some(line => line.startsWith('::warning::')), false);
    now = 120000;
    watcher.check(() => assert.fail('warning must not kill'));
    watcher.check(() => assert.fail('warning must not kill'));
    assert.equal(options.lines.filter(line => line.startsWith('::warning::')).length, 2);
    assert.match(options.lines.join('\n'), /::warning::Long-running spec class Specs.when_shared_fixture_hangs/);
    consume(testFinished);
    consume(classFinished());
    consume(classFinished('class-2'));
    now = 600000;
    watcher.check(() => assert.fail('finished class must not kill'));
    assert.equal(watcher.classes.size, 0);
    assert.equal(watcher.tests.size, 0);
    const progress = await readFile(path.join(options.directory, 'class-progress.log'), 'utf8');
    assert.match(progress, /class finish Specs.when_shared_fixture_hangs elapsed=120.0s/);
});

test('JSON reporter keeps diagnostics, failures, captured output and unknown messages visible', async () => {
    const options = await watchdogFixture();
    const watcher = new SpecHangWatchdog(options.specProgress);
    watcher.consume(JSON.stringify(testStarting));
    const messages = [
        { $type: 'diagnostic-message', Message: 'fixture diagnostic\nsecond line' },
        { $type: 'internal-diagnostic-message', Message: 'runner diagnostic' },
        { $type: 'error', Messages: ['runner error'], StackTraces: ['runner stack'] },
        { $type: 'test-failed', TestUniqueID: 'test-1', Messages: ['assertion failed'], StackTraces: ['test stack'], Output: 'failure output' },
        { $type: 'test-output', TestUniqueID: 'test-1', Output: 'captured\noutput' },
        { $type: 'future-message', Value: 'preserve unknown payload' }
    ];
    for (const message of messages) watcher.consume(JSON.stringify(message));
    watcher.consume('collector stderr-style output');
    assert.deepEqual(options.lines, [
        '[diagnostic-message]\nfixture diagnostic\nsecond line',
        '[internal-diagnostic-message]\nrunner diagnostic',
        '[error]\nrunner error\nrunner stack',
        '[test-failed] Specs.should_finish\nassertion failed\ntest stack\nfailure output',
        '[test-output] Specs.should_finish\ncaptured\noutput',
        JSON.stringify(messages.at(-1)),
        'collector stderr-style output'
    ]);
});

test('watchdog rejects invalid timeout configuration before launching', async () => {
    for (const value of ['0', '-1', 'no', 'Infinity', '']) {
        const options = await watchdogFixture({ SPEC_CLASS_HANG_TIMEOUT_SECONDS: value });
        await assert.rejects(fakeRunner(options, []), /SPEC_CLASS_HANG_TIMEOUT_SECONDS must be a positive number/);
    }
});

test('LOCAL fake JSON runner completes without a kill, passes non-JSON through, and preserves exit codes', async () => {
    for (const exitCode of [0, 37]) {
        const options = await watchdogFixture({ SPEC_CLASS_HANG_TIMEOUT_SECONDS: '0.2' });
        const messages = ['collector output', '{not json}', ...[classStarting(), testStarting, testFinished, classFinished()].map(JSON.stringify)];
        const result = await fakeRunner(options, messages, `process.exitCode = ${exitCode};`);
        assert.equal(result.code, exitCode);
        assert.deepEqual(options.lines.slice(0, 2), ['collector output', '{not json}']);
        assert.match(options.lines.join('\n'), /class finish Specs.when_shared_fixture_hangs/);
        assert.equal(options.lines.some(line => line.startsWith('::error::')), false);
        await assert.rejects(readFile(path.join(options.directory, 'spec-hang-summary.txt')), { code: 'ENOENT' });
    }
});

test('LOCAL fake JSON runner hang names every open class/test, saves partial progress, and kills the tree with 125', async () => {
    const options = await watchdogFixture({ SPEC_CLASS_HANG_TIMEOUT_SECONDS: '0.2', SPEC_CLASS_HANG_WARN_SECONDS: '0.05' });
    options.timeout = 3000;
    const pidFile = path.join(options.root, 'hang-descendant.pid');
    const ending = `const { spawn } = await import('node:child_process');
        const { writeFileSync } = await import('node:fs');
        const child = spawn(process.execPath, ['-e', 'setInterval(() => {}, 1000)'], { stdio: 'inherit' });
        writeFileSync(${JSON.stringify(pidFile)}, String(child.pid));
        setInterval(() => {}, 1000);`;
    const started = performance.now();
    const messages = [classStarting(), classStarting('class-2', 'Specs.also_open'), testStarting].map(JSON.stringify);
    await assert.rejects(fakeRunner(options, messages, ending), error => error.exitCode === 125);
    assert.ok(performance.now() - started < 2000, 'watchdog must beat the outer deadline');
    const summary = await readFile(path.join(options.directory, 'spec-hang-summary.txt'), 'utf8');
    for (const name of ['Specs.when_shared_fixture_hangs', 'Specs.also_open', 'Specs.should_finish']) {
        assert.ok(summary.includes(name));
        assert.ok(options.lines.some(line => line.startsWith('::error::') && line.includes(name)));
    }
    assert.match(summary, /elapsed=0.[2-9]s/);
    assert.ok(options.lines.some(line => line.startsWith('::warning::')));
    assert.match(await readFile(path.join(options.directory, 'class-progress.log'), 'utf8'), /class start Specs.when_shared_fixture_hangs/);
    const pid = Number(await readFile(pidFile, 'utf8'));
    let live = true;
    for (let attempt = 0; attempt < 30 && live; attempt++) {
        await new Promise(resolve => setTimeout(resolve, 50));
        try { process.kill(pid, 0); } catch (error) { assert.equal(error.code, 'ESRCH'); live = false; }
    }
    assert.equal(live, false, `hang descendant ${pid} still live`);
});

test('watchdog identifies a shared fixture hang with no running test', async () => {
    const options = await watchdogFixture();
    let now = 0;
    const watcher = new SpecHangWatchdog({ ...options.specProgress, now: () => now });
    watcher.consume(JSON.stringify(classStarting()));
    now = 300000;
    let failure;
    watcher.check(error => { failure = error; });
    assert.equal(failure.exitCode, 125);
    assert.match(await readFile(path.join(options.directory, 'spec-hang-summary.txt'), 'utf8'), /Specs.when_shared_fixture_hangs elapsed=300.0s/);
});

test('LOCAL outer session timeout keeps exit 124 and persists open classes and tests', async () => {
    const options = await watchdogFixture();
    options.timeout = 500;
    await assert.rejects(fakeRunner(options, [classStarting(), testStarting].map(JSON.stringify), 'setInterval(() => {}, 1000);'), error => error.exitCode === 124);
    const summary = await readFile(path.join(options.directory, 'spec-hang-summary.txt'), 'utf8');
    assert.match(summary, /TestSessionTimeout exceeded/);
    assert.match(summary, /Specs.when_shared_fixture_hangs/);
    assert.match(summary, /Specs.should_finish/);
    assert.match(await readFile(path.join(options.directory, 'class-progress.log'), 'utf8'), /class start/);
});

const probeScript = path.resolve('.github/scripts/verify-mixed-specs-collector.mjs');
test('MOCK collector acceptance: exact 37 over the documented single-child grammar, wrong/zero exit rejected', async () => {
    const args = collectorArgs('/out', ['exit', '37']);
    assert.deepEqual(args.slice(0, 9), ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', '/out/probe.cobertura.xml']);
    assert.equal(args.length, 10);
    assert.equal(args.at(-1), childCommand(process.execPath, [probeScript, '--probe-child', 'exit', '37']));
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
