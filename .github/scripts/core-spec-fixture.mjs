// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { mkdir, writeFile, readFile } from 'node:fs/promises';
import path from 'node:path';
import { CORE_PROJECT, coreUnitId, loadPartition } from './core-spec-partition.mjs';
import { exclusionPath } from './core-spec-coverage.mjs';

export const expandedProjects = projects => projects.flatMap(project => project === CORE_PROJECT
    ? [1, 2, 3, 4].map(coreUnitId) : [project]);
export async function coreExclusionFixture(root) {
    const entries = JSON.parse(await readFile(new URL('./core-spec-exclusions.json', import.meta.url), 'utf8'));
    await mkdir(path.join(root, '.github/scripts'), { recursive: true });
    await writeFile(path.join(root, exclusionPath), JSON.stringify(entries));
    const directory = path.join(root, path.dirname(CORE_PROJECT));
    await mkdir(directory, { recursive: true });
    await writeFile(path.join(directory, 'ExcludedHelpers.cs'), entries.map(entry => {
        const segments = entry.fqn.split('.');
        const name = segments.pop();
        return `namespace ${segments.join('.')} { public class ${name} {} }`;
    }).join('\n'));
}
function prefixSource(prefix) {
    const segments = prefix.slice(0, -1).split('.');
    const leaf = segments.at(-1);
    if (!/[+`]/.test(leaf)) return `namespace ${segments.join('.')} { public class spec { [Fact] void should_run() {} } }`;
    segments.pop();
    const classes = leaf.split('+').map(name => name.replace(/`(\d+)$/, (_, arity) => `<${Array.from({ length: Number(arity) }, (_, index) => `T${index}`).join(',')}>`));
    return `namespace ${segments.join('.')} { ${classes.map(name => `public class ${name} {`).join(' ')} [Fact] void should_run() {} ${'}'.repeat(classes.length)} }`;
}
export async function coreSpecFixture(root) {
    await coreExclusionFixture(root);
    const directory = path.join(root, path.dirname(CORE_PROJECT));
    await mkdir(directory, { recursive: true });
    const data = await loadPartition();
    await writeFile(path.join(directory, 'PartitionFixture.cs'), data.units.map(unit => prefixSource(unit.prefix)).join('\n'));
}
