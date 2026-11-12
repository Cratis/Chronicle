// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { chmod, mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { ORDINARY_SHARDS, ordinaryShardIds, sourcePlan } from './spec-shards.mjs';

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
    assert.match(packages, /if: needs\.already-ran\.outputs\.skip != 'true'/);
    assert.deepEqual(dependencies('package-verification'), ['already-ran', 'dotnet-build']);
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
    assert.match(step(packages, 'Restore obj cache'), /uses: actions\/cache\/restore@v4/);
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
    assert.match(restore, /uses: actions\/cache\/restore@v4/);
    assert.ok(restore.includes(`key: dotnet-obj-cache-\u0024{{ hashFiles('**/*.csproj', '**/Directory.*.props', '**/Directory.*.targets') }}-\u0024{{ github.sha }}`));
    const save = step(job, 'Save obj cache');
    assert.match(save, /if: github\.event_name != 'pull_request' && steps\.restore-obj\.outputs\.cache-hit != 'true'/);
    assert.match(save, /uses: actions\/cache\/save@v4/);
    assert.match(save, /path: \.\/\*\*\/obj/);
    assert.match(save, /key: \u0024\{\{ steps\.restore-obj\.outputs\.cache-primary-key \}\}/);
    for (const name of ['dotnet-build-development', 'dotnet-build-development-requested']) {
        assert.match(step(jobs.get(name), 'Restore obj cache'), /uses: actions\/cache@v4/);
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
    assert.match(clients, /find \.\/Source\/Clients -name "\*\.csproj" ! -name "\*\.Specs\.csproj"/);
    assert.match(clients, /dotnet sln "\$solution_dir\/ClientPackages\.slnx" add --include-references=false "\$\{projects\[@\]\}" \.\/Source\/Kernel\/Contracts\/Contracts\.csproj/);
    assert.equal(clients.match(/dotnet build /g)?.length, 1);
    assert.match(clients, /dotnet build "\$solution_dir\/ClientPackages\.slnx" --configuration Release -p:ShouldUnsetParentConfigurationAndPlatform=false -p:IsPackaging=true -p:SkipAnalyzersForTargetFramework=net11\.0 -p:DisableProxyGenerator=true -p:DisableDockerBuild=true/);
    const guards = step(packages, moved[1]);
    assert.match(guards, /node \.github\/scripts\/verify-rolling-delivery-build\.mjs/);
    assert.match(guards, /for framework in net10\.0 net11\.0/);
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
            SQL_RESULT: 'success', NET10_RESULT: 'success', PACKAGE_RESULT: result, ANALYZER_RESULT: 'success', PACK_RESULT: 'success', EVENT_NAME: 'pull_request'
        } });
        assert.equal(child.status, result === 'success' ? 0 : 1, `Package result: ${result}`);
    }
    assert.ok(dependencies('specs').includes('analyzers'));
    assert.deepEqual(dependencies('analyzers'), ['already-ran']);
    assert.match(aggregate, /ANALYZER_RESULT: \$\{\{ needs\.analyzers\.result \}\}/);
    for (const result of ['success', 'failure', 'cancelled', 'skipped', '']) {
        const child = spawnSync('bash', ['-c', command], { env: {
            WIRE_RESULT: 'success', GRAIN_RESULT: 'success', OTHER_RESULT: 'success',
            SQL_RESULT: 'success', NET10_RESULT: 'success', PACKAGE_RESULT: 'success', ANALYZER_RESULT: result, PACK_RESULT: 'success', EVENT_NAME: 'pull_request'
        } });
        assert.equal(child.status, result === 'success' ? 0 : 1, `Analyzer result: ${result}`);
    }
    for (const job of ['integration', 'integration-api', 'mongodb', 'neo4j', 'coverage-merge-and-cache']) {
        assert.ok(dependencies(job).includes('specs'), `${job} no longer waits for the aggregate`);
    }
});

test('publish-equivalent pack runs independently with the same setup and restore-only cache', () => {
    assert.deepEqual(dependencies('package-pack-check'), ['already-ran']);
    assert.match(pack, /if: needs\.already-ran\.outputs\.skip != 'true' && github\.event_name == 'pull_request'/);
    assert.ok(dependencies('specs').includes('package-pack-check'));
    for (const name of ['Checkout code', 'Setup .Net', 'Cache NuGet packages', 'Restore obj cache', 'Add GitHub Package Registry to NuGet']) {
        assert.equal(step(pack, name).replace(/^ +#.*\n/gm, '').trim(), step(packages, name).replace(/^ +#.*\n/gm, '').trim());
    }
    assert.doesNotMatch(pack, /actions\/cache\/save|actions\/download-artifact|derive-build-version|CRATIS_CHRONICLE_BUILD_VERSION/);
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
                NET10_RESULT: 'success', PACKAGE_RESULT: 'success', ANALYZER_RESULT: 'success', PACK_RESULT: result, EVENT_NAME: event
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

test('three-shard workflow matches the planner and isolates the historical long pole', async () => {
    const other = jobs.get('specs-other');
    const matrix = JSON.parse(other.match(/shard: (\[[^\]]+\])/)[1]);
    assert.deepEqual(matrix, ordinaryShardIds());
    assert.equal(step(other, 'Run tests with coverage').match(/^        run: (.+)$/m)[1],
        `node .github/scripts/run-mixed-specs.mjs --shard \u0024{{ matrix.shard }} --shards ${ORDINARY_SHARDS}`);
    const plan = await sourcePlan(process.cwd());
    assert.equal(plan.length, ORDINARY_SHARDS);
    assert.ok(plan.every(shard => shard.projects.length && shard.defaulted.length === 0));
    assert.equal(new Set(plan.flatMap(shard => shard.projects)).size, 25);
    const mongo = plan.find(shard => shard.projects.some(project => project.endsWith('/Storage.MongoDB.Specs.csproj')));
    const core = plan.find(shard => shard.projects.some(project => project.endsWith('/Core.Specs.csproj')));
    assert.notEqual(mongo.shard, core.shard);
    assert.equal(mongo.projects.length, 1);
    // Samples predate #5839: the MongoDB weight alone exceeds the five-minute goal.
    // Keep measured weights honest rather than claiming the new layout has met that goal.
    assert.ok(plan.every(shard => shard.seconds <= 510));
    assert.ok(core.seconds < 300);
    const timings = JSON.parse(await readFile(new URL('./spec-shard-timings.json', import.meta.url), 'utf8'));
    const runs = new Set(timings.samples.map(sample => sample.run));
    assert.ok(runs.size >= 5 && runs.size <= 8);
    for (const run of runs) assert.deepEqual(timings.samples.filter(sample => sample.run === run).map(sample => sample.shard).sort(), [1, 2]);
});
