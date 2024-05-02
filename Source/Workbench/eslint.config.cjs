// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**@type {import('eslint').Linter.Config} */

/* eslint-disable no-undef, @typescript-eslint/no-var-requires */

const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const vue3eslint = require('eslint-plugin-vue');
const typeScriptEslint = require('@typescript-eslint/eslint-plugin');
const noNull = require('eslint-plugin-no-null');
const importPlugin = require('eslint-plugin-import');
const globals = require('globals');
const headers = require('eslint-plugin-headers');
const vueEslintParser = require('vue-eslint-parser');

module.exports = [
    eslint.configs.recommended,
    ...tseslint.configs.recommended,
    ...vue3eslint.configs['flat/recommended'],

    {
        files: ['**/*.ts'],
        plugins: {
            headers
        },
        rules: {
            'headers/header-format': [
                'error',
                {
                    source: 'file',
                    style: 'line',
                    path: './licenseHeader',
                }
            ]
        }
    },
    {
        files: ['**/*.vue'],
        languageOptions: {
            parser: vueEslintParser,
            parserOptions: {
                parser: typeScriptEslint.parser
            }
        }
    },

    {
        files: ['**/*.ts'],
        languageOptions: {
            ecmaVersion: 2022,
            sourceType: 'module',
            globals: {
                ...globals.browser
            }
        },
        ignores: [
            '*.d.ts',
            '*.scss.d.ts',
            'tsconfig.*',
            'wallaby.js',
            'dist',
            'node_modules',
            'wwwroot'
        ],
        plugins: {
            typeScriptEslint,
            noNull,
            importPlugin
        },
        rules: {
            'no-irregular-whitespace': 0,
            'semi': [2, "always"],
            'no-prototype-builtins': 0,
            '@typescript-eslint/no-unused-vars': 0,
            '@typescript-eslint/no-explicit-any': 0,
            '@typescript-eslint/explicit-module-boundary-types': 0,
            '@typescript-eslint/no-non-null-assertion': 0,
            '@typescript-eslint/no-empty-function': 0,
            '@typescript-eslint/ban-types': 0,
            '@typescript-eslint/no-var-requires': 0,
        }
    }
];
