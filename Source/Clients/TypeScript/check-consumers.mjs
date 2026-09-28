// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { cpSync, existsSync, mkdtempSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageDirectory = path.dirname(fileURLToPath(import.meta.url));
const temporaryDirectory = mkdtempSync(path.join(tmpdir(), 'chronicle-contracts-consumer-'));

try
{
    if (!existsSync(path.join(packageDirectory, 'dist', 'esm', 'index.d.ts')))
    {
        throw new Error('Build the TypeScript package before checking packed consumers');
    }

    // npm 10 runs prepare when packing a directory even with --ignore-scripts. Pack a snapshot
    // without that lifecycle hook so the check consumes the already-built dist, not a rebuild.
    const stagedDirectory = path.join(temporaryDirectory, 'package');
    cpSync(packageDirectory, stagedDirectory, {
        recursive: true,
        filter: source => source !== path.join(packageDirectory, 'node_modules') &&
            !source.startsWith(path.join(packageDirectory, 'node_modules') + path.sep)
    });
    const manifestPath = path.join(stagedDirectory, 'package.json');
    const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
    delete manifest.scripts.prepare;
    writeFileSync(manifestPath, JSON.stringify(manifest));

    // Only the pack step runs without the npm and Yarn variables the calling script inherited, so nothing
    // from the outer lifecycle can re-enable scripts. The consumer install keeps the caller's npm configuration
    // (registry, cache, user config).
    const packEnvironment = Object.fromEntries(Object.entries(process.env).filter(([key]) =>
        !/^npm_|^yarn_|^INIT_CWD$/i.test(key)));
    packEnvironment.npm_config_ignore_scripts = 'true';
    const packedDirectory = path.join(temporaryDirectory, 'packed');
    mkdirSync(packedDirectory);
    execFileSync('npm', ['pack', '--ignore-scripts', '--json', '--pack-destination', packedDirectory], {
        cwd: stagedDirectory,
        env: packEnvironment,
        stdio: ['ignore', 'ignore', 'inherit']
    });
    const tarballs = readdirSync(packedDirectory).filter(filename => filename.endsWith('.tgz'));
    if (tarballs.length !== 1)
    {
        throw new Error(`Expected one packed TypeScript tarball, found ${tarballs.length}`);
    }
    const tarball = path.join(packedDirectory, tarballs[0]);
    const consumerDirectory = path.join(temporaryDirectory, 'consumer');
    mkdirSync(consumerDirectory);
    writeFileSync(path.join(consumerDirectory, 'package.json'), JSON.stringify({ private: true, type: 'module' }));
    execFileSync('npm', [
        'install', '--ignore-scripts', '--no-audit', '--no-fund', '--no-package-lock', '--prefer-offline',
        tarball, '@types/node@^26.3.0'
    ], { cwd: consumerDirectory, stdio: 'inherit' });

    writeFileSync(path.join(consumerDirectory, 'consumer.ts'), `
import { ConnectionServiceDefinition, chronicleDescriptorSet } from '@cratis/chronicle.contracts';
import type { ConnectionServiceClient, EventStoresClient } from '@cratis/chronicle.contracts';
declare const clients: [ConnectionServiceClient, EventStoresClient];
void clients;
void ConnectionServiceDefinition;
void chronicleDescriptorSet;
`);

    for (const [module, moduleResolution] of [['NodeNext', 'NodeNext'], ['ESNext', 'Bundler']])
    {
        writeFileSync(path.join(consumerDirectory, 'tsconfig.json'), JSON.stringify({
            compilerOptions: { target: 'ES2022', module, moduleResolution, strict: true, skipLibCheck: false, noEmit: true, types: ['node'] },
            files: ['consumer.ts']
        }));
        console.log(`Checking packed contracts with ${moduleResolution} and skipLibCheck: false`);
        execFileSync(process.execPath, [path.join(packageDirectory, 'node_modules/typescript/bin/tsc'), '-p', 'tsconfig.json'], {
            cwd: consumerDirectory,
            stdio: 'inherit'
        });
    }

    writeFileSync(path.join(consumerDirectory, 'package.json'), JSON.stringify({ private: true, type: 'commonjs' }));
    writeFileSync(path.join(consumerDirectory, 'consumer.cjs'), `
const assert = require('node:assert/strict');
const { ConnectionServiceDefinition, chronicleDescriptorSet } = require('@cratis/chronicle.contracts');
assert.ok(ConnectionServiceDefinition);
assert.ok(chronicleDescriptorSet);
`);
    console.log('Checking packed contracts with CommonJS require()');
    execFileSync(process.execPath, ['consumer.cjs'], { cwd: consumerDirectory, stdio: 'inherit' });

    writeFileSync(path.join(consumerDirectory, 'tsconfig.json'), JSON.stringify({
        compilerOptions: { target: 'ES2022', module: 'Node16', moduleResolution: 'Node16', strict: true, skipLibCheck: false, noEmit: true, types: ['node'] },
        files: ['consumer.ts']
    }));
    console.log('Checking packed CommonJS contracts with Node16 and skipLibCheck: false');
    execFileSync(process.execPath, [path.join(packageDirectory, 'node_modules/typescript/bin/tsc'), '-p', 'tsconfig.json'], {
        cwd: consumerDirectory,
        stdio: 'inherit'
    });
}
finally
{
    rmSync(temporaryDirectory, { recursive: true, force: true });
}
