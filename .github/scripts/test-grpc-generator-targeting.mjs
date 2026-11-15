// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const root = fileURLToPath(new URL('../../', import.meta.url));
const project = 'Source/Tools/GrpcCodeGenerator/GrpcCodeGenerator.csproj';

function evaluate(isPackaging) {
    const result = spawnSync('dotnet', [
        'msbuild', project, '-nologo', '-p:Configuration=Release',
        `-p:IsPackaging=${isPackaging}`,
        '-getProperty:TargetFramework,TargetFrameworks', '-getItem:PackageVersion'
    ], { cwd: root, encoding: 'utf8', timeout: 45000 });
    assert.ifError(result.error);
    assert.equal(result.status, 0, `MSBuild evaluation failed: ${result.stderr}\n${result.stdout}`);
    return JSON.parse(result.stdout);
}

function assertSingleTarget(evaluation) {
    assert.equal(evaluation.Properties.TargetFramework, 'net11.0');
    assert.equal(evaluation.Properties.TargetFrameworks, '', 'The host tool must not inherit packaging frameworks');
}

function roslynPins(evaluation) {
    return ['Microsoft.CodeAnalysis.CSharp', 'Microsoft.CodeAnalysis.Workspaces.Common'].map(name => {
        const matches = evaluation.Items.PackageVersion.filter(item => item.Identity === name);
        assert.equal(matches.length, 1, `Expected exactly one evaluated version for ${name}`);
        assert.ok(matches[0].Version, `Missing evaluated version for ${name}`);
        return matches[0].Version;
    });
}

test('the generator is a net11 host tool in a normal Release evaluation', () => {
    assertSingleTarget(evaluate(false));
});

test('packaging preserves the single net11 target and normal Roslyn pins', () => {
    const packaging = evaluate(true);
    assertSingleTarget(packaging);
    const pins = roslynPins(packaging);
    assert.deepEqual(pins, roslynPins(evaluate(false)));
    assert.equal(pins[0], pins[1], 'CSharp and Workspaces.Common must use compatible net11 pins');
});
