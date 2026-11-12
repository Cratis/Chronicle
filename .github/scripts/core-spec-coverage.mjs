// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { discoverTypes } from './core-spec-source.mjs';

const fail = message => { throw new Error(`Core type coverage: ${message}`); };
export const exclusionPath = '.github/scripts/core-spec-exclusions.json';
export async function loadExclusions(root) {
    const entries = JSON.parse(await readFile(path.join(root, exclusionPath), 'utf8'));
    if (!Array.isArray(entries)) fail('Expected explicit helper exclusion list');
    return entries;
}
function referencedNames(info) {
    // Conservative by-name closure, independent of inheritance resolution/generic parsing.
    return [...info.baseNames, ...info.aliasNames];
}
function relatedTypes(types, excluded) {
    const related = new Set(excluded);
    let previous = -1;
    while (related.size !== previous) {
        previous = related.size;
        const connected = types.filter(info => related.has(info.fqn));
        const names = new Set(connected.map(info => info.name.split('.').at(-1)));
        const bases = new Set(connected.flatMap(referencedNames));
        for (const info of types) {
            if (bases.has(info.name.split('.').at(-1)) || referencedNames(info).some(name => names.has(name))) related.add(info.fqn);
        }
    }
    return types.filter(info => related.has(info.fqn));
}
export function validateExclusions(types, entries) {
    const excluded = new Set();
    for (const entry of entries) {
        if (typeof entry.fqn !== 'string' || !entry.reason?.trim()) fail('Invalid helper exclusion entry');
        if (excluded.has(entry.fqn)) fail(`Duplicate helper exclusion ${entry.fqn}`);
        if (!types.some(info => info.fqn === entry.fqn)) fail(`Stale helper exclusion ${entry.fqn}`);
        excluded.add(entry.fqn);
    }
    const unsafe = relatedTypes(types, excluded).find(info => info.testSignals || referencedNames(info).some(name => /Specification|Scenario/.test(name)));
    if (unsafe) fail(`Excluded helper, source base or by-name derivative ${unsafe.fqn} contains test signals or a Specification-style base`);
    return excluded;
}
export async function coverageTypes(root) {
    const types = await discoverTypes(root);
    const excluded = validateExclusions(types, await loadExclusions(root));
    const covered = [...new Map(types.filter(info => !excluded.has(info.fqn)).map(info => [info.fqn, info])).values()];
    if (!covered.length) fail('No non-excluded Core type declarations');
    return covered;
}
