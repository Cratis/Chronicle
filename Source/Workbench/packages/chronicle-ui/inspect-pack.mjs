// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// What the published tarball must contain and must never contain.

export const requiredFiles = [
    'package.json',
    'README.md',
    'LICENSE',
    'dist/index.js',
    'dist/event-stores.js',
    'dist/styles.css',
    'dist/types/src/index.d.ts',
    'dist/types/src/event-stores/index.d.ts'
];

const forbidden = [
    [/(^|\/)\.env/, 'environment file'],
    [/(^|\/)wwwroot\//, 'application output'],
    [/\.map$/, 'source map'],
    [/(^|\/)generated-input\/.*(?<!\.d\.ts)$/, 'staged generated source'],
    [/(^|\/)(for_[^/]+|specs|stories)\//, 'spec or story'],
    [/monaco|worker|chart|echarts|pixi/i, 'editor, worker or chart code'],
    [/\.tsx?$/i, 'TypeScript source'],
    [/license.?key/i, 'license key']
];

const isDeclaration = file => file.endsWith('.d.ts');

/**
 * Returns the problems with a packed file list; an empty list means the artifact is publishable.
 */
export const inspectPackedFiles = files => {
    const problems = [];
    for (const required of requiredFiles) {
        if (!files.includes(required)) {
            problems.push(`missing ${required}`);
        }
    }
    for (const file of files) {
        for (const [pattern, reason] of forbidden) {
            if (pattern.test(file) && !(reason === 'TypeScript source' && isDeclaration(file))) {
                problems.push(`${file}: ${reason}`);
            }
        }
        if (!/^(package\.json|README\.md|LICENSE|dist\/.+)$/.test(file)) {
            problems.push(`${file}: outside the files allowlist`);
        }
    }
    return problems;
};
