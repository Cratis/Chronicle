// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { chmodSync, existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const root = fileURLToPath(new URL('../../', import.meta.url));
const workflow = readFileSync(join(root, '.github/workflows/pull-requests.yml'), 'utf8');
const producers = ['workbench', 'kernel-development', 'build-nuget-packages', 'build-docker'];
const publishers = ['publish-nuget-packages', 'publish-docker'];
const sameRepository = 'github.event.pull_request.head.repo.full_name == github.repository';
const publishGuard = `${sameRepository} && needs.release.outputs.publish == 'true'`;

// Deliberately accept only the workflow's canonical text shape. actionlint validates YAML;
// unknown run representations must fail here, never disappear from the security scan.
function runs(text) {
    const lines = text.split('\n');
    const found = [];
    for (let index = 0; index < lines.length; index++) {
        if (/^\s*#/.test(lines[index])) continue;
        const dataBlock = lines[index].match(/^(\s*)([\w-]+): [|>][+-]?$/);
        if (dataBlock && dataBlock[2] !== 'run') {
            const indentation = dataBlock[1].length;
            while (index + 1 < lines.length && (lines[index + 1].trim() === '' || lines[index + 1].search(/\S/) > indentation)) index++;
            continue;
        }
        if (!/(?:^|[\s,{])(?:run|"run"|'run')\s*:/.test(lines[index])) continue;
        const match = lines[index].match(/^        run: (.+)$/);
        assert.ok(match, `Unrecognized run field: ${lines[index]}`);
        const value = match[1];
        if (value !== '|') {
            assert.doesNotMatch(value, /^[|>"'&*!{[]/, 'Unsupported run scalar');
            found.push(value);
            continue;
        }
        const body = [];
        while (index + 1 < lines.length && /^(?: {10}|\s*$)/.test(lines[index + 1])) {
            body.push(lines[++index].replace(/^ {10}/, ''));
        }
        assert.ok(body.some(line => line.trim()), 'Empty run block');
        found.push(body.join('\n').trimEnd());
    }
    return found;
}

function jobs(text) {
    return new Map([...text.matchAll(/^  ([\w-]+):\n([\s\S]*?)(?=^  [\w-]+:|$(?![\s\S]))/gm)]
        .filter(match => text.indexOf(match[0]) > text.indexOf('\njobs:\n'))
        .map(match => [match[1], match[2]]));
}

function steps(job) {
    return [...job.matchAll(/^      - (?:name: (.+)|uses: (.+))\n([\s\S]*?)(?=^      - |$(?![\s\S]))/gm)]
        .map(match => ({ name: match[1], text: match[0], action: match[2] }));
}

function step(job, name) {
    const found = steps(job).find(candidate => candidate.name === name);
    assert.ok(found, `Missing step: ${name}`);
    return found.text;
}

function scalar(text, key, indent = 4) {
    const found = text.match(new RegExp(`^ {${indent}}${key}: (.+)$`, 'm'));
    assert.ok(found, `Missing ${key}`);
    return found[1];
}

function map(text, key, indent) {
    const found = text.match(new RegExp(`^ {${indent}}${key}:\\n((?: {${indent + 2}}[^\\n]+\\n?)+)`, 'm'));
    assert.ok(found, `Missing ${key} map`);
    return Object.fromEntries(found[1].trimEnd().split('\n').map(line => {
        const entry = line.trim().match(/^([\w-]+): (.+)$/);
        assert.ok(entry, `Unsupported map entry: ${line}`);
        return entry.slice(1);
    }));
}

function dependencies(job) {
    return scalar(job, 'needs').replace(/^\[|\]$/g, '').split(',').map(value => value.trim());
}

function assertRunInputs(text) {
    const scripts = runs(text);
    assert.ok(scripts.length > 0);
    for (const script of scripts) assert.doesNotMatch(script, /\$\{\{/, 'Expression in executable shell');
}

function assertAuthority(text) {
    assert.match(text, /^permissions: \{\}$/m);
    const all = jobs(text);
    assert.deepEqual([...all.keys()], ['release', ...producers.slice(0, 3), 'publish-nuget-packages', 'build-docker', 'publish-docker']);
    assert.deepEqual(map(all.get('release'), 'permissions', 4), { contents: 'read', 'pull-requests': 'read' });
    for (const name of producers) {
        const job = all.get(name);
        assert.deepEqual(map(job, 'permissions', 4), { contents: 'read' });
        assert.equal(scalar(job, 'if'), "needs.release.outputs.publish == 'true'");
        assert.doesNotMatch(job, /secrets\.|github\.token|GITHUB_TOKEN|NODE_AUTH_TOKEN|NPM_TOKEN|id-token:|docker\/login-action|dotnet nuget (?:add|update|push)/i);
    }
    for (const name of publishers) {
        assert.deepEqual(map(all.get(name), 'permissions', 4), { packages: 'write', 'pull-requests': 'write' });
        assert.equal(scalar(all.get(name), 'if'), publishGuard);
    }
    assert.equal(scalar(all.get('release'), 'if'), sameRepository);
    for (const job of all.values()) {
        assert.equal(scalar(job, 'runs-on'), 'ubuntu-latest');
        assert.equal(scalar(job, 'timeout-minutes'), '30');
        assert.doesNotMatch(job, /continue-on-error:|id-token:|always\(\).*needs/);
        for (const checkout of steps(job).filter(value => value.text.includes('uses: actions/checkout@'))) {
            assert.deepEqual(map(checkout.text, 'with', 8), {
                ref: '${{ github.event.pull_request.head.sha }}', 'persist-credentials': 'false'
            });
        }
    }
    for (const action of text.matchAll(/^\s+(?:- )?uses: (\S+)/gm)) assert.match(action[1], /@[a-f0-9]{40}$/);
}

// Exact command lines form a small publisher allowlist, not a blacklist of build verbs.
const publisherCommands = new Set([
    'set -euo pipefail', 'shopt -s nullglob', 'packages=("$PACKAGE_DIRECTORY"/*.nupkg)',
    '[ "${#packages[@]}" -gt 0 ]', 'for package in "${packages[@]}"; do', 'test -s "$package"', 'done',
    'cat > "$NUGET_CONFIG" <<\'EOF\'', '<?xml version="1.0" encoding="utf-8"?>',
    '<configuration><packageSources><clear /></packageSources></configuration>', 'EOF',
    'dotnet nuget add source "$NUGET_SOURCE" --name github --username cratis --password "$NUGET_TOKEN" --store-password-in-clear-text --configfile "$NUGET_CONFIG"',
    'dotnet nuget push "$PACKAGE_DIRECTORY/*.nupkg" --skip-duplicate --timeout 900 --api-key "$NUGET_TOKEN" --source "$NUGET_SOURCE" --configfile "$NUGET_CONFIG"',
    'rm -f "$NUGET_CONFIG"', 'test -s "$IMAGE_ARCHIVE"', 'docker load --input "$IMAGE_ARCHIVE"',
    'test -n "$(docker image inspect --format \'{{.Id}}\' "$IMAGE_TAG")"', 'docker push "$IMAGE_TAG"'
]);

function assertPublisherIsolation(text) {
    const all = jobs(text);
    for (const name of publishers) {
        const job = all.get(name);
        assert.ok(job, `Missing publisher ${name}`);
        assert.doesNotMatch(job, /^    (?:env|defaults):|working-directory:/m);
        for (const action of job.matchAll(/^\s+(?:- )?uses: (\S+)/gm)) {
            assert.ok(['actions/setup-dotnet', 'actions/download-artifact', 'docker/login-action', 'mshick/add-pr-comment'].includes(action[1].split('@')[0]), `Publisher action: ${action[1]}`);
        }
        for (const script of runs(job)) {
            for (const line of script.split('\n')) assert.ok(publisherCommands.has(line.trim()), `Publisher command: ${line}`);
        }
        assert.ok(!job.includes('uses: ./'));
    }
}

const all = jobs(workflow);

test('every executable shell input is environment data, including inline and block runs', () => assertRunInputs(workflow));
test('authority stays with checkout-free publishers; all actions and checkouts are bounded', () => assertAuthority(workflow));
test('publisher actions and shell commands cannot execute repository or artifact scripts', () => assertPublisherIsolation(workflow));

test('contracts reject interpolation, alternate run encodings and privilege regressions', () => {
    for (const text of ['        run: echo ${{ github.head_ref }}', '        run: |\n          echo ${{ needs.release.outputs.version }}']) {
        assert.throws(() => assertRunInputs(text));
    }
    for (const scalar of ['>\n          echo hello', '*script', '"echo hello"', '|-\n          echo hello']) {
        assert.throws(() => runs(`        run: ${scalar}`));
    }
    assert.throws(() => runs('        "run": echo hello'));
    assert.throws(() => runs('    steps: [{run: echo hello}]'));
    assert.doesNotThrow(() => assertRunInputs('        run: echo "$VERSION"\n        with:\n          message: |\n            run: ${{ needs.release.outputs.version }}'));
    assert.throws(() => assertAuthority(workflow.replace('  workbench:\n', '  workbench:\n    permissions:\n      packages: write\n')));
    for (const mutation of ['uses: actions/checkout@', 'uses: ./repository-action #', 'run: npm install #', 'run: docker run #']) {
        const altered = workflow.replace('        run: docker push "$IMAGE_TAG"', `        ${mutation}`);
        assert.ok(altered !== workflow, 'Publisher mutation must apply');
        assert.throws(() => assertPublisherIsolation(altered));
    }
});

test('release semantics, build configurations and publication-safe concurrency remain intact', () => {
    assert.match(workflow, /pull_request_target:\n    types: \[opened, synchronize, reopened\]/);
    assert.match(workflow, /group: \$\{\{ github.workflow \}\}-\$\{\{ github.event.pull_request.number \|\| github.ref \}\}/);
    assert.match(workflow, /cancel-in-progress: false\n  queue: max/);
    assert.deepEqual(dependencies(all.get('build-nuget-packages')), ['release', 'workbench']);
    assert.deepEqual(dependencies(all.get('build-docker')), ['release', 'workbench', 'kernel-development']);
    assert.deepEqual(dependencies(all.get('publish-nuget-packages')), ['release', 'build-nuget-packages']);
    assert.deepEqual(dependencies(all.get('publish-docker')), ['release', 'build-docker']);
    assert.match(step(all.get('build-nuget-packages'), 'Build root'), /dotnet build --configuration Debug -p:DisableProxyGenerator=true/);
    const pack = step(all.get('build-nuget-packages'), 'Create NuGet packages');
    assert.equal(map(pack, 'env', 8).PACKAGE_VERSION, '${{ needs.release.outputs.version }}');
    assert.equal(runs(pack)[0], 'dotnet pack --no-build --configuration Debug -o "$NUGET_OUTPUT" -p:PackageVersion="$PACKAGE_VERSION"');
    assert.match(all.get('kernel-development'), /dotnet publish -c Debug -p:DefineConstants=DEVELOPMENT -p:DisableProxyGenerator=true -o out\/development\/x64\n          cp -r out\/development\/x64 out\/development\/arm64/);
    const build = step(all.get('build-docker'), 'Build Development Docker Image');
    assert.match(build, /file: \.\/Docker\/Development\/Dockerfile/);
    assert.match(build, /platforms: linux\/amd64/);
    assert.match(build, /push: false/);
    assert.match(build, /outputs: type=docker,dest=\$\{\{ runner.temp \}\}\/pr-docker-image.tar/);
    assert.match(build, /ghcr.io\/cratis\/chronicle:\$\{\{ needs.release.outputs.version \}\}/);
    assert.match(build, /VERSION=\$\{\{ needs.release.outputs.version \}\}\n            ENTRYPOINT=pr_entrypoint.sh/);
    assert.doesNotMatch(build, /cache-to:|load: true/);
});

function assertArtifact(builder, publisher, uploadName, downloadName, artifact, output, destination) {
    const upload = map(step(all.get(builder), uploadName), 'with', 8);
    assert.deepEqual(upload, { 'retention-days': '3', name: artifact, path: output, 'if-no-files-found': 'error' });
    const download = map(step(all.get(publisher), downloadName), 'with', 8);
    assert.deepEqual(download, { name: artifact, path: destination });
}

test('package and image handoffs are exact-name current-run artifacts, not caches', () => {
    assertArtifact('build-nuget-packages', 'publish-nuget-packages', 'Upload NuGet packages', 'Download NuGet packages',
        'pr-nuget-packages', './Artifacts/NuGet/*.nupkg', '${{ runner.temp }}/pr-nuget-packages');
    assertArtifact('build-docker', 'publish-docker', 'Upload Docker image', 'Download Docker image',
        'pr-docker-image', '${{ runner.temp }}/pr-docker-image.tar', '${{ runner.temp }}/pr-docker-image');
    assert.ok(steps(all.get('build-nuget-packages')).findIndex(value => value.name === 'Verify NuGet packages') < steps(all.get('build-nuget-packages')).findIndex(value => value.name === 'Upload NuGet packages'));
    assert.ok(steps(all.get('build-docker')).findIndex(value => value.name === 'Verify Docker archive') < steps(all.get('build-docker')).findIndex(value => value.name === 'Upload Docker image'));
    for (const name of publishers) {
        assert.doesNotMatch(all.get(name), /cache|run-id:|github-token:|pattern:|merge-multiple:/i);
        const names = steps(all.get(name)).map(value => value.name);
        const validation = name === 'publish-docker' ? 'Load and verify Docker image' : 'Verify NuGet packages';
        const authentication = name === 'publish-docker' ? 'Login to GitHub Docker Registry' : 'Add GitHub Package Registry to NuGet';
        assert.ok(names.indexOf(validation) >= 0 && names.indexOf(validation) < names.indexOf(authentication));
    }
});

test('credentials are step-scoped and the package publisher uses only a fresh temporary config', () => {
    assert.deepEqual(map(workflow, 'env', 0), {
        NUGET_OUTPUT: './Artifacts/NuGet', NUGET_SOURCE: 'https://nuget.pkg.github.com/cratis/index.json', DOTNET_VERSION: '"11.0.x"'
    });
    const nuget = all.get('publish-nuget-packages');
    const permittedSecrets = new Set(['Add GitHub Package Registry to NuGet', 'Push NuGet packages', 'Add comment for NuGet packages', 'Login to GitHub Docker Registry', 'Add comment for Docker image']);
    for (const name of publishers) {
        for (const value of steps(all.get(name))) {
            if (value.text.includes('secrets.')) assert.ok(permittedSecrets.has(value.name));
        }
    }
    assert.deepEqual(map(step(nuget, 'Setup .Net'), 'with', 8), { 'dotnet-version': '${{ env.DOTNET_VERSION }}' });
    const config = { NUGET_CONFIG: '${{ runner.temp }}/pr-nuget.config' };
    const credential = { ...config, NUGET_TOKEN: '${{ secrets.GITHUB_TOKEN }}' };
    const packages = { PACKAGE_DIRECTORY: '${{ runner.temp }}/pr-nuget-packages' };
    for (const [name, expected] of [
        ['Verify NuGet packages', packages], ['Add GitHub Package Registry to NuGet', credential],
        ['Push NuGet packages', { ...packages, ...credential }], ['Remove NuGet credentials', config]
    ]) assert.deepEqual(map(step(nuget, name), 'env', 8), expected);
    assert.deepEqual(map(step(all.get('build-nuget-packages'), 'Verify NuGet packages'), 'env', 8), { PACKAGE_DIRECTORY: '${{ env.NUGET_OUTPUT }}' });
    assert.equal(scalar(step(nuget, 'Remove NuGet credentials'), 'if', 8), 'always()');
    const docker = all.get('publish-docker');
    const image = { IMAGE_TAG: 'ghcr.io/cratis/chronicle:${{ needs.release.outputs.version }}' };
    assert.deepEqual(map(step(docker, 'Load and verify Docker image'), 'env', 8), {
        IMAGE_ARCHIVE: '${{ runner.temp }}/pr-docker-image/pr-docker-image.tar', ...image
    });
    assert.deepEqual(map(step(docker, 'Push Docker image'), 'env', 8), image);
    assert.deepEqual(map(step(docker, 'Login to GitHub Docker Registry'), 'with', 8), {
        registry: 'ghcr.io', username: '${{ github.actor }}', password: '${{ secrets.GITHUB_TOKEN }}'
    });
});

function fixture(callback) {
    const scratch = join(root, '.ai-work');
    mkdirSync(scratch, { recursive: true });
    const directory = mkdtempSync(join(scratch, 'pr-prerelease-'));
    try { callback(directory); } finally { rmSync(directory, { recursive: true, force: true }); }
}

function fakeCli(directory) {
    const bin = join(directory, 'bin');
    mkdirSync(bin);
    const executable = join(bin, 'fake');
    writeFileSync(executable, `#!${process.execPath}\nconst fs = require('node:fs');\nfs.appendFileSync(process.env.CALLS, JSON.stringify(process.argv.slice(2)) + '\\n');\nif (process.argv[2] === 'image') { if (process.env.IMAGE_MISSING) process.exit(1); process.stdout.write('sha256:fixture'); }\n`);
    chmodSync(executable, 0o755);
    for (const command of ['dotnet', 'docker']) {
        writeFileSync(join(bin, command), readFileSync(executable));
        chmodSync(join(bin, command), 0o755);
    }
    return { PATH: `${bin}:${process.env.PATH}`, CALLS: join(directory, 'calls.jsonl') };
}

function execute(text, directory, env) {
    return spawnSync('bash', ['-euo', 'pipefail', '-c', runs(text).join('\n')], {
        cwd: directory, env: { ...process.env, ...env }, encoding: 'utf8', timeout: 5000
    });
}

function recorded(env) {
    if (!existsSync(env.CALLS)) return [];
    return readFileSync(env.CALLS, 'utf8').trim().split('\n').map(line => JSON.parse(line));
}

function packageEnvironment(directory) {
    return { ...fakeCli(directory), PACKAGE_DIRECTORY: join(directory, 'packages with spaces'), NUGET_CONFIG: join(directory, 'fresh config'),
        NUGET_SOURCE: 'https://nuget.pkg.github.com/cratis/index.json', NUGET_TOKEN: 'dummy " token $(touch TOKEN_EXECUTED)\nsecond line' };
}

test('actual package validation fails closed before any CLI authentication or push', () => {
    for (const payload of ['missing', 'empty-directory', 'empty-package', 'non-package']) fixture(directory => {
        const env = packageEnvironment(directory);
        if (payload !== 'missing') mkdirSync(env.PACKAGE_DIRECTORY);
        if (payload === 'empty-package') writeFileSync(join(env.PACKAGE_DIRECTORY, 'empty.nupkg'), '');
        if (payload === 'non-package') writeFileSync(join(env.PACKAGE_DIRECTORY, 'readme.txt'), 'not a package');
        const text = ['Verify NuGet packages', 'Add GitHub Package Registry to NuGet', 'Push NuGet packages'].map(name => step(all.get('publish-nuget-packages'), name)).join('\n');
        assert.notEqual(execute(text, directory, env).status, 0, payload);
        assert.deepEqual(recorded(env), []);
    });
});

test('actual NuGet commands preserve hostile token, version and path arguments as data', () => fixture(directory => {
    const env = packageEnvironment(directory);
    env.PACKAGE_VERSION = '19.0.0-pr-" $(touch VERSION_EXECUTED)\nnext';
    env.NUGET_OUTPUT = env.PACKAGE_DIRECTORY;
    mkdirSync(env.PACKAGE_DIRECTORY);
    writeFileSync(join(env.PACKAGE_DIRECTORY, 'fixture.nupkg'), 'package bytes');
    for (const [job, name] of [['build-nuget-packages', 'Create NuGet packages'], ['build-nuget-packages', 'Verify NuGet packages'],
        ['publish-nuget-packages', 'Verify NuGet packages'], ['publish-nuget-packages', 'Add GitHub Package Registry to NuGet'], ['publish-nuget-packages', 'Push NuGet packages']]) {
        assert.equal(execute(step(all.get(job), name), directory, env).status, 0, name);
    }
    assert.deepEqual(recorded(env), [
        ['pack', '--no-build', '--configuration', 'Debug', '-o', env.NUGET_OUTPUT, `-p:PackageVersion=${env.PACKAGE_VERSION}`],
        ['nuget', 'add', 'source', env.NUGET_SOURCE, '--name', 'github', '--username', 'cratis', '--password', env.NUGET_TOKEN, '--store-password-in-clear-text', '--configfile', env.NUGET_CONFIG],
        ['nuget', 'push', `${env.PACKAGE_DIRECTORY}/*.nupkg`, '--skip-duplicate', '--timeout', '900', '--api-key', env.NUGET_TOKEN, '--source', env.NUGET_SOURCE, '--configfile', env.NUGET_CONFIG]
    ]);
    assert.match(readFileSync(env.NUGET_CONFIG, 'utf8'), /<packageSources><clear \/><\/packageSources>/);
    assert.equal(execute(step(all.get('publish-nuget-packages'), 'Remove NuGet credentials'), directory, env).status, 0);
    assert.ok(!existsSync(env.NUGET_CONFIG));
    for (const marker of ['TOKEN_EXECUTED', 'VERSION_EXECUTED']) assert.ok(!existsSync(join(directory, marker)));
}));

test('actual Docker shells reject missing/empty archives and push only the expected env tag', () => {
    for (const payload of ['missing', 'empty', 'wrong-tag', 'image']) fixture(directory => {
        const env = { ...fakeCli(directory), IMAGE_ARCHIVE: join(directory, 'archive with spaces.tar'),
            IMAGE_TAG: 'ghcr.io/cratis/chronicle:19.0.0-" $(touch IMAGE_EXECUTED)\nnext' };
        if (payload === 'wrong-tag') env.IMAGE_MISSING = '1';
        if (payload !== 'missing') writeFileSync(env.IMAGE_ARCHIVE, ['image', 'wrong-tag'].includes(payload) ? 'archive bytes' : '');
        const text = ['Load and verify Docker image', 'Push Docker image'].map(name => step(all.get('publish-docker'), name)).join('\n');
        const result = execute(text, directory, env);
        if (payload !== 'image') {
            assert.notEqual(result.status, 0);
            const expected = payload === 'wrong-tag' ? [['load', '--input', env.IMAGE_ARCHIVE], ['image', 'inspect', '--format', '{{.Id}}', env.IMAGE_TAG]] : [];
            assert.deepEqual(recorded(env), expected);
        } else {
            assert.equal(result.status, 0);
            assert.deepEqual(recorded(env), [['load', '--input', env.IMAGE_ARCHIVE], ['image', 'inspect', '--format', '{{.Id}}', env.IMAGE_TAG], ['push', env.IMAGE_TAG]]);
        }
        assert.ok(!existsSync(join(directory, 'IMAGE_EXECUTED')));
    });
});

test('the existing lightweight CI job runs this contract without new dependencies', () => {
    const lint = readFileSync(join(root, '.github/workflows/workflow-lint.yml'), 'utf8');
    assert.match(lint, /^      - \.github\/scripts\/test-pr-prerelease-security.mjs$/m);
    assert.match(lint, /run: node --test \.github\/scripts\/test-pr-prerelease-security.mjs/);
});
