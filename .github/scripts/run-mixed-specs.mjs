// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Ordinary specs only. SQL/integration keep their existing VSTest/shard/retry entry points.
// Source contracts are mocks, NOT collector acceptance: Linux production hits and real
// negative/deadline/process-tree proof remain required in the coherent owner candidate.
import { readFile, readdir, mkdir, mkdtemp, access } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { runProcess } from './mixed-specs-process.mjs';
import { decode, validateTrx, validateCoverage, coverageFiles } from './mixed-specs-results.mjs';
import { ordinaryProjects, ordinaryFilter, inventory } from './mixed-specs-selection.mjs';

// Preserve the existing import surface for contract consumers.
export { runProcess } from './mixed-specs-process.mjs';
export { parseXml, validateTrx, validateCoverage } from './mixed-specs-results.mjs';
export { v3Projects, v2Projects, shippingProjects, ordinaryFilter, validateSelection, inventory } from './mixed-specs-selection.mjs';
const fail = message => { throw new Error(message); };

// dotnet-coverage documents ONE child command string. Quote each argument for
// .NET command-line parsing, never for a shell; reject control characters.
export function childCommand(args) {
    return args.map(value => {
        if (/[\x00-\x1f\x7f]/.test(value)) fail('Control character in child argument');
        return `"${value.replace(/(\\*)"/g, '$1$1\\"').replace(/(\\+)$/g, '$1$1')}"`;
    }).join(' ');
}
export function invocation(info, { root, directory, filter, timeout }) {
    const trx = path.join(directory, 'results.trx');
    if (info.runner === 'v2') return { command: 'dotnet', args: ['test', info.project, '--no-build', '--no-restore', '--configuration', 'Release', '--framework', info.tfm, '--settings', path.join(root, 'specs.runsettings'), '--filter', filter, '--collect:XPlat Code Coverage', '--results-directory', directory, '--logger', 'trx;LogFileName=results.trx'], timeout };
    return { command: 'dotnet', args: ['tool', 'run', 'dotnet-coverage', '--', 'collect', '-f', 'cobertura', '-o', path.join(directory, 'coverage.cobertura.xml'), childCommand(['dotnet', info.dll, '-filterVSTest', filter, '-result-trx', trx])], timeout };
}
export async function runProject(info, options, execute = runProcess) {
    const call = invocation(info, options);
    await access(path.resolve(options.root, info.dll));
    if ((await readdir(options.directory)).length) fail(`${info.project}: results directory must be fresh and empty`);
    const child = await execute(call.command, call.args, { cwd: options.root, timeout: call.timeout });
    if (child.code !== 0) throw Object.assign(new Error(`${info.project}: child exited ${child.code}`), { exitCode: child.code });
    const identities = validateTrx(await readFile(path.join(options.directory, 'results.trx'), 'utf8'));
    const files = await coverageFiles(options.directory);
    if (files.length !== 1) fail(`${info.project}: expected one fresh Cobertura file`);
    const production = await validateCoverage(await readFile(files[0], 'utf8'), options.root);
    console.log(`${info.project}: ${identities.length} results; production hit ${production}`);
}
export async function main(args = process.argv.slice(2), { root = process.cwd(), execute = runProcess } = {}) {
    if (args.length) fail('Usage: node .github/scripts/run-mixed-specs.mjs (ordinary Release specs; SQL/integration unchanged)');
    const settings = await readFile(path.join(root, 'specs.runsettings'), 'utf8');
    const timeouts = [...settings.matchAll(/<TestSessionTimeout>(\d+)<\/TestSessionTimeout>/g)];
    const filters = [...settings.matchAll(/<TestCaseFilter>([^<]+)<\/TestCaseFilter>/g)];
    if (timeouts.length !== 1 || filters.length !== 1 || Number(timeouts[0][1]) <= 0) fail('Unsupported runsettings: expected one deadline and filter');
    const filter = `(${ordinaryFilter})&(${decode(filters[0][1])})`;
    const infos = await inventory(root, execute); // All 29 tests + 2 shipping consumers, before launching any test.
    await mkdir(path.join(root, 'coverage-results'), { recursive: true });
    const output = await mkdtemp(path.join(root, 'coverage-results', 'mixed-'));
    for (const info of ordinaryProjects(infos)) {
        const directory = path.join(output, `${info.assemblyName}-${info.tfm}`);
        await mkdir(directory); // No reuse, caching or duplicate output assembly identities.
        await runProject(info, { root, directory, filter, timeout: Number(timeouts[0][1]) }, execute);
    }
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = error.exitCode ?? 1; });
}
