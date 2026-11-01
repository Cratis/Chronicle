// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Native MSBuild guard checks. Parent regressions use NoBuild and isolated local outputs;
// normal single-TFM Pack/Publish is exercised, but no compiler, Docker/image or test assembly runs.
import assert from 'node:assert/strict';
import path from 'node:path';
import os from 'node:os';
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readdirSync } from 'node:fs';

const clientProject = 'Source/Clients/DotNET/DotNET.csproj';
const root = path.join(os.tmpdir(), 'chronicle-guard-check', 'rolling-delivery-diagnostics');
const enabled = ['-p:EnableRollingDeliveryDiagnostics=true', `-p:ArtifactsPath=${root}`];
const validate = 'ValidateRollingDeliveryDiagnostics';

function check(name, properties, expectedError = '', { target = validate, project = clientProject } = {}) {
    const result = spawnSync('dotnet', ['msbuild', project, '-nologo', '-m:4', '-nodeReuse:false', '-p:TargetFramework=net10.0', `-t:${target}`, ...properties], {
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
check('distribution guard denies pack/publish/image', enabled, 'cannot be packed, published or put in an image', { target: 'ForbidRollingDeliveryDiagnosticsDistribution' });

// A parent publishes copy-local client references without running DotNET's own Publish target.
// Exercise real SDK entry points (including --no-build) rather than just invoking the guard target.
const base = path.resolve(process.env.RUNNER_TEMP ?? '.ai-work/t5ep', 'rolling-guard-regression');
mkdirSync(base, { recursive: true });
const output = mkdtempSync(path.join(base, 'parent-'));
const parent = 'Source/Clients/AspNetCore/AspNetCore.csproj';
const noBuild = ['-p:Configuration=Release', '-p:NoBuild=true', '-p:DisableDockerBuild=true', '-p:DisableProxyGenerator=true'];
const deniedPublish = path.join(output, 'denied-publish');
const deniedPack = path.join(output, 'denied-pack');
const parentError = 'cannot be packed, published or put in an image (AspNetCore)';
check('parent Publish with diagnostics denied before copy-local publication', [...enabled, ...noBuild, '-p:IsPublishable=true', `-p:PublishDir=${deniedPublish}${path.sep}`], parentError, { target: 'Publish', project: parent });
check('parent Pack with diagnostics denied before package creation', [...enabled, ...noBuild, '-p:IsPackable=true', `-p:PackageOutputPath=${deniedPack}`], parentError, { target: 'Pack', project: parent });
assert.equal(existsSync(deniedPublish), false, 'denied Publish created its output directory');
assert.equal(existsSync(deniedPack), false, 'denied Pack created its output directory');
const normalPublish = path.join(output, 'normal-publish');
const normalPack = path.join(output, 'normal-pack');
check('normal parent Publish succeeds', [...noBuild, '-p:IsPublishable=true', `-p:PublishDir=${normalPublish}${path.sep}`], '', { target: 'Publish', project: parent });
check('normal parent Pack succeeds', [...noBuild, '-p:IsPackable=true', `-p:PackageOutputPath=${normalPack}`], '', { target: 'Pack', project: parent });
assert.ok(existsSync(path.join(normalPublish, 'Cratis.Chronicle.dll')), 'normal parent Publish did not copy its client reference');
assert.ok(readdirSync(normalPack).some(file => file.endsWith('.nupkg')), 'normal parent Pack did not produce a package');
check('server Publish with diagnostics denied', [...enabled, ...noBuild, '-p:IsPublishable=true'], 'cannot be packed, published or put in an image (Server)', { target: 'Publish', project: 'Source/Kernel/Server/Server.csproj' });
check('local kernel helper denies publication before deleting outputs', enabled, 'cannot be packed, published or put in an image (LocalKernelPublish)', { target: 'PublishKernel', project: 'Integration/LocalKernelPublish.proj' });
check('diagnostic Docker build denied', [...enabled, '-p:DisableDockerBuild=false'], 'cannot be packed, published or put in an image (SqlClustering)', { target: 'BuildDocker', project: 'Integration/SqlClustering/SqlClustering.csproj' });
check('disabled Docker target permits the candidate', [...enabled, '-p:DisableDockerBuild=true'], '', { target: 'BuildDocker', project: 'Integration/SqlClustering/SqlClustering.csproj' });
console.log(`Parent regression artifacts: ${output}`);
