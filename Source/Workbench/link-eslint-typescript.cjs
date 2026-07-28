#!/usr/bin/env node
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @typescript-eslint's parser (and several of its own dependencies, e.g. ts-api-utils)
// statically read classic TypeScript compiler API surface (ts.Extension, ts.sys,
// ts.SyntaxKind, ...) that TypeScript 7's native package no longer exposes, so linting
// crashes as soon as any of them is required - regardless of configuration, since this
// happens at module load time. Yarn's peer-dependency resolution won't give these
// packages their own pinned TypeScript copy: peerDependencies are satisfied by
// whatever's hoisted at the workspace root (mismatched semver range or not), and
// neither `resolutions` nor `packageExtensions` can override an already-declared peer
// dependency to force a distinct install.
//
// Give them one directly: walk @typescript-eslint's real (Node-resolved) dependency
// tree, find every package in it that itself depends on `typescript`, and copy the
// `typescript-for-eslint` alias (a real TypeScript 6.0.3 install, within
// @typescript-eslint's supported peer range) into each one's own node_modules, where
// Node's module resolution finds it ahead of the hoisted TypeScript 7.

const fs = require('fs');
const path = require('path');

const ESLINT_TYPESCRIPT_ROOTS = [
    '@typescript-eslint/eslint-plugin',
    '@typescript-eslint/parser',
    '@typescript-eslint/type-utils',
    '@typescript-eslint/typescript-estree',
    '@typescript-eslint/utils',
    '@typescript-eslint/project-service',
    'typescript-eslint',
];

const shimSource = path.join(__dirname, 'node_modules', 'typescript-for-eslint');
if (!fs.existsSync(shimSource)) {
    process.exit(0);
}

// The whole point of the alias is to be a TypeScript the linter supports. If something bumps it to
// 7.x (npm-check-updates has done exactly that), copying it around still "succeeds" and the failure
// only surfaces much later as an opaque "typescript-eslint does not support TS 7.0" at lint time.
// Fail here instead, where the cause is obvious.
const shimVersion = JSON.parse(fs.readFileSync(path.join(shimSource, 'package.json'), 'utf-8')).version;
if (Number(shimVersion.split('.')[0]) >= 7) {
    console.error(
        `link-eslint-typescript: typescript-for-eslint resolves to TypeScript ${shimVersion}, but it must ` +
        'stay on a version typescript-eslint supports (<7). Pin it back in package.json.');
    process.exit(1);
}

function resolvePackageDir(name, fromDir) {
    let dir = fromDir;
    for (;;) {
        const candidate = path.join(dir, 'node_modules', name);
        if (fs.existsSync(path.join(candidate, 'package.json'))) return candidate;

        const parent = path.dirname(dir);
        if (parent === dir) return null;
        dir = parent;
    }
}

const visited = new Set();
const typescriptConsumers = new Set();

function visit(name, fromDir) {
    const pkgDir = resolvePackageDir(name, fromDir);
    if (!pkgDir || visited.has(pkgDir)) return;
    visited.add(pkgDir);

    const pkg = JSON.parse(fs.readFileSync(path.join(pkgDir, 'package.json'), 'utf-8'));
    const dependencyNames = Object.keys(pkg.dependencies || {});

    if (dependencyNames.includes('typescript') || (pkg.peerDependencies || {}).typescript) {
        typescriptConsumers.add(pkgDir);
    }

    for (const dependencyName of dependencyNames) {
        if (dependencyName === 'typescript') continue;
        visit(dependencyName, pkgDir);
    }
}

for (const root of ESLINT_TYPESCRIPT_ROOTS) {
    visit(root, __dirname);
}

for (const consumerDir of typescriptConsumers) {
    const target = path.join(consumerDir, 'node_modules', 'typescript');
    fs.rmSync(target, { recursive: true, force: true });
    fs.mkdirSync(path.dirname(target), { recursive: true });
    fs.cpSync(shimSource, target, { recursive: true });
}
