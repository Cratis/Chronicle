// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { languages } from 'monaco-editor';

export const languageId = 'projection-dsl';

// Keywords for the Chronicle Projection DSL
const KEYWORDS = [
    'projection',
    'every',
    'on',
    'from',
    'key',
    'parent',
    'join',
    'automap',
    'no',
    'children',
    'add',
    'subtract',
    'remove',
    'via',
    'with',
    'default',
    'increment',
    'decrement',
    'by',
    'count',
    'set',
    'unset',
    'events',
    'id',
    'exclude',
] as const;

// Built-in expressions and functions
const BUILTINS = [
    '$eventSourceId',
    '$causedBy',
    '$occurred',
    '$namespace',
    '$eventContext',
] as const;

// Operators
const OPERATORS = ['=', '+=', '-=', '=>', '.'];

export const configuration: languages.LanguageConfiguration = {
    comments: {
        lineComment: '#',
    },
    brackets: [
        ['{', '}'],
        ['[', ']'],
        ['(', ')'],
    ],
    autoClosingPairs: [
        { open: '{', close: '}' },
        { open: '[', close: ']' },
        { open: '(', close: ')' },
        { open: '"', close: '"' },
        { open: "'", close: "'" },
        { open: '`', close: '`' },
    ],
    surroundingPairs: [
        { open: '{', close: '}' },
        { open: '[', close: ']' },
        { open: '(', close: ')' },
        { open: '"', close: '"' },
        { open: "'", close: "'" },
        { open: '`', close: '`' },
    ],
    indentationRules: {
        increaseIndentPattern: /^.*(:|\bon\b|\bevery\b|\bchildren\b|\bparent\b)\s*$/,
        decreaseIndentPattern: /^.*\}.*$/,
    },
    folding: {
        offSide: true,
        markers: {
            start: /^\s*(projection|every|on|children|parent)/,
            end: /^\s*$/,
        },
    },
};

export const monarchLanguage: languages.IMonarchLanguage = {
    defaultToken: '',
    tokenPostfix: '.chronicle-rules-dsl',

    keywords: KEYWORDS,
    builtins: BUILTINS,
    operators: OPERATORS,

    // Symbols
    symbols: /[=><!~?:&|+\-*/^%]+/,
    escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

    tokenizer: {
        root: [
            // Whitespace
            { include: '@whitespace' },

            // Template strings with ${...} interpolation
            [/`/, { token: 'string.template', next: '@templateString' }],

            // Identifiers and keywords
            [
                /[a-zA-Z_$][\w$]*/,
                {
                    cases: {
                        '@keywords': 'keyword',
                        '@builtins': 'variable.predefined',
                        '@default': 'identifier',
                    },
                },
            ],

            // Delimiters and operators
            [/[{}()[\]]/, '@brackets'],
            [/=>/, 'operator.arrow'],
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
            [/0[xX][0-9a-fA-F]+/, 'number.hex'],
            [/\d+/, 'number'],

            // Delimiter: after number because of .\d floats
            [/[;,.]/, 'delimiter'],

            // Strings
            [/"([^"\\]|\\.)*$/, 'string.invalid'],
            [/"/, { token: 'string.quote', next: '@string' }],
        ],

        whitespace: [
            [/[ \t\r\n]+/, ''],
            [/#.*$/, 'comment'],
        ],

        // Template string state
        templateString: [
            [/\$\{/, { token: 'delimiter.bracket', next: '@templateExpression' }],
            [/[^\\`$]+/, 'string.template'],
            [/@escapes/, 'string.template.escape'],
            [/\\./, 'string.template.escape.invalid'],
            [/`/, { token: 'string.template', next: '@pop' }],
        ],

        // Template expression inside ${}
        templateExpression: [
            [/\}/, { token: 'delimiter.bracket', next: '@pop' }],
            { include: '@root' },
        ],

        // Regular string state
        string: [
            [/[^\\"]+/, 'string'],
            [/@escapes/, 'string.escape'],
            [/\\./, 'string.escape.invalid'],
            [/"/, { token: 'string.quote', next: '@pop' }],
        ],
    },
};
