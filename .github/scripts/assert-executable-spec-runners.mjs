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

function commandContent(file, content) {
    // Standalone Python triple-quoted documentation is not an executable command.
    // Keep newlines so diagnostics still point to the original source line.
    return file.endsWith('.py') ? content.replace(/^[ \t]*(?:"""[\s\S]*?"""|'''[\s\S]*?''')/gm, text => text.replace(/[^\n]/g, ' ')) : content;
}

export async function assertExecutableSpecRunners(root) {
    const workflows = (await readdir(path.join(root, '.github/workflows')))
        .filter(name => /\.ya?ml$/.test(name)).map(name => `.github/workflows/${name}`);
    const scripts = await scriptFiles(root, '.github/scripts');
    const actions = await scriptFiles(root, '.github/actions');
    if ([workflows, scripts, actions].some(files => !files.length)) throw new Error('Expected nonempty workflow, CI script and action populations');
    const violations = [];
    for (const file of [...workflows, ...scripts, ...actions]) {
        const content = commandContent(file, await readFile(path.join(root, file), 'utf8'));
        // Include continuations and folded YAML lines, without a project-name allowlist:
        // a solution or working-directory invocation is just as unsafe as an explicit project.
        for (const match of content.matchAll(/\bdotnet["']?(?:\s|\\\r?\n)+["']?test\b/g)) {
            const line = content.slice(0, match.index).split('\n').length;
            violations.push(`${file}:${line}: test-host invocation forbidden; run the built spec executable`);
        }
    }
    if (violations.length) throw new Error(violations.join('\n'));
    console.log(`Checked ${workflows.length} workflows, ${scripts.length} CI scripts and ${actions.length} action files for executable-only spec dispatch`);
}
