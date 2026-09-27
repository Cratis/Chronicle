// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { promises as fs } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Rollup emits working .js specifiers, but its declarations retain the extensionless imports
// from ts-proto. NodeNext treats these declarations as ESM because the package has type: module.
const root = path.dirname(fileURLToPath(import.meta.url));
const declarationRoots = ['dist/esm', 'dist/cjs'];
let checked = 0;
let rewritten = 0;

const rewriteDirectory = async directory =>
{
    for (const entry of await fs.readdir(directory, { withFileTypes: true }))
    {
        const file = path.join(directory, entry.name);
        if (entry.isDirectory())
        {
            await rewriteDirectory(file);
        }
        else if (entry.name.endsWith('.d.ts'))
        {
            checked++;
            const source = await fs.readFile(file, 'utf8');
            let changed = false;
            const result = source.replace(/((?:\bfrom\s*|\bimport\s*\(\s*|\bimport\s*)['"])(\.{1,2}\/[^'"]+)(['"])/g, (match, prefix, specifier, suffix) =>
            {
                if (path.extname(specifier)) return match;
                changed = true;
                return `${prefix}${specifier}.js${suffix}`;
            });
            if (changed)
            {
                await fs.writeFile(file, result);
                rewritten++;
            }
        }
    }
};

for (const directory of declarationRoots)
{
    await rewriteDirectory(path.join(root, directory));
}

if (!checked || !rewritten)
{
    throw new Error(`Expected extensionless declarations: checked ${checked} files, rewrote ${rewritten}`);
}
console.log(`Checked ${checked} declarations; rewrote relative imports in ${rewritten}`);
