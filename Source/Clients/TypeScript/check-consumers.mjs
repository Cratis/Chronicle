// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageDirectory = path.dirname(fileURLToPath(import.meta.url));
const temporaryDirectory = mkdtempSync(path.join(tmpdir(), 'chronicle-contracts-consumer-'));

try
{
    const packed = JSON.parse(execFileSync('npm', ['pack', '--ignore-scripts', '--json', '--pack-destination', temporaryDirectory], {
        cwd: packageDirectory,
        encoding: 'utf8'
    }));
    const tarball = Array.isArray(packed) ? packed[0].filename : Object.values(packed)[0].filename;
    const consumerDirectory = path.join(temporaryDirectory, 'consumer');
    mkdirSync(consumerDirectory);
    writeFileSync(path.join(consumerDirectory, 'package.json'), JSON.stringify({ private: true, type: 'module' }));
    execFileSync('npm', [
        'install', '--ignore-scripts', '--no-audit', '--no-fund', '--no-package-lock', '--prefer-offline',
        path.join(temporaryDirectory, tarball), '@types/node@^26.3.0'
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
}
finally
{
    rmSync(temporaryDirectory, { recursive: true, force: true });
}
