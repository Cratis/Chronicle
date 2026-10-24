// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

async function scriptFiles(root, directory) {
    const files = [];
    for (const entry of await readdir(path.join(root, directory), { withFileTypes: true })) {
        const file = `${directory}/${entry.name}`;
        if (entry.isDirectory()) files.push(...await scriptFiles(root, file));
        else if (entry.isFile()) files.push(file);
    }
    return files;
}

export async function assertExecutableSpecRunners(root) {
    const workflows = (await readdir(path.join(root, '.github/workflows')))
        .filter(name => /\.ya?ml$/.test(name)).map(name => `.github/workflows/${name}`);
    const scripts = await scriptFiles(root, '.github/scripts');
    if (!workflows.length || !scripts.length) throw new Error('Expected nonempty workflow and CI script populations');
    const violations = [];
    for (const file of [...workflows, ...scripts]) {
        const content = await readFile(path.join(root, file), 'utf8');
        // Include continuations and folded YAML lines, without a project-name allowlist:
        // a solution or working-directory invocation is just as unsafe as an explicit project.
        for (const match of content.matchAll(/\bdotnet["']?(?:\s|\\\r?\n)+["']?test\b/g)) {
            const line = content.slice(0, match.index).split('\n').length;
            violations.push(`${file}:${line}: test-host invocation forbidden; run the built spec executable`);
        }
    }
    if (violations.length) throw new Error(violations.join('\n'));
    console.log(`Checked ${workflows.length} workflows and ${scripts.length} CI scripts for executable-only spec dispatch`);
}
