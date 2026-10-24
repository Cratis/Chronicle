// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { parseXml, nodes } from './mixed-specs-results.mjs';

const project = directory => `${directory}/${directory.split('/').at(-1)}.csproj`;
const benchmarkDirectory = 'Benchmarks/Chronicle.Benchmarks.Clustering';
export const benchmarkProject = project(`${benchmarkDirectory}.Specs`);
export const v3Projects = [
    `${benchmarkDirectory}.Specs`,
    'Source/Infrastructure.Specs',
    ...['AspNetCore', 'Aspire', 'Connections', 'DotNET.CodeAnalysis', 'DotNET', 'Testing'].map(x => `Source/Clients/${x}.Specs`),
    ...['Compatibility', 'Concepts', 'Core', 'Server', 'Storage.AzureKeyVault', 'Storage.Vault'].map(x => `Source/Kernel/${x}.Specs`),
    ...['GrainCompatibility', 'GrpcCodeGenerator', 'LegacyContracts', 'ProtoGenerator', 'WireCompatibility'].map(x => `Source/Tools/${x}.Specs`),
    'Source/Clients/XUnit.Integration.Specs',
    'Source/Clients/XUnit.Integration.v3.Specs',
    ...['Storage', 'Storage.InMemory', 'Storage.MongoDB', 'Storage.Sql'].map(x => `Source/Kernel/${x}.Specs`),
    ...['Api', 'Client', 'Clustering', 'Kernel', 'MongoDB', 'SqlClustering'].map(x => `Integration/${x}`)
].map(project);
// Shipping packages are libraries, not spec runners. Each one declares the xunit generation it ships against:
// Cratis.Chronicle.Testing has none, XUnit.Integration is the xunit v2 package and XUnit.Integration.v3 the v3 one.
export const shippingRunners = {
    [project('Source/Clients/Testing')]: 'none',
    [project('Source/Clients/XUnit.Integration')]: 'v2',
    [project('Source/Clients/XUnit.Integration.v3')]: 'v3'
};
export const shippingProjects = Object.keys(shippingRunners);
export const ordinaryFilter = 'FullyQualifiedName!~Cratis.Chronicle.Integration&FullyQualifiedName!~Cratis.Chronicle.Kernel.Integration&FullyQualifiedName!~Cratis.Chronicle.Storage.Sql.';
const fail = message => { throw new Error(message); };

export function ordinaryProjects(infos) {
    return infos.filter(x => !shippingProjects.includes(x.project) && !x.project.startsWith('Integration/') && !x.project.includes('Storage.Sql.Specs/'));
}

function packageMap(libraries) {
    return new Map(Object.keys(libraries).map(key => { const slash = key.lastIndexOf('/'); return [key.slice(0, slash).toLowerCase(), key.slice(slash + 1)]; }));
}
function validateProperties(projectPath, properties) {
    if (![...v3Projects, ...shippingProjects].includes(projectPath)) fail(`Unknown project ${projectPath}`);
    const shipping = shippingProjects.includes(projectPath);
    if ((String(properties.IsTestProject).toLowerCase() === 'true') === shipping) fail(`${projectPath}: ambiguous test/shipping selection`);
    if (!shipping && properties.OutputType !== 'Exe') fail(`${projectPath}: test project must be an executable`);
    validateOutput(projectPath, properties);
}
function validateOutput(projectPath, properties) {
    if (![properties.TargetFramework, properties.AssemblyName, properties.TargetPath].every(Boolean)) fail(`${projectPath}: missing output/TFM`);
    if (properties.TargetFrameworks) fail(`${projectPath}: ambiguous multi-TFM output`);
    if (![properties.AssemblyName, properties.TargetFramework].every(x => /^[\w.-]+$/.test(x))) fail(`${projectPath}: invalid output identity`);
    if (path.basename(properties.TargetPath) !== `${properties.AssemblyName}.dll`) fail(`${projectPath}: output assembly mismatch`);
}
function validateEvaluatedPackages(projectPath, items, resolved) {
    const central = new Map((items.PackageVersion ?? []).map(x => [x.Identity.toLowerCase(), x.Version]));
    for (const reference of items.PackageReference) {
        const id = reference.Identity.toLowerCase();
        const version = reference.VersionOverride || reference.Version || central.get(id);
        if (!version) fail(`${projectPath}: missing evaluated package version for ${id}`);
        if (resolved.get(id) !== version) fail(`${projectPath}: stale evaluated package version for ${id}`);
    }
}
function validateRuntimePackages(projectPath, deps, resolved) {
    for (const [key, library] of Object.entries(deps.libraries ?? {})) {
        if (library.type !== 'package') continue;
        const [id, version] = key.split('/');
        if (resolved.get(id.toLowerCase()) !== version) fail(`${projectPath}: stale runtime package ${id}`);
    }
}
function packageChecker(projectPath, resolved, runtime) {
    return (name, present) => {
        if (![resolved.has(name), runtime.has(name)].every(value => value === present)) fail(`${projectPath}: resolved/runtime mismatch for ${name}`);
        if (!present) return;
        if (resolved.get(name) !== runtime.get(name)) fail(`${projectPath}: stale runtime version for ${name}`);
    };
}
function validateRunnerPackages(projectPath, { ids, resolved, check }) {
    // Every spec project runs on xunit v3. A shipping package resolves exactly its declared generation,
    // never both, and its assets and runtime must agree.
    const runner = shippingRunners[projectPath] ?? 'v3';
    const v2 = runner === 'v2', v3 = runner === 'v3';
    check('xunit.v3.extensibility.core', v3);
    check('xunit.extensibility.core', v2);
    check('cratis.specifications.xunit.v3', v3);
    check('cratis.specifications.xunit', v2);
    if (ids.includes('cratis.specifications.xunit.v3') !== v3) fail(`${projectPath}: ambiguous evaluated v3 specifications reference`);
    if (ids.includes('cratis.specifications.xunit') !== v2) fail(`${projectPath}: ambiguous evaluated v2 specifications reference`);
    if (!shippingProjects.includes(projectPath) && !resolved.has('xunit.v3.mtp-off')) fail(`${projectPath}: MTP-off selection missing`);
}
export function validateSelection(projectPath, evaluated, assets, deps) {
    if (!evaluated.Properties) fail(`${projectPath}: missing evaluated properties`);
    if (!Array.isArray(evaluated.Items?.PackageReference)) fail(`${projectPath}: missing evaluated references`);
    const properties = evaluated.Properties;
    validateProperties(projectPath, properties);
    const resolved = packageMap(assets.libraries ?? {}), runtime = packageMap(deps.libraries ?? {});
    validateEvaluatedPackages(projectPath, evaluated.Items, resolved);
    validateRuntimePackages(projectPath, deps, resolved);
    validateRunnerPackages(projectPath, {
        ids: evaluated.Items.PackageReference.map(x => x.Identity.toLowerCase()),
        resolved, check: packageChecker(projectPath, resolved, runtime)
    });
    return { project: projectPath, runner: 'v3', tfm: properties.TargetFramework, dll: properties.TargetPath, assemblyName: properties.AssemblyName,
        coverageRoots: projectPath === benchmarkProject ? ['Source', benchmarkDirectory] : ['Source'] };
}
function checkProjectBoundary(relative) {
    const candidate = relative.endsWith('.Specs.csproj') || (relative.startsWith('Integration/') && relative.endsWith('.csproj'));
    if (!candidate) return;
    if (![...v3Projects, ...shippingProjects].includes(relative)) fail(`Unclassified test project ${relative}`);
}
export function validateSolutionProjects(xml) {
    const solution = parseXml(xml);
    if (solution.name !== 'Solution') fail('Expected Chronicle.slnx Solution root');
    const projects = nodes(solution, 'Project').map(item => {
        if (!item.attrs.Path) fail('Chronicle.slnx project missing Path');
        return item.attrs.Path.replaceAll('\\', '/');
    });
    for (const projectPath of projects) checkProjectBoundary(projectPath);
    if (!projects.some(item => item.endsWith('.Specs.csproj'))) fail('Chronicle.slnx contains no spec projects');
    return projects;
}
async function discoverProjects(root, directory) {
    for (const entry of await readdir(path.join(root, directory), { withFileTypes: true })) {
        if (['bin', 'obj', 'node_modules', '.git'].includes(entry.name)) continue;
        const relative = `${directory}/${entry.name}`;
        if (entry.isDirectory()) await discoverProjects(root, relative);
        else checkProjectBoundary(relative);
    }
}
export async function evaluateProject(root, projectPath, execute) {
    const query = await execute('dotnet', ['msbuild', projectPath, '-nologo', '-p:Configuration=Release', '-p:DisableDockerBuild=true', '-getProperty:IsTestProject,OutputType,TargetFramework,TargetFrameworks,AssemblyName,TargetPath,ProjectAssetsFile', '-getItem:PackageReference,PackageVersion'], { cwd: root, capture: true, timeout: 120000 });
    if (query.code !== 0) throw Object.assign(new Error(`${projectPath}: evaluation failed\n${query.stderr}`), { exitCode: query.code });
    const evaluated = JSON.parse(query.stdout);
    const readJson = async file => JSON.parse(await readFile(path.resolve(root, file), 'utf8'));
    const assets = await readJson(evaluated.Properties.ProjectAssetsFile);
    const deps = await readJson(evaluated.Properties.TargetPath.replace(/\.dll$/, '.deps.json'));
    return validateSelection(projectPath, evaluated, assets, deps);
}
export async function inventory(root, execute) {
    const result = [];
    validateSolutionProjects(await readFile(path.join(root, 'Chronicle.slnx'), 'utf8'));
    await discoverProjects(root, 'Source');
    await discoverProjects(root, 'Integration');
    for (const projectPath of [...v3Projects, ...shippingProjects]) result.push(await evaluateProject(root, projectPath, execute));
    return result;
}
