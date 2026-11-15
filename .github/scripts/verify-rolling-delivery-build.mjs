// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Native MSBuild guard checks. Parent regressions use NoBuild and isolated local outputs;
// normal Publish and opt-in multi-TFM Pack are exercised, but no compiler, Docker/image or test assembly runs.
import assert from 'node:assert/strict';
import path from 'node:path';
import os from 'node:os';
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync } from 'node:fs';

const clientProject = 'Source/Clients/DotNET/DotNET.csproj';
const root = path.join(os.tmpdir(), 'chronicle-guard-check', 'rolling-delivery-diagnostics');
const enabled = ['-p:EnableRollingDeliveryDiagnostics=true', `-p:ArtifactsPath=${root}`];
const validate = 'ValidateRollingDeliveryDiagnostics';
// The default build framework: project references resolve to it, so a parent probe must use it too.
const framework = 'net11.0';

function check(name, properties, expectedError = '', { target = validate, project = clientProject } = {}) {
    const result = spawnSync('dotnet', ['msbuild', project, '-nologo', '-m:4', '-nodeReuse:false', `-p:TargetFramework=${framework}`, `-p:TargetFrameworks=${framework}`, `-t:${target}`, ...properties], {
        encoding: 'utf8', timeout: 60000, maxBuffer: 1024 * 1024
    });
    return checkResult(name, result, expectedError);
}

function checkResult(name, result, expectedError = '') {
    assert.ifError(result.error);
    const output = result.stdout + result.stderr;
    if (expectedError) {
        assert.notEqual(result.status, 0, `${name}: unexpectedly accepted`);
        assert.ok(output.includes(expectedError), `${name}: wrong failure\n${output}`);
    } else {
        assert.equal(result.status, 0, `${name}: ${output}`);
    }
    console.log(`PASS ${name}`);
    return output;
}

function pack(name, properties, expectedError = '') {
    const result = spawnSync('dotnet', ['pack', parent, '--no-build', '--configuration', 'Release',
        '-p:IsPackaging=true', '-p:DisableProxyGenerator=true', '-p:DisableDockerBuild=true',
        '-p:PackageVersion=0.0.0-guard', `-p:PackageOutputPath=${path.join(output, 'normal-pack')}`, ...properties], {
        encoding: 'utf8', timeout: 120000, maxBuffer: 1024 * 1024
    });
    return checkResult(name, result, expectedError);
}

function verifyPackedFrameworks() {
    const packageFile = path.join(output, 'normal-pack', 'Cratis.Chronicle.AspNetCore.0.0.0-guard.nupkg');
    assert.ok(existsSync(packageFile), 'normal parent Pack did not create its package');
    const listing = spawnSync('unzip', ['-l', packageFile], { encoding: 'utf8', timeout: 60000 });
    assert.ifError(listing.error);
    assert.equal(listing.status, 0, `cannot inspect normal parent package: ${listing.stderr}`);
    for (const targetFramework of ['net10.0', 'net11.0']) {
        assert.ok(listing.stdout.includes(`lib/${targetFramework}/Cratis.Chronicle.AspNetCore.dll`), `normal parent package is missing ${targetFramework}`);
    }
}

check('normal defaults', []);
check('distribution guard allows normal pack/publish/image', [], '', { target: 'ForbidRollingDeliveryDiagnosticsDistribution' });
// Packaging props must not expand this single-TFM probe back to the shipping framework list.
const targeting = check('single-TFM packaging evaluation', ['-p:IsPackaging=true', '-getProperty:TargetFramework,TargetFrameworks']);
assert.deepEqual(JSON.parse(targeting).Properties, { TargetFramework: framework, TargetFrameworks: framework });
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
check('parent distribution guard allows normal pack/publish/image', [], '', { target: 'ForbidRollingDeliveryDiagnosticsDistribution', project: parent });
const noBuild = ['-p:Configuration=Release', '-p:NoBuild=true', '-p:DisableDockerBuild=true', '-p:DisableProxyGenerator=true'];
const deniedPublish = path.join(output, 'denied-publish');
const deniedPack = path.join(output, 'denied-pack');
const parentError = 'cannot be packed, published or put in an image (AspNetCore)';
check('parent Publish with diagnostics denied before copy-local publication', [...enabled, ...noBuild, '-p:IsPublishable=true', `-p:PublishDir=${deniedPublish}${path.sep}`], parentError, { target: 'Publish', project: parent });
check('parent Pack with diagnostics denied before package creation', [...enabled, ...noBuild, '-p:IsPackable=true', `-p:PackageOutputPath=${deniedPack}`], parentError, { target: 'Pack', project: parent });
assert.equal(existsSync(deniedPublish), false, 'denied Publish created its output directory');
assert.equal(existsSync(deniedPack), false, 'denied Pack created its output directory');
const normalPublish = path.join(output, 'normal-publish');
check('normal parent Publish succeeds', [...noBuild, '-p:IsPublishable=true', `-p:PublishDir=${normalPublish}${path.sep}`], '', { target: 'Publish', project: parent });
// CI has already built all shipped frameworks in Release. Do not narrow this Pack to a single TFM:
// NuGet requires lib assets for every dependency group, and --no-build reuses those existing outputs.
if (process.env.ROLLING_GUARD_PACK_ALL_FRAMEWORKS === 'true') {
    pack('normal parent multi-framework Pack succeeds', []);
    verifyPackedFrameworks();
}
assert.ok(existsSync(path.join(normalPublish, 'Cratis.Chronicle.dll')), 'normal parent Publish did not copy its client reference');
check('server Publish with diagnostics denied', [...enabled, ...noBuild, '-p:IsPublishable=true'], 'cannot be packed, published or put in an image (Server)', { target: 'Publish', project: 'Source/Kernel/Server/Server.csproj' });
check('local kernel helper denies publication before deleting outputs', enabled, 'cannot be packed, published or put in an image (LocalKernelPublish)', { target: 'PublishKernel', project: 'Integration/LocalKernelPublish.proj' });
check('diagnostic Docker build denied', [...enabled, '-p:DisableDockerBuild=false'], 'cannot be packed, published or put in an image (SqlClustering)', { target: 'BuildDocker', project: 'Integration/SqlClustering/SqlClustering.csproj' });
check('disabled Docker target permits the candidate', [...enabled, '-p:DisableDockerBuild=true'], '', { target: 'BuildDocker', project: 'Integration/SqlClustering/SqlClustering.csproj' });
console.log(`Parent regression artifacts: ${output}`);
