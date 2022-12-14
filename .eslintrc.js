// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**@type {import('eslint').Linter.Config} */
// eslint-disable-next-line no-undef
module.exports = {
    root: true,
    parser: '@typescript-eslint/parser',
    ignorePatterns: [
        '*.d.ts',
        '*.scss.d.ts',
        'tsconfig.*',
        'wallaby.js',
        'dist',
        'node_modules',
        'wwwroot',
        'templates'
    ],
    plugins: [
        '@typescript-eslint',
        'header',
        'no-null',
        'import'
    ],
    extends: [
        'eslint:recommended',
        'plugin:@typescript-eslint/recommended',
        'plugin:react/recommended'
    ],
    settings: {
        react: {
            version: 'detect',
        },
    },
    rules: {
        'no-irregular-whitespace': 0,
        'semi': [2, "always"],
        'react/display-name': 0,
        'react/react-in-jsx-scope': 0,
        'no-prototype-builtins': 0,
        '@typescript-eslint/no-unused-vars': 0,
        '@typescript-eslint/no-explicit-any': 0,
        '@typescript-eslint/explicit-module-boundary-types': 0,
        '@typescript-eslint/no-non-null-assertion': 0,
        '@typescript-eslint/no-empty-function': 0,
        '@typescript-eslint/ban-types': 0,
        '@typescript-eslint/no-var-requires': 0,
        // eslint-plugin-header
        'header/header': [
            2,
            'line',
            [
                ' Copyright (c) Aksio Insurtech. All rights reserved.',
                ' Licensed under the MIT license. See LICENSE file in the project root for full license information.'
            ]
        ]
    },
    overrides: [
        {
            files: ['**/for_*/**'],
            rules: {
                '@typescript-eslint/naming-convention': 0,
                '@typescript-eslint/no-unused-expressions': 0,
                'no-restricted-globals': 0
            }
        }
    ]
};
