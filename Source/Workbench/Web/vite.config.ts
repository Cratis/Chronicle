// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vite';
import react from "@vitejs/plugin-react";
import path from 'path';
import { EmitMetadataPlugin } from '@cratis/applications.vite';

export default defineConfig({
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
    plugins: [
        react(),
        EmitMetadataPlugin() as any
    ],
    server: {
        port: 9000,
        open: false,
        proxy: {
            '/api': {
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
