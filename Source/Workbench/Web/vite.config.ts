/// <reference types="vitest/config" />

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';
import react from "@vitejs/plugin-react";
import path from 'path';
import { EmitMetadataPlugin } from '@cratis/applications.vite';

export default defineConfig({
    optimizeDeps: {
        exclude: ['tslib'],
    },
    esbuild: {
        supported: {
            'top-level-await': true,
        },
    },
    build: {
        outDir: './wwwroot',
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
        poolOptions: {
            forks: {
                isolate: false,
            },
        },
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
        exclude: ['**/dist/**', '**/node_modules/**', 'node_modules/**', '**/wwwroot/**', 'wwwroot/**', '**/given/**'],
        include: ['**/for_*/when_*/**/*.ts', '**/for_*/**/when_*.ts'],
        setupFiles: `${__dirname}/vitest.setup.ts`
    },
    plugins: [
        react(),
        EmitMetadataPlugin() as any
    ],
    server: {
        port: 9000,
        open: false,
        proxy: {
            '/api': {
                target: 'http://localhost:8082',
                ws: true
            },
            '/swagger': {
                target: 'http://localhost:8082'
            }
        }
    },
    resolve: {
        alias: {
            'Api': path.resolve('./Api'),
            'assets': path.resolve('./assets'),
            'Shared': path.resolve('./Shared'),
            'State': path.resolve('./State'),
            'Components': path.resolve('./Components'),
            'Browser': path.resolve('./Infrastructure/Browser'),
            'Layout': path.resolve('./Layout'),
            'Features': path.resolve('./Features'),
            'Strings': path.resolve('./Strings'),
        }
    }
});
