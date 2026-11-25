// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { createHash, randomUUID } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, renameSync, writeFileSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';

const sources = [
    'Source/Kernel/Core/Security/AdministratorRoster.cs',
    'Source/Kernel/Core/Security/AdministratorRosterLogging.cs'
];

function writeIfChanged(path, contents) {
    if (existsSync(path) && readFileSync(path).equals(contents)) return;
    const temporary = `${path}.${randomUUID()}.tmp`;
    writeFileSync(temporary, contents, { flag: 'wx' });
    renameSync(temporary, path);
}

function extractionIdentity(revision) {
    const implementation = createHash('sha256')
        .update(readFileSync(new URL(import.meta.url)))
        .update(readFileSync(new URL('./Baseline.csproj', import.meta.url)))
        .digest('hex');
    return Buffer.from(JSON.stringify({ revision, sources, implementation }));
}

function isCached(stamp, identity, directory) {
    if (!existsSync(stamp)) return false;
    if (!readFileSync(stamp).equals(identity)) return false;
    return sources.every(source => existsSync(join(directory, basename(source))));
}

function extract(revision, directory) {
    if (!revision || !directory) throw new Error('Expected a baseline revision and output directory.');
    const output = resolve(directory);
    const stamp = join(output, 'baseline-extraction.json');
    const identity = extractionIdentity(revision);
    if (isCached(stamp, identity, output)) return;

    mkdirSync(output, { recursive: true });
    writeIfChanged(stamp, Buffer.alloc(0));
    const extracted = sources.map(source => ({
        path: join(output, basename(source)),
        contents: execFileSync('git', ['show', `${revision}:${source}`], { stdio: ['ignore', 'pipe', 'pipe'] })
    }));
    for (const source of extracted) writeIfChanged(source.path, source.contents);
    writeIfChanged(stamp, identity);
    console.log(`Extracted roster baseline ${revision}.`);
}

try {
    extract(process.argv[2], process.argv[3]);
} catch (error) {
    console.error(`Baseline extraction failed: ${error.message}`);
    process.exitCode = 1;
}
