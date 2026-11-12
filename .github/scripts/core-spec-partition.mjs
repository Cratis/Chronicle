// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, writeFile } from 'node:fs/promises';
import { discoverSpecClasses } from './core-spec-source.mjs';
import { coverageTypes } from './core-spec-coverage.mjs';
export { discoverSpecClasses, discoverNamespaces } from './core-spec-source.mjs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { nodes, parseXml } from './mixed-specs-results.mjs';

export const CORE_PROJECT = 'Source/Kernel/Core.Specs/Core.Specs.csproj';
export const CORE_PARTS = 2;
export const PER_TEST_SECONDS = 0.003;
const table = new URL('./core-spec-partition.json', import.meta.url);
const repair = 'run node .github/scripts/core-spec-partition.mjs --refresh <trx...>';
const fail = message => { throw new Error(`${message}; ${repair}`); };
const ordinal = (a, b) => a < b ? -1 : a > b ? 1 : 0;
export const coreUnitId = part => `${CORE_PROJECT}#core-part-${part}-of-${CORE_PARTS}`;

export async function loadPartition(file = table) {
    return JSON.parse(await readFile(file, 'utf8'));
}
export function partitionUnits(units, parts = CORE_PARTS) {
    if (parts !== CORE_PARTS) fail(`Expected ${CORE_PARTS} Core parts`);
    const result = Array.from({ length: parts }, (_, index) => ({ part: index + 1, units: [], weight: 0 }));
    for (const unit of [...units].sort((a, b) => b.weight - a.weight || ordinal(a.prefix, b.prefix))) {
        const part = result.reduce((best, candidate) => candidate.weight < best.weight ? candidate : best);
        part.units.push(unit);
        part.weight += unit.weight;
    }
    if (result.some(part => !part.units.length)) fail('Empty Core part');
    return result;
}
export function partFilter(prefixes) {
    if (!prefixes.length || prefixes.some(prefix => !/^(?:[\w+`]+\.)+$/.test(prefix))) fail('Invalid/empty Core filter prefixes');
    return `(${prefixes.map(prefix => `FullyQualifiedName~${prefix}`).join('|')})`;
}
export const matchesFilter = (fqn, prefixes) => prefixes.some(prefix => `${fqn}.`.includes(prefix));
export function assertExactOnce(classes, parts) {
    if (!classes.length) fail('No type declarations for exact-once guard');
    for (const fqn of classes) {
        const matches = parts.filter(part => matchesFilter(fqn, part.units.map(unit => unit.prefix)));
        if (matches.length !== 1) fail(`Core type ${fqn} matches ${matches.length} parts (expected exactly one)`);
    }
}
function validateUnits(data) {
    const valid = [data?.version === 1, Array.isArray(data?.units), Boolean(data?.units?.length)].every(Boolean);
    if (!valid) fail('Invalid Core partition schema');
    const prefixes = [];
    for (const unit of data.units) {
        validateUnit(unit, prefixes);
        prefixes.push(unit.prefix);
    }
}
function validateUnit(unit, prefixes) {
    partFilter([unit.prefix]);
    if (!(Number.isFinite(unit.weight) && unit.weight > 0)) fail(`Invalid Core weight ${unit.prefix}`);
    if (prefixes.some(prefix => prefix.startsWith(unit.prefix) || unit.prefix.startsWith(prefix))) fail(`Duplicate/overlapping Core unit ${unit.prefix}`);
}
export async function checkPartition(root, data, parts = CORE_PARTS) {
    validateUnits(data);
    const classes = await coverageTypes(root);
    const unassigned = classes.find(info => data.units.filter(unit => `${info.fqn}.`.startsWith(unit.prefix)).length !== 1);
    if (unassigned) fail(`Unassigned Core type ${unassigned.fqn}`);
    const unmatched = data.units.find(unit => !classes.some(info => `${info.fqn}.`.startsWith(unit.prefix)));
    if (unmatched) fail(`Unmatched Core unit ${unmatched.prefix}`);
    const result = partitionUnits(data.units, parts);
    assertExactOnce(classes.map(info => info.fqn), result);
    return result;
}
function durationSeconds(duration) {
    const match = /^(\d+):(\d{2}):(\d{2}(?:\.\d+)?)$/.exec(duration ?? '');
    if (!match) fail(`Invalid TRX duration ${duration}`);
    return Number(match[1]) * 3600 + Number(match[2]) * 60 + Number(match[3]);
}
function fileTests(xml, file) {
    const definitions = new Map(nodes(xml, 'UnitTest').map(test => [test.attrs.id, nodes(test, 'TestMethod')[0]?.attrs.className]));
    const results = nodes(xml, 'UnitTestResult');
    if (!results.length) fail(`No tests in ${file}`);
    return results.map(result => {
        const fqn = definitions.get(result.attrs.testId);
        if (!fqn) fail(`Missing TRX class identity in ${file}`);
        return { fqn, weight: durationSeconds(result.attrs.duration) + PER_TEST_SECONDS };
    });
}
export async function trxTests(files) {
    const tests = [];
    for (const file of files) tests.push(...fileTests(parseXml(await readFile(file, 'utf8')), file));
    return tests;
}
function terminalUnit(prefix, classes, weight, threshold) {
    if (weight <= threshold) return true;
    if (prefix.split('.').at(-2).startsWith('for_')) return true;
    return classes.every(info => `${info.fqn}.` === prefix);
}
function splitUnit(prefix, classes, weights, threshold) {
    const weight = classes.reduce((sum, info) => sum + (weights.get(info.fqn) ?? 0), 0);
    if (terminalUnit(prefix, classes, weight, threshold)) return [{ prefix, weight: Math.max(weight, 0.1) }];
    const children = new Map();
    for (const info of classes) {
        const rest = `${info.fqn}.`.slice(prefix.length);
        const child = info.namespace + '.' === prefix ? `${info.fqn}.` : prefix + rest.split('.')[0] + '.';
        children.set(child, [...(children.get(child) ?? []), info]);
    }
    if (children.size === 1 && children.has(prefix)) return [{ prefix, weight: Math.max(weight, 0.1) }];
    return [...children].flatMap(([child, members]) => child === `${members[0].fqn}.`
        ? [{ prefix: child, weight: Math.max(members.reduce((sum, info) => sum + (weights.get(info.fqn) ?? 0), 0), 0.1) }]
        : splitUnit(child, members, weights, threshold));
}
export async function refresh(root, files) {
    const classes = await coverageTypes(root);
    const tests = await trxTests(files);
    const known = new Set(classes.map(info => info.fqn));
    const weights = new Map();
    for (const test of tests) {
        if (!known.has(test.fqn)) fail(`TRX spec class not discovered from source: ${test.fqn}`);
        weights.set(test.fqn, (weights.get(test.fqn) ?? 0) + test.weight);
    }
    const groups = new Map();
    for (const info of classes) {
        const prefix = info.namespace.split('.').length < 3 ? `${info.fqn}.` : info.namespace.split('.').slice(0, 3).join('.') + '.';
        groups.set(prefix, [...(groups.get(prefix) ?? []), info]);
    }
    const threshold = tests.reduce((sum, test) => sum + test.weight, 0) / 8;
    const units = [...groups].flatMap(([prefix, members]) => splitUnit(prefix, members, weights, threshold));
    const total = units.reduce((sum, unit) => sum + unit.weight, 0);
    const data = { version: 1, units: units.map(unit => ({ ...unit, weight: unit.weight / total })).sort((a, b) => ordinal(a.prefix, b.prefix)) };
    const parts = await checkPartition(root, data);
    assertExactOnce(tests.map(test => test.fqn), parts);
    return data;
}
export async function main(args = process.argv.slice(2), root = process.cwd()) {
    let data;
    if (args[0] === '--refresh' && args.length > 1) {
        data = await refresh(root, args.slice(1));
        await writeFile(table, JSON.stringify(data, null, 4) + '\n');
    } else if (args.length === 1 && args[0] === '--check') data = await loadPartition();
    else fail('Usage: core-spec-partition.mjs --check | --refresh <trx...>');
    const parts = await checkPartition(root, data);
    console.log(`Checked ${(await coverageTypes(root)).length} type declarations (${(await discoverSpecClasses(root)).length} detected spec classes); ${data.units.length} units; ${CORE_PARTS} nonempty parts`);
    for (const part of parts) console.log(`Core part ${part.part}: ${part.units.length} units; relative weight ${part.weight.toFixed(6)}`);
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
