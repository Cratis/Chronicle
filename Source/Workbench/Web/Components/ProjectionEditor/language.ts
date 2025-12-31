// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';

export const languageId = 'projection-dsl';

export const configuration: languages.LanguageConfiguration = {
    comments: {
        lineComment: '#',
    },
    brackets: [
        ['[', ']'],
    ],
    autoClosingPairs: [
        { open: '[', close: ']' },
        { open: '"', close: '"' },
        { open: "'", close: "'" },
    ],
    surroundingPairs: [
        { open: '[', close: ']' },
        { open: '"', close: '"' },
        { open: "'", close: "'" },
    ],
};

export const monarchLanguage: languages.IMonarchLanguage = {
    defaultToken: '',
    tokenPostfix: '.projection-dsl',

    keywords: [
        'key',
        'increment',
        'decrement',
        'count',
        'by',
        'on',
        'join',
        'identifier',
        'removedWith',
    ],

    operators: ['=', '+', '-', '|', '.', ':', ','],

    symbols: /[=><!~?:&|+\-*\/\^%]+/,

    escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

    tokenizer: {
        root: [
            // Comments
            [/#.*$/, 'comment'],

            // Keywords
            [
                /[a-zA-Z_$][\w$]*/,
                {
                    cases: {
                        '@keywords': 'keyword',
                        '@default': 'identifier',
                    },
                },
            ],

            // Whitespace
            { include: '@whitespace' },

            // Delimiters and operators
            [/[[\]]/, '@brackets'],
            [/[(),.]/, 'delimiter'],
            [
                /@symbols/,
                {
                    cases: {
                        '@operators': 'operator',
                        '@default': '',
                    },
                },
            ],

            // Numbers
            [/\d*\.\d+([eE][-+]?\d+)?/, 'number.float'],
            [/\d+/, 'number'],

            // Strings
            [/"([^"\\]|\\.)*$/, 'string.invalid'], // non-terminated string
            [/'([^'\\]|\\.)*$/, 'string.invalid'], // non-terminated string
            [/"/, 'string', '@string_double'],
            [/'/, 'string', '@string_single'],
        ],

        whitespace: [[/[ \t\r\n]+/, '']],

        string_double: [
            [/[^\\"]+/, 'string'],
            [/@escapes/, 'string.escape'],
            [/\\./, 'string.escape.invalid'],
            [/"/, 'string', '@pop'],
        ],

        string_single: [
            [/[^\\']+/, 'string'],
            [/@escapes/, 'string.escape'],
            [/\\./, 'string.escape.invalid'],
            [/'/, 'string', '@pop'],
        ],
    },
};
