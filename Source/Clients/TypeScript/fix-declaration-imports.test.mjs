// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const script = path.join(path.dirname(fileURLToPath(import.meta.url)), 'fix-declaration-imports.mjs');
const fixture = mkdtempSync(path.join(tmpdir(), 'chronicle-declaration-imports-'));

try
{
    for (const format of ['esm', 'cjs'])
    {
        const directory = path.join(fixture, 'dist', format);
        mkdirSync(path.join(directory, 'generated'), { recursive: true });
        writeFileSync(path.join(directory, 'generated', 'index.d.ts'), 'export declare const value: number;\n');
        writeFileSync(path.join(directory, 'types.d.ts'), 'export interface Type { value: number }\n');
        writeFileSync(path.join(directory, 'index.d.ts'), [
            "export * from './generated';",
            "export { value } from './generated/index';",
            "import type { Type } from './types';",
            "export type Lazy = typeof import('./generated');",
            "export type Existing = typeof import('./generated/index.js');",
            ''
        ].join('\n'));
    }

    execFileSync(process.execPath, [script, fixture], { stdio: 'inherit' });
    for (const format of ['esm', 'cjs'])
    {
        const declaration = readFileSync(path.join(fixture, 'dist', format, 'index.d.ts'), 'utf8');
        assert.match(declaration, /export \* from '\.\/generated\/index\.js'/);
        assert.match(declaration, /export \{ value \} from '\.\/generated\/index\.js'/);
        assert.match(declaration, /import type \{ Type \} from '\.\/types\.js'/);
        assert.match(declaration, /import\('\.\/generated\/index\.js'\)/);
        assert.match(declaration, /Existing = typeof import\('\.\/generated\/index\.js'\)/);
    }
    console.log('Declaration import fixtures passed for ESM and CJS');
}
finally
{
    rmSync(fixture, { recursive: true, force: true });
}
