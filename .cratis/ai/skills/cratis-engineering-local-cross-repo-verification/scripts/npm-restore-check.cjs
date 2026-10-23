// cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/scripts/npm-restore-check.cjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
'use strict';

// Read-only guard: all validation completes before the shell restores either file.
const fs = require('fs');
const path = require('path');
const {isDeepStrictEqual: equal} = require('util');
const [repo, backup] = process.argv.slice(2);
const read = (base, name) => fs.readFileSync(path.join(base, name), 'utf8');
const json = (base, name) => JSON.parse(read(base, name));
const refuse = message => { throw new Error(message); };

function overlayResolutions(original, expected) {
    const before = original.resolutions || {};
    const after = expected.resolutions || {};
    return new Map(Object.entries(after).filter(([name, value]) => !equal(value, before[name])));
}

function checkManifest(original, current, overlay) {
    for (const [name, value] of overlay) {
        const actual = (current.resolutions || {})[name];
        if (actual !== value && !equal(actual, (original.resolutions || {})[name]))
            refuse('Changed overlay resolution: ' + name);
    }
    const withoutOverlay = manifest => {
        const copy = JSON.parse(JSON.stringify(manifest));
        if (copy.resolutions) {
            for (const name of overlay.keys()) delete copy.resolutions[name];
            if (!Object.keys(copy.resolutions).length) delete copy.resolutions;
        }
        return copy;
    };
    if (!equal(withoutOverlay(current), withoutOverlay(original)))
        refuse('Unrelated package.json changes');
}

// Yarn 4 lockfiles have quoted top-level descriptor keys and indented bodies.
// Preserve bytes for every unaffected block, including metadata and comments;
// unsupported syntax or install churn is a conflict, not permission to overwrite.
function lockBlocks(text) {
    const starts = [...text.matchAll(/^[^\s#].*$/gm)];
    const header = text.slice(0, starts.length ? starts[0].index : text.length);
    const blocks = new Map();
    for (let index = 0; index < starts.length; index++) {
        const line = starts[index][0];
        const key = line === '__metadata:' ? '__metadata' : JSON.parse(line.slice(0, -1));
        if (!line.endsWith(':') || typeof key !== 'string' || blocks.has(key))
            refuse('Unsupported or duplicate lockfile key');
        blocks.set(key, text.slice(starts[index].index, starts[index + 1]?.index ?? text.length));
    }
    return {header, blocks};
}

function descriptorNames(key) {
    return key.split(', ').map(descriptor => {
        const match = descriptor.match(/^(@[^/]+\/[^@]+|[^@]+)@/);
        if (!match) refuse('Unsupported lockfile descriptor: ' + descriptor);
        return match[1];
    });
}

function localBlock(key, body, overlay) {
    if (key === '__metadata') return false;
    const names = descriptorNames(key);
    const name = names[0];
    if (!overlay.has(name) || names.some(other => other !== name)) return false;
    const match = body.match(/^  resolution: (.+)$/m);
    if (!match) return false;
    const resolution = JSON.parse(match[1]);
    const prefix = name + '@' + overlay.get(name);
    // Yarn may append archive/locator parameters, but not a different filename.
    return [prefix, encodeURI(prefix)].some(value => resolution === value ||
        resolution.startsWith(value + '::') || resolution.startsWith(value + '#'));
}

function checkLock(original, current, overlay) {
    if (original === current) return;
    const before = lockBlocks(original), after = lockBlocks(current);
    if (before.header !== after.header) refuse('Changed lockfile header');
    const localNames = new Set();
    for (const [key, body] of after.blocks) {
        if (before.blocks.get(key) === body) continue;
        if (!localBlock(key, body, overlay)) refuse('Unrelated lockfile change: ' + key);
        for (const name of descriptorNames(key)) localNames.add(name);
    }
    for (const [key, body] of before.blocks) {
        if (after.blocks.get(key) === body) continue;
        // Only entries for locally replaced package names may change or vanish.
        // Changed entries were already checked against the exact tarball above.
        if (key === '__metadata' ||
            descriptorNames(key).some(name => !localNames.has(name)))
            refuse('Changed or removed unrelated lockfile entry: ' + key);
    }
}

try {
    const original = json(backup, 'package.json');
    const expected = json(backup, 'expected-package.json');
    const overlay = overlayResolutions(original, expected);
    checkManifest(original, json(repo, 'package.json'), overlay);
    const beforeExists = fs.existsSync(path.join(backup, 'yarn.lock'));
    const afterExists = fs.existsSync(path.join(repo, 'yarn.lock'));
    if (beforeExists !== afterExists) refuse('Lockfile created or removed; manual review required');
    if (beforeExists) checkLock(read(backup, 'yarn.lock'), read(repo, 'yarn.lock'), overlay);
} catch (error) {
    console.error('ERROR: ' + error.message);
    process.exitCode = 2;
}
