// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Native MSBuild property/guard checks only: no compiler, package, publish, image, or test assembly execution.
import assert from 'node:assert/strict';
import path from 'node:path';
import os from 'node:os';
import { spawnSync } from 'node:child_process';

const project = 'Source/Clients/DotNET/DotNET.csproj';
const root = path.join(os.tmpdir(), 'chronicle-guard-check', 'rolling-delivery-diagnostics');
const enabled = ['-p:EnableRollingDeliveryDiagnostics=true', `-p:ArtifactsPath=${root}`];
const validate = 'ValidateRollingDeliveryDiagnostics';

function check(name, properties, expectedError = '', target = validate) {
    const result = spawnSync('dotnet', ['msbuild', project, '-nologo', '-p:TargetFramework=net10.0', `-t:${target}`, ...properties], {
        encoding: 'utf8', timeout: 60000, maxBuffer: 1024 * 1024
    });
    assert.ifError(result.error);
    const output = result.stdout + result.stderr;
    if (expectedError) {
        assert.notEqual(result.status, 0, `${name}: unexpectedly accepted`);
        assert.ok(output.includes(expectedError), `${name}: wrong failure\n${output}`);
    } else {
        assert.equal(result.status, 0, `${name}: ${output}`);
    }
    console.log(`PASS ${name}`);
}

check('normal defaults', []);
check('isolated candidate accepted', enabled);
check('raw symbol denied', ['-p:DefineConstants=CHRONICLE_ROLLING_DIAGNOSTICS'], 'require EnableRollingDeliveryDiagnostics');
check('missing isolation denied', ['-p:EnableRollingDeliveryDiagnostics=true'], 'require an absolute ArtifactsPath');
check('shipping artifact tree denied', ['-p:EnableRollingDeliveryDiagnostics=true', `-p:ArtifactsPath=${path.resolve('Source/Clients/DotNET/bin/rolling-delivery-diagnostics')}`], 'never in shipping artifact inputs');
check('output override denied', [...enabled, `-p:OutputPath=${path.join(os.tmpdir(), 'shipping-output')}`], 'output and intermediate paths');
check('intermediate override denied', [...enabled, `-p:IntermediateOutputPath=${path.join(os.tmpdir(), 'shipping-intermediate')}`], 'output and intermediate paths');
check('packaging mode denied', [...enabled, '-p:IsPackaging=true'], 'cannot be packaged');
check('automatic packing denied', [...enabled, '-p:GeneratePackageOnBuild=true'], 'cannot be packaged');
check('distribution guard denies pack/publish/image', enabled, 'cannot be packed, published or put in an image', 'ForbidRollingDeliveryDiagnosticsDistribution');
