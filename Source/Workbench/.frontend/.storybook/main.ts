// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { StorybookConfig } from '@storybook/react-vite';
import type { UserConfig as ViteConfig } from 'vite';
import path from 'path';
import { fileURLToPath } from 'url';
import tailwindcss from '@tailwindcss/vite';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const config: StorybookConfig = {
    stories: [
        '../../Components/**/*.stories.@(js|jsx|ts|tsx)',
        '../../Features/**/*.stories.@(js|jsx|ts|tsx)',
        '../../*.stories.@(js|jsx|ts|tsx)',
    ],
    addons: [],
    framework: {
        name: '@storybook/react-vite',
        options: {}
    },
    core: { builder: '@storybook/builder-vite' },
    async viteFinal(existingConfig: ViteConfig) {
        const cfg: ViteConfig = { ...existingConfig };
        delete cfg.root;
        cfg.server = { ...(cfg.server || {}), open: false } as unknown;
        cfg.plugins = [...(cfg.plugins as [] || []), tailwindcss()];

        const root = path.resolve(__dirname, '../..');
        const newAliases = [
            { find: 'Api',        replacement: path.join(root, 'Api') },
            { find: 'Shared',     replacement: path.join(root, 'Shared') },
            { find: 'State',      replacement: path.join(root, 'State') },
            { find: 'Components', replacement: path.join(root, 'Components') },
            { find: 'Icons',      replacement: path.join(root, 'Icons') },
            { find: 'Browser',    replacement: path.join(root, 'Infrastructure/Browser') },
            { find: 'Layout',     replacement: path.join(root, 'Layout') },
            { find: 'Features',   replacement: path.join(root, 'Features') },
            { find: 'Strings',    replacement: path.join(root, 'Strings.ts') },
            { find: 'Utilities',  replacement: path.join(root, 'Utilities') },
            { find: 'given',      replacement: path.join(root, 'given.ts') },
        ];

        const existingAlias = cfg.resolve?.alias;
        const existingAliasArray = Array.isArray(existingAlias)
            ? existingAlias
            : Object.entries(existingAlias || {}).map(([find, replacement]) => ({ find, replacement }));

        cfg.resolve = {
            ...(cfg.resolve || {}),
            alias: [...existingAliasArray, ...newAliases],
        };
        return cfg;
    }
};

export default config;
