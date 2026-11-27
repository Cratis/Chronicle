// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Consumes the @cratis/chronicle-ui tarball from a project outside this checkout, with the host's real peers
// installed - no workspace link, alias or NODE_PATH. With a .tgz argument it checks exactly that file (the
// release verifies the artifact it then publishes); with a directory, or none, it packs the built package first.

import { execFileSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { inspectPackedFiles } from './inspect-pack.mjs';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
// The directory holding the built package to pack: this package by default, or a staged copy (the release).
const argument = process.argv[2] ? path.resolve(process.argv[2]) : undefined;
const tarballMode = argument?.endsWith('.tgz') === true;
const packageDirectory = tarballMode ? scriptDirectory : (argument ?? scriptDirectory);
const temporaryDirectory = mkdtempSync(path.join(tmpdir(), 'chronicle-ui-consumer-'));
const run = (command, args, options = {}) => execFileSync(command, args, { stdio: 'inherit', ...options });

const readTarball = tarball => ({
    tarball,
    files: execFileSync('tar', ['-tzf', tarball]).toString().split('\n').filter(Boolean).map(file => file.replace(/^package\//, '')),
    manifest: JSON.parse(execFileSync('tar', ['-xOzf', tarball, 'package/package.json']).toString())
});

const packDirectory = directory => {
    for (const required of ['dist/index.js', 'dist/event-stores.js', 'dist/styles.css']) {
        if (!existsSync(path.join(directory, required))) {
            throw new Error(`Build the package first (yarn build:ui): ${required} is missing`);
        }
    }

    // Stage only what is publishable, without lifecycle scripts, and pack that.
    const staged = path.join(temporaryDirectory, 'package');
    mkdirSync(staged);
    for (const entry of ['dist', 'README.md', 'LICENSE']) {
        cpSync(path.join(directory, entry), path.join(staged, entry), { recursive: true });
    }
    const source = JSON.parse(readFileSync(path.join(directory, 'package.json'), 'utf8'));
    const { scripts: _scripts, ...publishable } = source;
    writeFileSync(path.join(staged, 'package.json'), JSON.stringify(publishable, undefined, 2));

    const packEnvironment = Object.fromEntries(Object.entries(process.env).filter(([key]) => !/^npm_|^yarn_|^INIT_CWD$/i.test(key)));
    const packed = path.join(temporaryDirectory, 'packed');
    mkdirSync(packed);
    const packOutput = execFileSync('npm', ['pack', '--ignore-scripts', '--json', '--pack-destination', packed], {
        cwd: staged, env: packEnvironment, stdio: ['ignore', 'pipe', 'inherit']
    }).toString();
    const packResult = JSON.parse(packOutput);
    const entry = Array.isArray(packResult) ? packResult[0] : Object.values(packResult)[0];
    return {
        tarball: path.join(packed, readdirSync(packed).find(file => file.endsWith('.tgz'))),
        files: entry.files.map(file => file.path),
        manifest: source
    };
};

try {
    const { tarball, files, manifest } = tarballMode ? readTarball(argument) : packDirectory(packageDirectory);
    const { devDependencies } = manifest;
    const problems = inspectPackedFiles(files);
    if (problems.length > 0) {
        throw new Error(`The packed artifact is not publishable:\n  ${problems.join('\n  ')}`);
    }

    const consumer = path.join(temporaryDirectory, 'consumer');
    mkdirSync(consumer);
    writeFileSync(path.join(consumer, 'package.json'), JSON.stringify({ private: true, type: 'module' }));

    const peers = Object.entries(manifest.peerDependencies).map(([name, range]) => `${name}@${range}`);
    const tools = ['typescript', 'vite', '@types/react', '@types/react-dom'].map(name => `${name}@${devDependencies[name]}`);
    run('npm', ['install', '--ignore-scripts', '--no-audit', '--no-fund', '--no-package-lock', '--prefer-offline', tarball, ...peers, ...tools], { cwd: consumer });

    const installed = path.join(consumer, 'node_modules', '@cratis', 'chronicle-ui');
    if (existsSync(path.join(installed, 'node_modules'))) {
        throw new Error('The package installed its own copy of a dependency; peers must resolve to the host.');
    }

    writeFileSync(path.join(consumer, 'env.d.ts'), '/// <reference types="vite/client" />\n');
    writeFileSync(path.join(consumer, 'entry.tsx'), `
import 'reflect-metadata';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { Card, type CardProps } from '@cratis/chronicle-ui';
import { useEventStoreNames } from '@cratis/chronicle-ui/event-stores';
import '@cratis/chronicle-ui/styles.css';

const props: CardProps = { header: 'header' };
const names: () => string[] = useEventStoreNames;
export const render = () => renderToStaticMarkup(createElement(Card, props, 'body'));
export { names };
`);
    for (const [module, moduleResolution] of [['ESNext', 'Bundler'], ['NodeNext', 'NodeNext']]) {
        writeFileSync(path.join(consumer, `tsconfig.${moduleResolution}.json`), JSON.stringify({
            compilerOptions: {
                target: 'ES2022', module, moduleResolution, jsx: 'react-jsx', strict: true,
                skipLibCheck: false, noEmit: true, types: [], lib: ['ES2022', 'DOM'], experimentalDecorators: true
            },
            files: ['env.d.ts', 'entry.tsx']
        }));
        console.log(`Type-checking the packed package with ${moduleResolution} resolution and skipLibCheck: false`);
        run(process.execPath, [path.join(consumer, 'node_modules/typescript/bin/tsc'), '-p', `tsconfig.${moduleResolution}.json`], { cwd: consumer });
    }

    writeFileSync(path.join(consumer, 'vite.config.mjs'), `
export default { logLevel: 'warn', build: { outDir: 'out', minify: false, lib: { entry: 'entry.tsx', formats: ['es'], fileName: 'entry' } } };
`);
    console.log('Production-building a host entry from the packed package');
    run(process.execPath, [path.join(consumer, 'node_modules/vite/bin/vite.js'), 'build'], { cwd: consumer });
    const outFiles = readdirSync(path.join(consumer, 'out'));
    const css = outFiles.find(file => file.endsWith('.css'));
    const js = outFiles.find(file => file.endsWith('.js') || file.endsWith('.mjs'));
    if (!css || !js || !readFileSync(path.join(consumer, 'out', css), 'utf8').includes('.workbench-card__body')) {
        throw new Error('The host build lacks the card stylesheet or script');
    }

    // The packed dist, resolved by the host's own node_modules: the generated type keeps its field metadata
    // and the card renders inside the host's Components.
    writeFileSync(path.join(consumer, 'runtime.mjs'), `
import 'reflect-metadata';
import assert from 'node:assert/strict';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { Card } from '@cratis/chronicle-ui';
import { useEventStoreNames } from '@cratis/chronicle-ui/event-stores';

assert.equal(typeof useEventStoreNames, 'function');
const markup = renderToStaticMarkup(createElement(Card, { header: 'header', footer: 'footer' }, 'body'));
assert.match(markup, /workbench-card__header[^>]*>header</);
assert.match(markup, /workbench-card__body[^>]*>body</);
assert.match(markup, /workbench-card__footer[^>]*>footer</);
`);
    console.log('Running the packed package against the host peers');
    run(process.execPath, ['runtime.mjs'], { cwd: consumer });
}
finally {
    rmSync(temporaryDirectory, { recursive: true, force: true });
}
