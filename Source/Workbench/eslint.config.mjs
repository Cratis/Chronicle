import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { FlatCompat } from '@eslint/eslintrc';
import js from '@eslint/js';
import eslint from '@eslint/js';
import typescriptEslint from '@typescript-eslint/eslint-plugin';
import tsParser from '@typescript-eslint/parser';
import header from '@tony.ganchev/eslint-plugin-header';
import noNull from 'eslint-plugin-no-null';
import globals from 'globals';
import tseslint from 'typescript-eslint';
import reactlint from 'eslint-plugin-react';

header.rules.header.meta.schema = false;

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const compat = new FlatCompat({
    baseDirectory: __dirname,
    recommendedConfig: js.configs.recommended,
    allConfig: js.configs.all,
});

const getRules = configArray => {
    let rules = {};

    const addRulesFromObject = config => {
        if (config.hasOwnProperty('rules')) {
            rules = {
                ...rules,
                ...config.rules,
            };
        }
    };

    if (Array.isArray(configArray)) {
        for (const config of configArray) {
            addRulesFromObject(config);
        }
    } else {
        addRulesFromObject(configArray);
    }

    return rules;
};

const rules = {
    ...getRules(eslint.configs.recommended),
    ...getRules(tseslint.configs.recommended),
    ...getRules(reactlint.configs.recommended),
    ...{
        'no-irregular-whitespace': 0,
        semi: [2, 'always'],
        'react/display-name': 0,
        'react/react-in-jsx-scope': 0,
        'no-prototype-builtins': 0,

        '@typescript-eslint/no-unused-vars': [
            'error',
            {
                ignoreRestSiblings: true,
            },
        ],

        '@typescript-eslint/no-explicit-any': 'error',
        '@typescript-eslint/explicit-module-boundary-types': 0,
        '@typescript-eslint/no-non-null-assertion': 0,
        '@typescript-eslint/no-empty-function': 'error',
        '@typescript-eslint/no-var-requires': 'error',
        '@typescript-eslint/ban-ts-comment': 0,
        "@typescript-eslint/no-empty-interface": 0,

        '@tony.ganchev/header': [
            2,
            'line',
            [
                ' Copyright (c) Cratis. All rights reserved.',
                ' Licensed under the MIT license. See LICENSE file in the project root for full license information.'
            ],
            1
        ],
    },
};

const defaultConfig = [
    {
        ignores: [
            '**/*.d.ts',
            '**/*.scss.d.ts',
            '**/tsconfig.*',
            '**/wallaby.js',
            '**/*.js',
            '**/dist/**',
            '**/node_modules/**',
            '**/wwwroot/**',
            '**/templates/**',
            '**/Api/**',
            '**/rollup.config.mjs',
            '**/vite.config.ts'
        ],
    },
    {
        files: ['**/*.ts', '**/*.tsx'],

        plugins: {
            '@typescript-eslint': typescriptEslint,
            react: reactlint,
            '@tony.ganchev': header,
            'no-null': noNull
        },

        rules: rules,

        languageOptions: {
            globals: {
                ...globals.browser,
            },
            parser: tsParser,
            sourceType: 'module',
        },

        settings: {
            react: {
                version: '19.2',
            },
        },
    },
    {
        files: ['**/for_*/**/*.ts'],
        rules: {
            '@typescript-eslint/naming-convention': 0,
            '@typescript-eslint/no-unused-expressions': 0,
            "@typescript-eslint/no-empty-function": "off",
            'no-restricted-globals': 0,
        },
    },
];

const config = tseslint.config(...defaultConfig);
export default config;
