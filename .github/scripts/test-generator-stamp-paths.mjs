// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import os from 'node:os';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const root = fileURLToPath(new URL('../../', import.meta.url));
const artifacts = path.join(os.tmpdir(), 'chronicle-stamp-spec-artifacts');

const generators = [
    { project: 'Source/Kernel/Contracts/Contracts.csproj', target: '_PrepareProtoGeneration', property: '_ProtoGenerationStamp', stamp: 'GenerateProtoFiles.stamp', extra: ['-p:TargetFramework=net10.0'] },
    { project: 'Source/Kernel/Core/Core.csproj', target: '_PrepareGrpcContractGeneration', property: '_GrpcContractGenerationStamp', stamp: 'GenerateGrpcContracts.stamp', extra: ['-p:TargetFramework=net10.0'] }
];

function stampPath(generator, extra) {
    const result = spawnSync('dotnet', [
        'msbuild', generator.project, '-nologo', '-p:Configuration=Release', ...generator.extra, ...extra,
        `-t:${generator.target}`, `-getProperty:${generator.property}`
    ], { cwd: root, encoding: 'utf8', timeout: 120000 });
    assert.ifError(result.error);
    assert.equal(result.status, 0, `MSBuild evaluation failed: ${result.stderr}\n${result.stdout}`);
    return result.stdout.trim();
}

for (const generator of generators) {
    test(`${generator.stamp} is project-relative by default`, () => {
        const stamp = stampPath(generator, []);
        assert.ok(path.isAbsolute(stamp), stamp);
        assert.ok(!stamp.includes('//'), `Stamp path is double-rooted: ${stamp}`);
        assert.ok(stamp.startsWith(path.join(root, path.dirname(generator.project))), stamp);
        assert.ok(stamp.endsWith(generator.stamp), stamp);
    });

    test(`${generator.stamp} honours an absolute ArtifactsPath`, () => {
        const stamp = stampPath(generator, [`-p:ArtifactsPath=${artifacts}`]);
        assert.ok(stamp.startsWith(artifacts), `Stamp path must live under the artifacts path: ${stamp}`);
        assert.ok(!stamp.slice(1).includes(`${path.sep}${path.sep}`) && !stamp.includes(path.join(root, path.dirname(generator.project)) + path.sep + artifacts), `Stamp path is double-rooted: ${stamp}`);
        assert.ok(stamp.endsWith(generator.stamp), stamp);
    });
}
