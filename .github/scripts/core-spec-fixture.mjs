// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { CORE_PROJECT, coreUnitId, loadPartition } from './core-spec-partition.mjs';

export const expandedProjects = projects => projects.flatMap(project => project === CORE_PROJECT
    ? [1, 2, 3, 4].map(coreUnitId) : [project]);
export async function coreSpecFixture(root) {
    const directory = path.join(root, path.dirname(CORE_PROJECT));
    await mkdir(directory, { recursive: true });
    const data = await loadPartition();
    await writeFile(path.join(directory, 'PartitionFixture.cs'), data.units.map(unit =>
        `namespace ${unit.prefix.slice(0, -1)} { public class spec { [Fact] void should_run() {} } }`).join('\n'));
}
