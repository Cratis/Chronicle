// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createHash } from 'node:crypto';
import { constants, realpathSync } from 'node:fs';
import { copyFile, lstat, mkdir, mkdtemp, open, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { evaluateProject, v3Projects } from './mixed-specs-selection.mjs';
import { runProcess } from './mixed-specs-process.mjs';

const projects = Object.fromEntries(['Client', 'Kernel', 'Clustering'].map(name => [name.toLowerCase(),
    v3Projects.find(project => project === `Integration/${name}/${name}.csproj`)]));
const digest = value => createHash('sha256').update(value).digest('hex');
const fail = message => { throw new Error(message); };
function layout(bundle) {
    const project = projects[bundle];
    if (!project) fail(`Unknown bundle ${bundle}`);
    const directory = path.posix.dirname(project);
    const name = path.posix.basename(project);
    return { project, bin: `${directory}/bin`, output: `${directory}/bin/Release/net10.0`,
        assets: `${directory}/obj/project.assets.json`,
        imports: ['props', 'targets'].map(extension => `${directory}/obj/${name}.nuget.g.${extension}`),
        manifest: `.integration-bundles/${bundle}.json` };
}
function relative(root, file) {
    const result = path.relative(realpathSync(root), realpathSync(path.resolve(root, file))).split(path.sep).join('/');
    if (!result) fail('Path outside checkout');
    if (result.split('/').includes('..')) fail('Path outside checkout');
    return result;
}
async function filesUnder(root, directory = '') {
    const files = [];
    for (const entry of await readdir(path.join(root, directory), { withFileTypes: true })) {
        const file = path.posix.join(directory, entry.name);
        if (entry.isDirectory()) files.push(...await filesUnder(root, file));
        else if (entry.isFile()) files.push(file);
        else fail(`Unsupported bundle entry ${file}`);
    }
    return files.sort();
}
async function inventory(root, bundle) {
    const { output, assets, imports } = layout(bundle);
    const files = [...await filesUnder(root, output), assets, ...imports].sort();
    return Promise.all(files.map(async file => {
        if (!(await lstat(path.join(root, file))).isFile()) fail(`Not a regular file ${file}`);
        const bytes = await readFile(path.join(root, file));
        return { path: file, size: bytes.length, sha256: digest(bytes) };
    }));
}
async function command(execute, root, program, args) {
    const result = await execute(program, args, { cwd: root, capture: true, timeout: 300000 });
    if (result.code !== 0 || result.signal) fail(`${program}: bundle command failed`);
    return result;
}
function validCase(item) {
    if (!item) return false;
    return [item.Class, item.Method].every(value => typeof value === 'string' && value.length > 0);
}
export async function discover(root, dll, execute = runProcess) {
    const result = await command(execute, root, 'dotnet', [dll, '-nologo', '-list', 'full/json']);
    // The runner emits one complete JSON array after discovery. Any diagnostic outside
    // it, a truncated document, or a discovery error object invalidates the entire list.
    if (result.stderr.trim()) fail('Discovery wrote diagnostics');
    const cases = JSON.parse(result.stdout);
    if (!Array.isArray(cases)) fail('Invalid discovery list');
    if (!cases.length) fail('Empty discovery list');
    if (!cases.every(validCase)) fail('Invalid discovery case');
    return cases.map(item => `${item.Class}.${item.Method}`).sort();
}
function discovered(cases) {
    // Class/method plus occurrence index is stable even for theories with dynamic
    // display names and IDs. Preserve multiplicity without hashing theory values.
    const occurrences = new Map();
    const identities = cases.map(name => {
        const index = occurrences.get(name) ?? 0;
        occurrences.set(name, index + 1);
        return `${name}#${index}`;
    });
    return { count: cases.length, idsSha256: digest(JSON.stringify(identities)) };
}
function filteredCount(cases, filter) {
    const terms = filter.split('|').map(term => {
        const match = /^FullyQualifiedName~([\w.+]+)$/.exec(term);
        if (!match) fail(`Unsupported bundle filter ${filter}`);
        return match[1].toLowerCase();
    });
    return cases.filter(name => terms.some(term => name.toLowerCase().includes(term))).length;
}
async function exists(file) {
    try { await lstat(file); return true; }
    catch (error) { if (error.code === 'ENOENT') return false; throw error; }
}
export async function pack({ root, bundle, out, prefix, sha, execute = runProcess }) {
    if (!sha || !prefix) fail('Bundle SHA and prefix are required');
    const contract = layout(bundle);
    const info = await evaluateProject(root, contract.project, execute);
    const dll = relative(root, info.dll);
    if (!dll.startsWith(`${contract.output}/`)) fail('Unexpected bundle output');
    const cases = await discover(root, dll, execute);
    const manifest = { sha, project: contract.project, dll, files: await inventory(root, bundle), discovered: discovered(cases) };
    if (!manifest.files.some(file => file.path === dll)) fail('Bundle executable missing');
    await mkdir(path.join(root, '.integration-bundles'), { recursive: true });
    await writeFile(path.join(root, contract.manifest), `${JSON.stringify(manifest, null, 2)}\n`);
    await mkdir(out, { recursive: true });
    await command(execute, root, 'tar', ['--use-compress-program', 'zstd', '-cf', path.resolve(out, `integration-bundle-${bundle}.tar.zst`),
        ...manifest.files.map(file => file.path), contract.manifest]);
    return manifest;
}
async function extract(stage, contract, archive, execute) {
    const names = await command(execute, stage, 'tar', ['--use-compress-program', 'zstd -d', '-tf', archive]);
    const types = await command(execute, stage, 'tar', ['--use-compress-program', 'zstd -d', '-tvf', archive]);
    if (names.stderr.trim() || types.stderr.trim()) fail('Bundle archive listing wrote diagnostics');
    const entries = names.stdout.trim().split('\n');
    const details = types.stdout.trim().split('\n');
    // Both GNU tar and bsdtar mark regular members with '-', hardlinks with 'h'
    // and symlinks with 'l'. Reject every other type before invoking extraction.
    if (details.length !== entries.length) fail('Inconsistent bundle archive listing');
    if (!details.every(line => line.startsWith('-'))) fail('Non-regular bundle archive member');
    if (!entries.includes(contract.manifest)) fail('Missing bundle manifest');
    if (new Set(entries).size !== entries.length) fail('Duplicate bundle archive paths');
    if (!entries.every(file => validEntry(file, contract))) fail('Invalid bundle archive paths');
    await command(execute, stage, 'tar', ['--use-compress-program', 'zstd -d', '-xf', archive, '--no-same-owner', '--no-same-permissions']);
    return entries.sort();
}
async function checkedDirectory(root, directory) {
    let current = root;
    for (const segment of directory.split('/')) {
        current = path.join(current, segment);
        try { await mkdir(current, { mode: 0o700 }); }
        catch (error) { if (error.code !== 'EEXIST') throw error; }
        if (!(await lstat(current)).isDirectory()) fail(`Unsafe bundle parent ${current}`);
    }
    return current;
}
async function regularFile(file) {
    const stat = await lstat(file);
    if (!stat.isFile() || stat.nlink !== 1) fail(`Non-regular or linked bundle file ${file}`);
    return stat;
}
async function evaluateStaged(root, stage, contract, execute) {
    return evaluateProject(stage, contract.project, (program, args, options) => execute(program, [...args,
        `-p:BaseOutputPath=${path.join(stage, contract.bin)}/`,
        `-p:MSBuildProjectExtensionsPath=${path.dirname(path.join(stage, contract.assets))}/`
    ], { ...options, cwd: root }));
}
async function validateStage({ root, stage, bundle, entries, filter, namespaces, sha, execute }) {
    if (JSON.stringify(await filesUnder(stage)) !== JSON.stringify(entries)) fail('Bundle extraction inventory mismatch');
    for (const file of entries) await regularFile(path.join(stage, file));
    const contract = layout(bundle);
    const manifest = JSON.parse(await readFile(path.join(stage, contract.manifest), 'utf8'));
    await validateInventory(stage, bundle, manifest, sha);
    const info = await evaluateStaged(root, stage, contract, execute);
    if (relative(stage, info.dll) !== manifest.dll) fail('Bundle executable mismatch');
    validateCases(await discover(root, path.join(stage, manifest.dll), execute), manifest, filter, namespaces);
    return manifest;
}
async function copyRegularFile(stage, root, file) {
    await checkedDirectory(root, path.posix.dirname(file));
    const stat = await regularFile(path.join(stage, file));
    const source = await open(path.join(stage, file), constants.O_RDONLY | constants.O_NOFOLLOW);
    try {
        const destination = await open(path.join(root, file), constants.O_WRONLY | constants.O_CREAT |
            constants.O_EXCL | constants.O_NOFOLLOW, stat.mode & 0o777);
        try { await destination.writeFile(await source.readFile()); }
        finally { await destination.close(); }
    } finally { await source.close(); }
}
async function copyValidated(stage, root, entries) {
    // Check every destination before the first copy, then recheck each parent at
    // copy time. Exclusive no-follow opens never truncate an existing inode.
    for (const file of entries) {
        await checkedDirectory(root, path.posix.dirname(file));
        if (await exists(path.join(root, file))) fail(`Pre-existing bundle destination ${file}`);
    }
    for (const file of entries) await copyRegularFile(stage, root, file);
}
function validEntry(file, contract) {
    if (path.posix.normalize(file) !== file) return false;
    return file.startsWith(`${contract.output}/`) || [contract.manifest, contract.assets, ...contract.imports].includes(file);
}
async function validateInventory(root, bundle, manifest, sha) {
    const contract = layout(bundle);
    if (!sha) fail('Bundle SHA is required');
    if (manifest.sha !== sha) fail('Bundle SHA mismatch');
    if (manifest.project !== contract.project) fail('Bundle project mismatch');
    if (JSON.stringify(manifest.files) !== JSON.stringify(await inventory(root, bundle))) fail('Bundle file inventory mismatch');
    const binFiles = await filesUnder(root, contract.bin);
    if (binFiles.length !== manifest.files.filter(file => file.path.startsWith(`${contract.bin}/`)).length) fail('Extra bundle bin files');
}
function validateCases(cases, manifest, filter, namespaces) {
    if (JSON.stringify(discovered(cases)) !== JSON.stringify(manifest.discovered)) fail('Bundle discovery mismatch');
    // Apply case-insensitive FQN contains locally to the complete list rather than
    // depending on runner execution filters being applied during listing.
    if (!filteredCount(cases, filter)) fail('Empty filtered bundle');
    for (const namespace of namespaces) {
        if (!filteredCount(cases, `FullyQualifiedName~${namespace}.`)) fail(`Empty bundle namespace ${namespace}`);
    }
}
export async function verify({ root, bundle, archive, filter, namespaces = [], sha, execute = runProcess }) {
    root = realpathSync(root);
    const contract = layout(bundle);
    for (const file of [contract.bin, contract.assets]) {
        if (await exists(path.join(root, file))) fail(`Pre-existing bundle output ${file}`);
    }
    const parent = await checkedDirectory(root, '.ai-work/integration-bundles');
    const temporary = await mkdtemp(path.join(parent, 'verify-'));
    try {
        const snapshot = path.join(temporary, 'bundle.tar.zst');
        await copyFile(path.resolve(root, archive), snapshot, constants.COPYFILE_EXCL);
        const stage = await checkedDirectory(temporary, 'checkout');
        const entries = await extract(stage, contract, snapshot, execute);
        const manifest = await validateStage({ root, stage, bundle, entries, filter, namespaces, sha, execute });
        await copyValidated(stage, root, entries);
        return manifest;
    } finally { await rm(temporary, { recursive: true, force: true }); }
}
function parseOptions(args) {
    const options = new Map();
    for (let index = 0; index < args.length; index += 2) {
        if (!args[index].startsWith('--')) fail('Expected --option value');
        if (!args[index + 1]) fail('Expected --option value');
        const key = args[index].slice(2);
        options.set(key, [...(options.get(key) ?? []), args[index + 1]]);
    }
    return options;
}
async function packOptions(options, common) {
    if (!options.has('bundle')) fail('pack requires bundles');
    if (!options.has('out')) fail('pack requires output directory');
    for (const bundle of options.get('bundle')) {
        await pack({ ...common, bundle, out: options.get('out')[0], prefix: options.get('prefix')?.[0] });
    }
}
export async function main([action, ...args], root = process.cwd(), execute = runProcess) {
    const options = parseOptions(args);
    const value = key => options.get(key)?.[0];
    const common = { root: path.resolve(root), sha: process.env.GITHUB_SHA, execute };
    if (action === 'pack') return packOptions(options, common);
    if (action !== 'verify') fail('Expected pack or verify');
    return verify({ ...common, bundle: value('bundle'), archive: value('archive'), filter: value('filter'),
        namespaces: value('namespaces')?.split(',').filter(Boolean) ?? [] });
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    main(process.argv.slice(2)).catch(error => { console.error(error.message); process.exitCode = 1; });
}
