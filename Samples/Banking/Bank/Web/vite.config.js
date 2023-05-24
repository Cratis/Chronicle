// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { ViteEjsPlugin } from 'vite-plugin-ejs';
import path from 'path';

export default defineConfig({
    build: {
        outDir: './wwwroot',
        assetsDir: ''
    },
    plugins: [
        react(),
        ViteEjsPlugin((viteConfig) => {
            return {
                root: viteConfig.root,
                domain: 'cratis.io',
                title: 'Cratis Workbench'
            };
        })
    ],
    server: {
        port: 9200,
        proxy: {
            '/api': {
                target: 'http://localhost:5200',
                ws: true
            },
            '/swagger': {
                target: 'http://localhost:5200'
            }
        }
    },
    resolve: {
        alias: {
            'API': path.resolve('./API')
        }
    }
});
