// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vite';
import { EmitMetadataPlugin } from '@cratis/arc.vite';

const here = (relative: string) => fileURLToPath(new URL(relative, import.meta.url));

// Everything the host provides stays out of the bundle, subpaths included (react/jsx-runtime,
// @cratis/components/Common, @cratis/arc.react/queries, ...): the host must keep one instance of each.
const hostProvided = /^(react|react-dom|reflect-metadata|@cratis\/(arc|arc\.react|components|fundamentals))(\/|$)/;

export default defineConfig({
    // Not the Workbench app: no env prefixes, no license key, no public directory.
    envPrefix: [],
    publicDir: false,
    plugins: [EmitMetadataPlugin({ tsconfigPath: here('./tsconfig.json') }) as any],
    build: {
        outDir: 'dist',
        emptyOutDir: true,
        target: 'es2022',
        minify: false,
        sourcemap: false,
        cssCodeSplit: false,
        lib: {
            entry: {
                'index': here('./src/index.ts'),
                'event-stores': here('./src/event-stores/index.ts')
            },
            formats: ['es'],
            cssFileName: 'styles'
        },
        rollupOptions: {
            external: (id: string) => hostProvided.test(id)
        }
    }
});
