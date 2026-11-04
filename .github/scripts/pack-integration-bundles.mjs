// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Packs one runtime bundle per integration project (client, kernel, clustering): the project's Release
// output directory plus a selection record. The record is evaluated and validated on the producer, where the
// full restore exists, so a shard can run `dotnet <dll>` from the bundle without restoring or evaluating.
import { access, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';
import { evaluateProject, v3Projects } from './mixed-specs-selection.mjs';
import { runProcess } from './mixed-specs-process.mjs';

const fail = message => { throw new Error(message); };
const names = ['client', 'kernel', 'clustering'];
const posix = value => value.split(path.sep).join('/');

export function layout(name) {
    if (!names.includes(name)) fail(`Unknown integration bundle ${name}`);
    const label = name[0].toUpperCase() + name.slice(1);
    const project = `Integration/${label}/${label}.csproj`;
    if (!v3Projects.includes(project)) fail(`Unclassified integration project ${project}`);
    const directory = path.posix.dirname(project);
    return { project, outputDirectory: `${directory}/bin/Release/net10.0`, binDirectory: `${directory}/bin/Release`,
        selection: `.integration-bundles/${name}.selection.json`, archive: `.integration-bundles/integration-bundle-${name}.tar.zst` };
}

export function selectionRecord(root, info) {
    const { binDirectory } = layout(names.find(name => layout(name).project === info.project) ?? '');
    const dll = posix(path.relative(path.resolve(root), path.resolve(root, info.dll)));
    if (!dll.startsWith(`${binDirectory}/`)) fail(`${info.project}: dll ${dll} is outside ${binDirectory}/`);
    return { ...info, dll };
}

export async function readSelection(root, project, file) {
    const record = JSON.parse(await readFile(path.resolve(root, file), 'utf8'));
    if (record.project !== project) fail(`${file}: selection is for ${record.project}, not ${project}`);
    if (record.runner !== 'v3') fail(`${file}: executable v3 runner required`);
    if (typeof record.dll !== 'string' || path.isAbsolute(record.dll) || record.dll.split('/').includes('..')) fail(`${file}: dll must be a relative path inside the checkout`);
    await access(path.resolve(root, record.dll)).catch(() => fail(`${file}: ${record.dll} is missing from the bundle`));
    return record;
}

export async function pack(root, out, bundles = names, { execute = runProcess, evaluate = evaluateProject } = {}) {
    await mkdir(path.resolve(root, out), { recursive: true });
    for (const name of bundles) {
        const files = layout(name);
        const record = selectionRecord(root, await evaluate(root, files.project, execute));
        await writeFile(path.resolve(root, files.selection), `${JSON.stringify(record, null, 2)}\n`);
        const result = await execute('tar', ['-I', 'zstd -T0', '-cf', files.archive, files.outputDirectory, files.selection], { cwd: root, timeout: 600000 });
        if (result.code !== 0) fail(`${name}: tar exited ${result.code}`);
    }
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    const { values, positionals } = parseArgs({ allowPositionals: true, options: { out: { type: 'string', default: '.integration-bundles' } } });
    if (positionals[0] !== 'pack' || values.out !== '.integration-bundles') fail('Usage: pack-integration-bundles.mjs pack [--out .integration-bundles]');
    pack(process.cwd(), values.out).catch(error => { console.error(error.message); process.exitCode = error.exitCode ?? 1; });
}
