/// <reference types="vitest/config" />

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';
import react from "@vitejs/plugin-react";
import { fileURLToPath } from 'node:url';
import { EmitMetadataPlugin } from '@cratis/arc.vite';
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    root: fileURLToPath(new URL('./', import.meta.url)),
    envPrefix: 'CHRONICLE_',
    optimizeDeps: {
        exclude: ['tslib'],
    },
    esbuild: {
        supported: {
            'top-level-await': true,
        },
    },
    build: {
        outDir: '../wwwroot',
        assetsDir: '',
        modulePreload: false,
        target: 'esnext',
        minify: false,
        cssCodeSplit: false,
        rollupOptions: {
            external: [
            ],
        },
    },
    test: {
        globals: true,
        environment: 'node',
        isolate: false,
        fileParallelism: false,
        pool: 'threads',
        coverage: {
            provider: 'v8',
            exclude: [
                '**/for_*/**',
                '**/wwwroot/**',
                '**/api/**',
                '**/Api/**',
                '**/dist/**',
                '**/*.test.tsx',
                '**/*.d.ts',
                '**/declarations.ts',
            ],
        },
        exclude: ['../dist/**', '../node_modules/**', 'node_modules/**', '../wwwroot/**', 'wwwroot/**', '../**/given/**'],
        include: ['../**/for_*/when_*/**/*.ts', '../**/for_*/**/when_*.ts'],
        setupFiles: fileURLToPath(new URL('./vitest.setup.ts', import.meta.url))
    },
    plugins: [
        react(),
        tailwindcss(),
        EmitMetadataPlugin({ tsconfigPath: fileURLToPath(new URL('./tsconfig.json', import.meta.url)) }) as any
    ],
    server: {
        port: 9000,
        open: false,
        proxy: {
            '/api': {
                target: 'http://localhost:8080',
                ws: true
            },
            '/identity': {
                target: 'http://localhost:8080',
                ws: true
            },
            '/swagger': {
                target: 'http://localhost:8080'
            }
        }
    },
    resolve: {
        alias: {
            'Api':        fileURLToPath(new URL('../Api', import.meta.url)),
            'assets':     fileURLToPath(new URL('../assets', import.meta.url)),
            'Shared':     fileURLToPath(new URL('../Shared', import.meta.url)),
            'State':      fileURLToPath(new URL('../State', import.meta.url)),
            'Components': fileURLToPath(new URL('../Components', import.meta.url)),
            'Icons':      fileURLToPath(new URL('../Icons', import.meta.url)),
            'Browser':    fileURLToPath(new URL('../Infrastructure/Browser', import.meta.url)),
            'Layout':     fileURLToPath(new URL('../Layout', import.meta.url)),
            'Features':   fileURLToPath(new URL('../Features', import.meta.url)),
            'Strings':    fileURLToPath(new URL('../Strings.ts', import.meta.url)),
            'Utilities':  fileURLToPath(new URL('../Utilities', import.meta.url)),
            'given':      fileURLToPath(new URL('../given.ts', import.meta.url)),
        }
    }
});
