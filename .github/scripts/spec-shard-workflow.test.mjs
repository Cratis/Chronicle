// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { chmod, mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { ORDINARY_SHARDS, ordinaryShardIds, sourcePlan } from './spec-shards.mjs';
import { CORE_PARTS } from './core-spec-partition.mjs';

const workflow = await readFile(new URL('../workflows/dotnet-build.yml', import.meta.url), 'utf8');
const jobs = new Map([...workflow.matchAll(/^  ([\w-]+):\n([\s\S]*?)(?=^  [\w-]+:|$(?![\s\S]))/gm)].map(match => [match[1], match[2]]));
const build = jobs.get('dotnet-build');
const packages = jobs.get('package-verification');
const pack = jobs.get('package-pack-check');
const moved = [
    'Build client packages for every shipped framework',
    'Validate rolling diagnostic guards and shipped-client absence',
    'Verify packed client packages restore for consumers',
    'Verify local Docker image without push',
    'Verify NuGet packages can be created'
];

function step(job, name) {
    const found = [...job.matchAll(/^      - name: (.+)\n([\s\S]*?)(?=^      - name:|$(?![\s\S]))/gm)].find(match => match[1] === name);
    assert.ok(found, `Missing step: ${name}`);
    return found[2];
}

function dependencies(job) {
    const found = jobs.get(job)?.match(/^    needs: \[([^\]]+)\]/m);
    assert.ok(found, `Missing dependencies: ${job}`);
    return found[1].split(',').map(value => value.trim());
}

test('only independent verifications leave the build; every moved check remains blocking', () => {
    assert.ok(build && packages);
    for (const name of moved) {
        assert.ok(!build.includes(`- name: ${name}\n`), `${name} is still serial`);
        step(name === moved[4] ? pack : packages, name);
        assert.equal(workflow.split(`- name: ${name}\n`).length - 1, 1);
    }
    assert.doesNotMatch(packages + pack, /continue-on-error:/);
    assert.match(packages, /needs\.already-ran\.outputs\.skip != 'true'/);
    assert.deepEqual(dependencies('package-verification'), ['already-ran', 'dotnet-build', 'package-pack-check']);
    for (const shard of ['specs-other', 'specs-sql']) {
        assert.deepEqual(dependencies(shard), ['already-ran', 'dotnet-build']);
    }
});

test('Release artifact is uploaded immediately after the build integrity checks', () => {
    const names = [...build.matchAll(/^      - name: (.+)$/gm)].map(match => match[1]);
    const index = names.indexOf('Assert the build carries the stamped version');
    assert.ok(index > 0);
    assert.deepEqual(names.slice(index + 1, index + 3), ['Pack Release build output', 'Upload Release build output']);
    assert.match(step(build, 'Pack Release build output'), /find \. -type d -name bin/);
    assert.match(step(build, 'Upload Release build output'), /if-no-files-found: error/);
    assert.match(step(packages, 'Download Release build output'), /name: build-output-release/);
    assert.match(step(packages, 'Extract Release build output'), /tar -I 'zstd -d --long=30' -xf build-output-release\.tar\.zst/);
    assert.match(step(packages, 'Restore obj cache'), /uses: actions\/cache\/restore@v6/);
    assert.ok(!packages.includes('derive-build-version'), 'package verification must reuse the build version, not derive it again');
});

test('every Release artifact reader accepts the measured long-window compression', () => {
    assert.match(step(build, 'Pack Release build output'), /tar --null -I 'zstd -T0 --long=30' -cf build-output-release\.tar\.zst/);
    const readers = [...jobs.values()].filter(job => job.includes('- name: Download Release build output\n'));
    assert.equal(readers.length, 7);
    for (const reader of readers) {
        assert.match(step(reader, 'Extract Release build output'), /tar -I 'zstd -d --long=30' -xf build-output-release\.tar\.zst/);
    }
});

// Only the critical-path dotnet-build job skips the obj cache save on PRs. The two development build jobs keep
// identical steps (test-sparse-integration-workflows.py), so they keep the combined restore-and-save action.
test('dotnet-build restores the obj cache on PRs and saves only non-PR misses under the restored primary key', () => {
    const job = jobs.get('dotnet-build');
    const restore = step(job, 'Restore obj cache');
    assert.match(restore, /id: restore-obj/);
    assert.match(restore, /uses: actions\/cache\/restore@v6/);
    assert.ok(restore.includes(`key: dotnet-obj-cache-\u0024{{ hashFiles('**/*.csproj', '**/Directory.*.props', '**/Directory.*.targets') }}-\u0024{{ github.sha }}`));
    const save = step(job, 'Save obj cache');
    assert.match(save, /if: github\.event_name != 'pull_request' && steps\.restore-obj\.outputs\.cache-hit != 'true'/);
    assert.match(save, /uses: actions\/cache\/save@v6/);
    assert.match(save, /path: \.\/\*\*\/obj/);
    assert.match(save, /key: \u0024\{\{ steps\.restore-obj\.outputs\.cache-primary-key \}\}/);
    for (const name of ['dotnet-build-development', 'dotnet-build-development-requested']) {
        assert.match(step(jobs.get(name), 'Restore obj cache'), /uses: actions\/cache@v6/);
        assert.ok(!jobs.get(name).includes('actions/cache/save@'));
    }
});

test('package verification stamps exactly the version dotnet-build derived', () => {
    assert.match(build, /^      build-version: \$\{\{ steps\.export-build-version\.outputs\.version \}\}$/m);
    const names = [...build.matchAll(/^      - name: (.+)$/gm)].map(match => match[1]);
    assert.equal(names.indexOf('Export the build version'), names.indexOf('Stamp the build version') + 1);
    assert.match(step(build, 'Export the build version'), /id: export-build-version\n\s+run: echo "version=\$CRATIS_CHRONICLE_BUILD_VERSION" >> "\$GITHUB_OUTPUT"/);
    const stamp = step(packages, 'Stamp the version of the downloaded build');
    assert.match(stamp, /BUILD_VERSION: \$\{\{ needs\.dotnet-build\.outputs\.build-version \}\}/);
    const script = stamp.match(/run: \|\n([\s\S]+)/)[1].replace(/^ {10}/gm, '');
    for (const [version, accepted] of [['19.51.0-ci-42', true], ['', false], ['19.51.0', false], ['19.51.0-ci-42\nEVIL=1', false]]) {
        const child = spawnSync('bash', ['-c', `GITHUB_ENV="$OUT"; ${script}`], {
            env: { ...process.env, BUILD_VERSION: version, OUT: '/dev/null' }, encoding: 'utf8'
        });
        assert.equal(child.status === 0, accepted, `Version: ${JSON.stringify(version)}`);
    }
});

test('all shipped frameworks, diagnostic guards, consumer restore, Docker and stable pack stay covered', () => {
    const clients = step(packages, moved[0]);
    assert.match(clients, /if: github\.event_name != 'pull_request'/);
    assert.match(clients, /find \.\/Source\/Clients -name "\*\.csproj" ! -name "\*\.Specs\.csproj"/);
    assert.match(clients, /dotnet sln "\$solution_dir\/ClientPackages\.slnx" add --include-references=false "\$\{projects\[@\]\}" \.\/Source\/Kernel\/Contracts\/Contracts\.csproj/);
    assert.equal(clients.match(/dotnet build /g)?.length, 1);
    assert.match(clients, /dotnet build "\$solution_dir\/ClientPackages\.slnx" --configuration Release -p:ShouldUnsetParentConfigurationAndPlatform=false -p:IsPackaging=true -p:SkipAnalyzersForTargetFramework=net11\.0 -p:DisableProxyGenerator=true -p:DisableDockerBuild=true/);
    const guards = step(packages, moved[1]);
    assert.match(guards, /ROLLING_GUARD_PACK_ALL_FRAMEWORKS: "true"/);
    assert.match(guards, /node \.github\/scripts\/verify-rolling-delivery-build\.mjs/);
    assert.match(guards, /for framework in net10\.0 net11\.0/);
    assert.match(step(packages, moved[2]), /dotnet pack \.\/Source\/Clients\/Packages\.slnf --no-build --no-restore --configuration Release/);
    assert.match(step(packages, moved[2]), /verify-packed-consumer-restore\.sh "\$PACKED_VERSION" "\$feed"/);
    const docker = step(packages, moved[3]);
    assert.match(docker, /if: github\.event_name == 'pull_request' && !inputs\.integration_requested/);
    assert.match(docker, /cp -r Source\/Kernel\/Server\/bin\/Release\/net11\.0\/\./);
    assert.match(docker, /docker build -t "chronicle-dockerfile-check:/);
    assert.match(step(pack, moved[4]), /if: github\.event_name == 'pull_request'/);
    assert.equal(step(pack, moved[4]).match(/^        run: (.+)$/m)[1], 'dotnet pack ./Source/Clients/Packages.slnf --configuration Release -o "$RUNNER_TEMP/nuget-pack-check" -p:IsPackaging=true -p:Version=99.0.0 -p:PackageVersion=99.0.0 -p:SourceRevisionId=${{ github.sha }}');
    assert.ok(packages.indexOf(moved[1]) < packages.indexOf(moved[2]));
    for (const name of ['Build development Docker image', 'Push development Docker image', 'Export image']) step(build, name);
});

test('failed, cancelled and skipped package verification or analyzers cannot turn the aggregate green', () => {
    assert.ok(dependencies('specs').includes('package-verification'));
    const aggregate = step(jobs.get('specs'), 'Require every spec job to pass');
    assert.match(aggregate, /PACKAGE_RESULT: \$\{\{ needs\.package-verification\.result \}\}/);
    const command = aggregate.match(/^        run: (.+)$/m)?.[1];
    assert.ok(command);
    for (const result of ['success', 'failure', 'cancelled', 'skipped', '']) {
        const child = spawnSync('bash', ['-c', command], { env: {
            WIRE_RESULT: 'success', GRAIN_RESULT: 'success', OTHER_RESULT: 'success',
            SQL_RESULT: 'success', NET10_RESULT: 'success', PACKAGE_RESULT: result, ANALYZER_RESULT: 'success', DEV_ANALYZER_RESULT: 'success', RUNNER_RESULT: 'success', PACK_RESULT: 'success', EVENT_NAME: 'pull_request'
        } });
        assert.equal(child.status, result === 'success' ? 0 : 1, `Package result: ${result}`);
    }
    assert.ok(dependencies('specs').includes('analyzers'));
    assert.deepEqual(dependencies('analyzers'), ['already-ran']);
    assert.match(aggregate, /ANALYZER_RESULT: \$\{\{ needs\.analyzers\.result \}\}/);
    for (const result of ['success', 'failure', 'cancelled', 'skipped', '']) {
        const child = spawnSync('bash', ['-c', command], { env: {
            WIRE_RESULT: 'success', GRAIN_RESULT: 'success', OTHER_RESULT: 'success',
            SQL_RESULT: 'success', NET10_RESULT: 'success', PACKAGE_RESULT: 'success', ANALYZER_RESULT: result, DEV_ANALYZER_RESULT: 'success', RUNNER_RESULT: 'success', PACK_RESULT: 'success', EVENT_NAME: 'pull_request'
        } });
        assert.equal(child.status, result === 'success' ? 0 : 1, `Analyzer result: ${result}`);
    }
    // #5865: the requested lanes start after their builds, beside the ordinary specs. Only the coverage merge (and
    // the run's verdict through the specs aggregate) still requires every spec and every lane, so nothing caches or
    // goes green with a failed, cancelled or skipped one.
    for (const job of ['integration', 'integration-api', 'mongodb', 'neo4j']) {
        assert.ok(!dependencies(job).includes('specs'), `${job} starts without waiting for the aggregate`);
        assert.ok(dependencies('coverage-merge-and-cache').includes(job), `coverage merge waits for ${job}`);
    }
    assert.ok(dependencies('coverage-merge-and-cache').includes('specs'), 'coverage merge still waits for the aggregate');
    // #5875: the per-group partial merges wait for and require every lane, and the final merge waits for all of them.
    for (const job of ['specs', 'integration', 'integration-api', 'mongodb', 'neo4j']) {
        assert.ok(dependencies('coverage-partial').includes(job), `partial coverage merge waits for ${job}`);
    }
    assert.ok(dependencies('coverage-merge-and-cache').includes('coverage-partial'), 'final coverage merge waits for the partials');
});

test('publish-equivalent pack runs independently with the same setup and restore-only cache', () => {
    assert.deepEqual(dependencies('package-pack-check'), ['already-ran']);
    assert.match(pack, /if: needs\.already-ran\.outputs\.skip != 'true' && github\.event_name == 'pull_request'/);
    assert.ok(dependencies('specs').includes('package-pack-check'));
    for (const name of ['Checkout code', 'Setup .Net', 'Cache NuGet packages', 'Restore obj cache', 'Add GitHub Package Registry to NuGet']) {
        const setup = job => step(job, name).replace(/^ +#.*\n/gm, '').replace(/^        if: github\.event_name != 'pull_request'\n/m, '').trim();
        assert.equal(setup(pack), setup(packages));
    }
    assert.doesNotMatch(pack, /actions\/cache\/save|actions\/download-artifact|derive-build-version|CRATIS_CHRONICLE_BUILD_VERSION/);
});

test('package artifact reuse is PR-only and fails closed without both successful producers', () => {
    const condition = packages.match(/^    if: >-\n((?:      .+\n)+)/m)[1].trim().replace(/\s+/g, ' ');
    assert.equal(condition, "!cancelled() && needs.already-ran.outputs.skip != 'true' && needs.dotnet-build.result == 'success' && (needs.package-pack-check.result == 'success' || (github.event_name != 'pull_request' && needs.package-pack-check.result == 'skipped'))");
    for (const name of ['Download client package build output', 'Extract client package build output', 'Restore client package dependencies']) {
        assert.match(step(packages, name), /if: github\.event_name == 'pull_request'/);
    }
    assert.match(step(packages, 'Download client package build output'), /name: client-package-build-output/);
    const restore = step(packages, 'Restore client package dependencies');
    assert.match(restore, /dotnet restore \.\/Source\/Clients\/Packages\.slnf -p:IsPackaging=true -p:Version=99\.0\.0 -p:PackageVersion=99\.0\.0/);
    assert.doesNotMatch(restore, /dotnet (?:build|pack|publish)/);
    assert.ok(packages.indexOf('Restore client package dependencies') < packages.indexOf(moved[1]));
    assert.match(step(packages, 'Restore obj cache'), /if: github\.event_name != 'pull_request'/);
    assert.ok(packages.indexOf('Extract Release build output') < packages.indexOf('Extract client package build output'));
    assert.ok(packages.indexOf('Extract client package build output') < packages.indexOf(moved[1]));
    const upload = step(pack, 'Upload client package build output');
    for (const field of ['name: client-package-build-output', 'path: client-package-build-output.tar.zst', 'retention-days: 1', 'compression-level: 0', 'if-no-files-found: error']) {
        assert.ok(upload.includes(field), `Missing artifact setting: ${field}`);
    }
    assert.ok(pack.indexOf(moved[4]) < pack.indexOf('Archive client package build output'));
});

test('client artifact roundtrip preserves both frameworks and obj without overwriting the server', async () => {
    const root = await mkdtemp(join(tmpdir(), 'chronicle-client-artifact-'));
    try {
        const projects = ['Source/Clients/DotNET', 'Source/Kernel/Contracts'];
        for (const project of projects) {
            const assembly = project.endsWith('DotNET') ? 'Cratis.Chronicle' : 'Cratis.Chronicle.Contracts';
            for (const framework of ['net10.0', 'net11.0']) {
                await mkdir(join(root, project, 'bin/Release', framework), { recursive: true });
                await writeFile(join(root, project, 'bin/Release', framework, `${assembly}.dll`), framework);
            }
            await mkdir(join(root, project, 'obj'), { recursive: true });
            await writeFile(join(root, project, 'obj/project.assets.json'), 'pack inputs');
        }
        const run = name => step(pack, name).match(/run: \|\n([\s\S]+)/)[1];
        const archived = spawnSync('bash', ['-c', run('Archive client package build output')], { cwd: root, encoding: 'utf8' });
        assert.equal(archived.status, 0, archived.stderr);
        await writeFile(join(root, 'Source/Clients/DotNET/bin/Release/net11.0/Cratis.Chronicle.dll'), 'single-framework build');
        await mkdir(join(root, 'Source/Kernel/Server/bin'), { recursive: true });
        await writeFile(join(root, 'Source/Kernel/Server/bin/server.dll'), 'server build');
        const extract = step(packages, 'Extract client package build output').match(/run: \|\n([\s\S]+)/)[1];
        const extracted = spawnSync('bash', ['-c', extract], { cwd: root, encoding: 'utf8' });
        assert.equal(extracted.status, 0, extracted.stderr);
        assert.equal(await readFile(join(root, 'Source/Clients/DotNET/bin/Release/net11.0/Cratis.Chronicle.dll'), 'utf8'), 'net11.0');
        assert.equal(await readFile(join(root, 'Source/Clients/DotNET/obj/project.assets.json'), 'utf8'), 'pack inputs');
        assert.equal(await readFile(join(root, 'Source/Kernel/Server/bin/server.dll'), 'utf8'), 'server build');
        await rm(join(root, 'Source/Clients/DotNET/bin/Release/net10.0'), { recursive: true });
        const incomplete = spawnSync('bash', ['-c', run('Archive client package build output')], { cwd: root, encoding: 'utf8' });
        assert.notEqual(incomplete.status, 0, 'missing framework output must fail the producer');
    } finally {
        await rm(root, { recursive: true, force: true });
    }
});

test('pack result fails closed, permitting skipped only outside pull requests', () => {
    const aggregate = step(jobs.get('specs'), 'Require every spec job to pass');
    assert.match(aggregate, /PACK_RESULT: \$\{\{ needs\.package-pack-check\.result \}\}/);
    assert.match(aggregate, /EVENT_NAME: \$\{\{ github\.event_name \}\}/);
    const command = aggregate.match(/^        run: (.+)$/m)[1];
    for (const event of ['pull_request', 'push', 'schedule', 'workflow_call', 'workflow_dispatch']) {
        for (const result of ['success', 'failure', 'cancelled', 'skipped', '']) {
            const child = spawnSync('bash', ['-c', command], { env: {
                WIRE_RESULT: 'success', GRAIN_RESULT: 'success', OTHER_RESULT: 'success', SQL_RESULT: 'success',
                NET10_RESULT: 'success', PACKAGE_RESULT: 'success', ANALYZER_RESULT: 'success', DEV_ANALYZER_RESULT: 'success', RUNNER_RESULT: 'success', PACK_RESULT: result, EVENT_NAME: event
            } });
            const accepted = result === 'success' || (event !== 'pull_request' && result === 'skipped');
            assert.equal(child.status, accepted ? 0 : 1, `${event}: ${result}`);
        }
    }
});

test('single build receives the find-derived client set, including new projects but not specs', async () => {
    const root = await mkdtemp(join(tmpdir(), 'chronicle-client-build-'));
    try {
        const projects = ['Source/Clients/First/First.csproj', 'Source/Clients/New Client/New.csproj', 'Source/Kernel/Contracts/Contracts.csproj'];
        for (const project of [...projects, 'Source/Clients/First/First.Specs.csproj', 'Source/Other/Other.csproj']) {
            await mkdir(join(root, project, '..'), { recursive: true });
            await writeFile(join(root, project), '<Project />');
        }
        await writeFile(join(root, 'dotnet'), `#!${process.execPath}\nrequire('node:fs').appendFileSync(process.env.CALL_LOG, JSON.stringify(process.argv.slice(2)) + '\\n');\n`);
        await chmod(join(root, 'dotnet'), 0o755);
        const log = join(root, 'calls.jsonl');
        const child = spawnSync('bash', ['-c', step(packages, moved[0]).match(/run: \|\n([\s\S]+)/)[1]], {
            cwd: root, env: { ...process.env, RUNNER_TEMP: root, PATH: `${root}:${process.env.PATH}`, CALL_LOG: log }, encoding: 'utf8'
        });
        assert.equal(child.status, 0, child.stderr);
        const calls = (await readFile(log, 'utf8')).trim().split('\n').map(line => JSON.parse(line));
        assert.deepEqual(calls.map(call => call[0]), ['new', 'sln', 'build']);
        assert.equal(calls[1][3], '--include-references=false');
        assert.deepEqual(calls[1].slice(4).sort(), projects.map(project => `./${project}`).sort());
        assert.equal(calls[1][1], calls[2][1]);
        assert.deepEqual(calls[2].slice(2), ['--configuration', 'Release', '-p:ShouldUnsetParentConfigurationAndPlatform=false', '-p:IsPackaging=true', '-p:SkipAnalyzersForTargetFramework=net11.0', '-p:DisableProxyGenerator=true', '-p:DisableDockerBuild=true']);
    } finally {
        await rm(root, { recursive: true, force: true });
    }
});

for (const [job, runName, suffix] of [
    ['specs-other', 'Run tests with coverage', '-${{ matrix.shard }}'],
    ['specs-sql', 'Run SQL specs with coverage', '-${{ matrix.shard }}'],
    ['integration-api', 'Run API integration tests with coverage', ''],
    ['mongodb', 'Run MongoDB integration tests with coverage', ''],
    ['neo4j', 'Run Neo4j integration tests with coverage (${{ matrix.lane }})', '-${{ matrix.lane }}']
]) {
    test(`${job} keeps crash minidumps for failed jobs`, () => {
        // Cratis/Chronicle#5630: the test host intermittently dies with SIGSEGV and no dump was kept.
        const run = step(jobs.get(job), runName);
        const env = run.match(/^        env:\n([\s\S]*?)(?=^        \S)/m)?.[1];
        assert.ok(env, 'Crash dump settings must be on the test-run step');
        assert.match(env, /^          DOTNET_DbgEnableMiniDump: 1$/m);
        assert.match(env, /^          DOTNET_DbgMiniDumpType: 1$/m);
        assert.ok(env.includes('DOTNET_DbgMiniDumpName: ${{ runner.temp }}/crash-dumps/%e.%p.dmp\n'));
        const script = run.split('        run: |\n')[1];
        assert.ok(script, 'The test-run step must prepare the dump directory');
        const mkdir = script.indexOf('mkdir -p "$RUNNER_TEMP/crash-dumps"');
        assert.ok(mkdir >= 0 && mkdir < script.indexOf('node .github/scripts/run-'));
        const upload = step(jobs.get(job), 'Upload crash dumps');
        assert.match(upload, /^        if: failure\(\)$/m);
        assert.match(upload, /uses: actions\/upload-artifact@/);
        assert.ok(upload.includes(`          name: crash-dumps-${job}${suffix}\n`));
        assert.ok(upload.includes('          path: ${{ runner.temp }}/crash-dumps/\n'));
        assert.match(upload, /^          if-no-files-found: ignore$/m);
        assert.match(upload, /^          retention-days: 3$/m);
    });
}

test('three-shard workflow matches the planner and refreshed timing samples', async () => {
    const other = jobs.get('specs-other');
    const matrix = JSON.parse(other.match(/shard: (\[[^\]]+\])/)[1]);
    assert.deepEqual(matrix, ordinaryShardIds());
    const run = step(other, 'Run tests with coverage');
    assert.ok(run.trimEnd().endsWith(`node .github/scripts/run-mixed-specs.mjs --shard \u0024{{ matrix.shard }} --shards ${ORDINARY_SHARDS}`));
    assert.match(run, /set -euo pipefail/);
    assert.match(run, /\[specs resources\].*date -u/);
    assert.match(run, /free -m/);
    assert.match(run, /df -h \/ "\$GITHUB_WORKSPACE"/);
    assert.match(run, /ps -eo pid,rss,etime,args --sort=-rss \| sed -n '1,6p'/);
    assert.match(run, /sleep 30/);
    assert.match(run, /\) &\n\s+sampler_pid=\$!/);
    assert.match(run, /trap 'status=\$\?; kill "\$sampler_pid" 2>\/dev\/null \|\| true; wait "\$sampler_pid" \|\| true; exit "\$status"' EXIT/);
    const plan = await sourcePlan(process.cwd());
    assert.equal(plan.length, ORDINARY_SHARDS);
    assert.ok(plan.every(shard => shard.projects.length && shard.defaulted.length === 0));
    const projects = plan.flatMap(shard => shard.projects);
    assert.equal(projects.length, 26 + CORE_PARTS);
    assert.equal(new Set(projects).size, 26 + CORE_PARTS);
    assert.equal(projects.filter(project => project.includes('#core-part-')).length, CORE_PARTS);
    assert.ok(!projects.includes('Source/Kernel/Core.Specs/Core.Specs.csproj'));
    // Train 8 wall calibration includes 55 seconds per Core part and mean shard overhead.
    // This is a planning bound, not a claim of measured split-shard performance.
    assert.ok(plan.every(shard => shard.seconds < 295));
    assert.ok(plan.every(shard => shard.projects.filter(project => project.includes('#core-part-')).length <= 1));
    const timings = JSON.parse(await readFile(new URL('./spec-shard-timings.json', import.meta.url), 'utf8'));
    const runs = new Set(timings.samples.map(sample => sample.run));
    assert.ok(runs.size >= 1 && runs.size <= 8);
    for (const run of runs) {
        // Each sampled run must contribute every shard it had, numbered 1..n, whatever the shard count was then.
        const shards = timings.samples.filter(sample => sample.run === run).map(sample => sample.shard).sort((a, b) => a - b);
        assert.ok(shards.length >= 2 && shards.length <= ORDINARY_SHARDS);
        assert.deepEqual(shards, shards.map((_, index) => index + 1));
    }
});

test('the unused full build-output-development tarball is no longer packed or uploaded (#5866)', () => {
    for (const name of ['dotnet-build-development', 'dotnet-build-development-requested']) {
        const job = jobs.get(name);
        assert.ok(job, name);
        assert.doesNotMatch(job, /Pack DEVELOPMENT build output|Upload DEVELOPMENT build output|build-output-development\.tar\.zst/);
        assert.match(step(job, 'Pack per-project integration build output'), /prefix: build-output-development/);
    }
    assert.match(jobs.get('integration'), /build-output-artifact: build-output-development/);
});
