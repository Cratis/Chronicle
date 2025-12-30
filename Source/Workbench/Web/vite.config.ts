/// <reference types="vitest/config" />

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';
import react from "@vitejs/plugin-react";
import path from 'path';
import { EmitMetadataPlugin } from '@cratis/arc.vite';
import tailwindcss from "@tailwindcss/vite";

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
        tailwindcss(),
        EmitMetadataPlugin() as any
    ],
    server: {
        port: 9000,
        open: false,
        proxy: {
            '/api': {
                target: 'https://localhost:8080',
                ws: true,
                secure: false,
                changeOrigin: true
            },
            '/identity': {
                target: 'https://localhost:8080',
                ws: true,
                secure: false,
                changeOrigin: true
            },
            '/swagger': {
                target: 'https://localhost:8080',
                secure: false,
                changeOrigin: true
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
            'given': path.resolve('./given.ts'),
        }
    }
});
