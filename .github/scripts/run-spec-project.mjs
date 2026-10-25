// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { mkdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';
import { evaluateProject, v3Projects } from './mixed-specs-selection.mjs';
import { runProject, runProcess } from './run-mixed-specs.mjs';
import { decode } from './mixed-specs-results.mjs';

export async function main(args = process.argv.slice(2), { root = process.cwd(), execute = runProcess, evaluate = evaluateProject } = {}) {
    const { values } = parseArgs({ args, options: {
        project: { type: 'string' }, filter: { type: 'string', default: '' },
        'results-directory': { type: 'string' }, settings: { type: 'string' },
        coverage: { type: 'boolean', default: false }
    } });
    if (!v3Projects.includes(values.project) || !values['results-directory']) {
        throw new Error('Expected a known spec project and --results-directory');
    }
    let filter = values.filter, timeout = 1800000;
    if (values.settings) {
        const settings = await readFile(path.resolve(root, values.settings), 'utf8');
        const deadlines = [...settings.matchAll(/<TestSessionTimeout>(\d+)<\/TestSessionTimeout>/g)];
        const filters = [...settings.matchAll(/<TestCaseFilter>([^<]+)<\/TestCaseFilter>/g)];
        if (deadlines.length !== 1 || filters.length !== 1 || Number(deadlines[0][1]) <= 0) throw new Error('Unsupported runsettings: expected one deadline and filter');
        timeout = Number(deadlines[0][1]);
        filter = filter ? `(${filter})&(${decode(filters[0][1])})` : decode(filters[0][1]);
    }
    const info = await evaluate(root, values.project, execute);
    const directory = path.resolve(root, values['results-directory']);
    await mkdir(directory, { recursive: true });
    await runProject(info, { root, directory, filter, timeout, coverage: values.coverage }, execute);
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = error.exitCode ?? 1; });
}
