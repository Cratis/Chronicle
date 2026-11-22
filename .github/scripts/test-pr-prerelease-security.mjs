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

function isIndented(line, indentation) {
    return line.trim() === '' || line.startsWith(' '.repeat(indentation));
}

function blockEnd(lines, index, indentation) {
    while (index + 1 < lines.length && isIndented(lines[index + 1], indentation)) index++;
    return index;
}

function skipDataBlock(lines, index) {
    const block = lines[index].match(/^(\s*)([\w-]+): [|>][+-]?$/);
    if (!block || block[2] === 'run') return index;
    return blockEnd(lines, index, block[1].length + 1);
}

function readRun(lines, index) {
    const match = lines[index].match(/^        run: (.+)$/);
    assert.ok(match, `Unrecognized run field: ${lines[index]}`);
    const value = match[1];
    if (value !== '|') {
        assert.doesNotMatch(value, /^[|>"'&*!{[]/, 'Unsupported run scalar');
        return { script: value, end: index };
    }
    const end = blockEnd(lines, index, 10);
    const body = lines.slice(index + 1, end + 1).map(line => line.replace(/^ {10}/, ''));
    assert.ok(body.some(line => line.trim()), 'Empty run block');
    return { script: body.join('\n').trimEnd(), end };
}

// Deliberately accept only the workflow's canonical text shape. actionlint validates YAML;
// unknown run representations must fail here, never disappear from the security scan.
function runs(text) {
    const lines = text.split('\n');
    const found = [];
    for (let index = 0; index < lines.length; index++) {
        if (/^\s*#/.test(lines[index])) continue;
        const end = skipDataBlock(lines, index);
        if (end > index) { index = end; continue; }
        if (!/(?:^|[\s,{])(?:run|"run"|'run')\s*:/.test(lines[index])) continue;
        const run = readRun(lines, index);
        found.push(run.script);
        index = run.end;
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

// This complete fixed program is allowlisted: packages are ZIP/XML data, never code.
const packageVerificationScript = String.raw`set -euo pipefail
python3 - <<'PY'
import os
from pathlib import Path
import re
import xml.etree.ElementTree as ET
import zipfile

def fail():
    print("::error::Invalid prerelease NuGet package identity or artifact contents.")
    raise SystemExit(1)

try:
    expected_version = os.environ["EXPECTED_VERSION"]
    if "-" not in expected_version:
        fail()
    packages = list(Path(os.environ["PACKAGE_DIRECTORY"]).iterdir())
    if not packages:
        fail()
    suffix = f".{expected_version}.nupkg"
    for package in packages:
        if package.is_symlink() or not package.is_file() or package.stat().st_size == 0:
            fail()
        if not package.name.endswith(suffix):
            fail()
        package_id = package.name[:-len(suffix)]
        if re.fullmatch(r"Cratis\.[A-Za-z0-9.]+", package_id) is None:
            fail()
        with zipfile.ZipFile(package) as archive:
            specs = [entry for entry in archive.infolist() if entry.filename.lower().endswith(".nuspec")]
            if len(specs) != 1 or "/" in specs[0].filename:
                fail()
            document = ET.fromstring(archive.read(specs[0]))
        if document.tag.split("}")[-1] != "package":
            fail()
        metadata, = document.findall("{*}metadata")
        identity, = metadata.findall("{*}id")
        version, = metadata.findall("{*}version")
        if len(identity) or len(version) or identity.text != package_id or version.text != expected_version:
            fail()
except (OSError, ValueError, RuntimeError, LookupError, zipfile.BadZipFile, ET.ParseError):
    fail()
PY`;

// Exact command lines form a small publisher allowlist, not a blacklist of build verbs.
const publisherCommands = new Set([
    'set -euo pipefail',
    'cat > "$NUGET_CONFIG" <<\'EOF\'', '<?xml version="1.0" encoding="utf-8"?>',
    '<configuration><packageSources><clear /></packageSources></configuration>', 'EOF',
    'dotnet nuget add source "$NUGET_SOURCE" --name github --username cratis --password "$NUGET_TOKEN" --store-password-in-clear-text --configfile "$NUGET_CONFIG"',
    'dotnet nuget push "$PACKAGE_DIRECTORY/*.nupkg" --skip-duplicate --timeout 900 --api-key "$NUGET_TOKEN" --source "$NUGET_SOURCE" --configfile "$NUGET_CONFIG"',
    'rm -f "$NUGET_CONFIG"', 'test -s "$IMAGE_ARCHIVE"', 'docker load --input "$IMAGE_ARCHIVE"',
    'test -n "$(docker image inspect --format \'{{.Id}}\' "$IMAGE_TAG")"', 'docker push "$IMAGE_TAG"'
]);

function assertPublisherAction(action) {
    assert.ok(['actions/setup-dotnet', 'actions/download-artifact', 'docker/login-action', 'mshick/add-pr-comment'].includes(action[1].split('@')[0]), `Publisher action: ${action[1]}`);
}

function assertPublisherScript(script) {
    if (script === packageVerificationScript) return;
    for (const line of script.split('\n')) assert.ok(publisherCommands.has(line.trim()), `Publisher command: ${line}`);
}

function assertPublisherJob(job, name) {
    assert.ok(job, `Missing publisher ${name}`);
    assert.doesNotMatch(job, /^    (?:env|defaults):|working-directory:/m);
    [...job.matchAll(/^\s+(?:- )?uses: (\S+)/gm)].forEach(assertPublisherAction);
    runs(job).forEach(assertPublisherScript);
    assert.ok(!job.includes('uses: ./'));
}

function assertPublisherIsolation(text) {
    const all = jobs(text);
    for (const name of publishers) assertPublisherJob(all.get(name), name);
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

function assertArtifact({ builder, publisher, uploadName, downloadName, artifact, output, destination }) {
    const upload = map(step(all.get(builder), uploadName), 'with', 8);
    assert.deepEqual(upload, { 'retention-days': '3', name: artifact, path: output, 'if-no-files-found': 'error' });
    const download = map(step(all.get(publisher), downloadName), 'with', 8);
    assert.deepEqual(download, { name: artifact, path: destination });
}

test('package and image handoffs are exact-name current-run artifacts, not caches', () => {
    assertArtifact({ builder: 'build-nuget-packages', publisher: 'publish-nuget-packages',
        uploadName: 'Upload NuGet packages', downloadName: 'Download NuGet packages',
        artifact: 'pr-nuget-packages', output: './Artifacts/NuGet/*.nupkg', destination: '${{ runner.temp }}/pr-nuget-packages' });
    assertArtifact({ builder: 'build-docker', publisher: 'publish-docker',
        uploadName: 'Upload Docker image', downloadName: 'Download Docker image',
        artifact: 'pr-docker-image', output: '${{ runner.temp }}/pr-docker-image.tar', destination: '${{ runner.temp }}/pr-docker-image' });
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

function assertPackageVerification(text) {
    const job = jobs(text).get('publish-nuget-packages');
    const verification = step(job, 'Verify NuGet packages');
    assert.equal(map(verification, 'env', 8).EXPECTED_VERSION, '${{ needs.release.outputs.version }}');
    const script = runs(verification)[0];
    assert.equal(script, packageVerificationScript);
    assert.ok(script.includes('if "-" not in expected_version:'));
    assert.ok(script.includes('suffix = f".{expected_version}.nupkg"'));
    assert.ok(script.includes('if not package.name.endswith(suffix):'));
    assert.ok(script.includes('identity.text != package_id or version.text != expected_version'));
    const names = steps(job).map(value => value.name);
    assert.ok(names.indexOf('Verify NuGet packages') < names.indexOf('Add GitHub Package Registry to NuGet'));
    assert.ok(names.indexOf('Verify NuGet packages') < names.indexOf('Push NuGet packages'));
}

test('NuGet publisher verifies exact prerelease filename and nuspec identity before credentials or push', () => {
    assertPackageVerification(workflow);
    for (const guard of ['if "-" not in expected_version:', 'if not package.name.endswith(suffix):']) {
        const altered = workflow.replace(guard, 'if False:');
        assert.ok(altered !== workflow, 'Identity guard mutation must apply');
        assert.throws(() => assertPackageVerification(altered));
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
        ['Verify NuGet packages', { ...packages, EXPECTED_VERSION: '${{ needs.release.outputs.version }}' }], ['Add GitHub Package Registry to NuGet', credential],
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
    return { ...fakeCli(directory), EXPECTED_VERSION: '19.0.0-pr-fixture', PACKAGE_DIRECTORY: join(directory, 'packages with spaces'), NUGET_CONFIG: join(directory, 'fresh config'),
        NUGET_SOURCE: 'https://nuget.pkg.github.com/cratis/index.json', NUGET_TOKEN: 'dummy " token $(touch TOKEN_EXECUTED)\nsecond line' };
}

function escapeXml(value) {
    const entities = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;' };
    return value.replace(/[&<>"']/g, character => entities[character]);
}

const packageFixtureWriter = `import json, sys, zipfile
with zipfile.ZipFile(sys.argv[1], 'w') as archive:
    for name, contents in json.loads(sys.argv[2]).items():
        archive.writestr(name, contents)
`;

function writePackage(env, options = {}) {
    const id = options.id ?? 'Cratis.Chronicle';
    const version = options.version ?? env.EXPECTED_VERSION;
    const identity = escapeXml(options.nuspecId ?? id);
    const metadataVersion = escapeXml(options.nuspecVersion ?? version);
    const xml = options.xml ?? `<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>${identity}</id><version>${metadataVersion}</version></metadata></package>`;
    const entries = options.entries ?? { [`${id}.nuspec`]: xml };
    mkdirSync(env.PACKAGE_DIRECTORY, { recursive: true });
    const path = join(env.PACKAGE_DIRECTORY, `${id}.${version}.nupkg`);
    const result = spawnSync('python3', ['-c', packageFixtureWriter, path, JSON.stringify(entries)], { encoding: 'utf8', timeout: 5000 });
    assert.equal(result.status, 0, result.stderr);
    return path;
}

function addArtifactFile(env, name, contents = 'unexpected') {
    mkdirSync(env.PACKAGE_DIRECTORY, { recursive: true });
    writeFileSync(join(env.PACKAGE_DIRECTORY, name), contents);
}

const invalidPackages = [
    { name: 'missing', prepare: () => {} },
    { name: 'empty-directory', prepare: env => mkdirSync(env.PACKAGE_DIRECTORY) },
    { name: 'empty-package', prepare: env => writeFileSync(writePackage(env), '') },
    { name: 'non-package', prepare: env => addArtifactFile(env, 'readme.txt') },
    { name: 'stable-release', prepare: env => { env.EXPECTED_VERSION = '99.0.0'; writePackage(env); } },
    { name: 'stable-filename', prepare: env => writePackage(env, { version: '99.0.0' }) },
    { name: 'wrong-prerelease', prepare: env => writePackage(env, { version: '19.0.0-other-pr' }) },
    { name: 'mixed-versions', prepare: env => { writePackage(env); writePackage(env, { id: 'Cratis.Other', version: '99.0.0' }); } },
    { name: 'unrelated-id', prepare: env => writePackage(env, { id: 'Unrelated.Package' }) },
    { name: 'invalid-id-character', prepare: env => writePackage(env, { id: 'Cratis.Bad-Id' }) },
    { name: 'missing-id-prefix-dot', prepare: env => writePackage(env, { id: 'CratisOther' }) },
    { name: 'extra-file', prepare: env => { writePackage(env); addArtifactFile(env, 'readme.txt'); } },
    { name: 'hidden-file', prepare: env => { writePackage(env); addArtifactFile(env, '.hidden'); } },
    { name: 'extra-directory', prepare: env => { writePackage(env); mkdirSync(join(env.PACKAGE_DIRECTORY, 'extra')); } },
    { name: 'nuspec-id-mismatch', prepare: env => writePackage(env, { nuspecId: 'Unrelated.Package' }) },
    { name: 'nuspec-version-mismatch', prepare: env => writePackage(env, { nuspecVersion: '99.0.0' }) },
    { name: 'missing-nuspec', prepare: env => writePackage(env, { entries: {} }) },
    { name: 'multiple-nuspecs', prepare: env => writePackage(env, { entries: { 'one.nuspec': '<package/>', 'two.nuspec': '<package/>' } }) },
    { name: 'invalid-xml', prepare: env => writePackage(env, { xml: '<package' }) },
    { name: 'missing-metadata', prepare: env => writePackage(env, { xml: '<package/>' }) },
    { name: 'duplicate-identity', prepare: env => writePackage(env, { xml: '<package><metadata><id>Cratis.Chronicle</id><id>Other</id><version>19.0.0-pr-fixture</version></metadata></package>' }) },
    { name: 'nested-identity', prepare: env => writePackage(env, { xml: '<package><metadata><id>Cratis.Chronicle<other>Different</other></id><version>19.0.0-pr-fixture</version></metadata></package>' }) },
    { name: 'invalid-archive', prepare: env => writeFileSync(writePackage(env), 'not a ZIP') }
];

function assertRejectedPackage(directory, scenario) {
    const env = packageEnvironment(directory);
    scenario.prepare(env);
    const text = ['Verify NuGet packages', 'Add GitHub Package Registry to NuGet', 'Push NuGet packages'].map(name => step(all.get('publish-nuget-packages'), name)).join('\n');
    const result = execute(text, directory, env);
    assert.notEqual(result.status, 0, scenario.name);
    assert.match(result.stdout, /::error::/);
    assert.deepEqual(recorded(env), []);
}

test('actual package validation fails closed before any CLI authentication or push', () => {
    invalidPackages.forEach(scenario => fixture(directory => assertRejectedPackage(directory, scenario)));
});

test('actual NuGet commands preserve hostile token, version and path arguments as data', () => fixture(directory => {
    const env = packageEnvironment(directory);
    env.PACKAGE_VERSION = '19.0.0-pr-" $(touch VERSION_EXECUTED)\nnext';
    env.NUGET_OUTPUT = env.PACKAGE_DIRECTORY;
    env.EXPECTED_VERSION = env.PACKAGE_VERSION;
    writePackage(env);
    writePackage(env, { id: 'Cratis.Other' });
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
