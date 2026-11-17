// cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/scripts/score-fidelity.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, realpathSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

export const DEFAULT_WEIGHTS = { rule: 0.25, rejection: 0.25, flow: 0.25, time: 0.1, authorization: 0.1, 'query-shape': 0.05 };
export const CLASS_ITEM_WEIGHTS = { rule: 1, rejection: 1, flow: 1, time: 1.5, authorization: 1, 'query-shape': 1 };
export const NAMING_WEIGHTS = { commands: 0.2, events: 0.15, readModels: 0.15, screens: 0.15, constraints: 0.15, concepts: 0.1, specifications: 0.03, handWrittenRules: 0.07 };
const STRUCTURE = ['commands', 'events', 'readModels', 'screens', 'concepts'];
const RULES = ['constraints', 'handWrittenRules', 'specifications'];
const MODEL_SCORE = { expressed: 1, partial: 0.5, missing: 0 };
const EXECUTED_SCORE = { pass: 1, fail: 0 };
// Legacy structure and rules score a renamed item as found; naming scores it as not matching.
const LEGACY_RESULT = { exact: 1, renamed: 1, partial: 0.5, missing: 0 };
const NAMING_RESULT = { exact: 1, renamed: 0, partial: 0.5, missing: 0 };
const TOLERANCE = 1e-6;

export class CatalogError extends Error {}

const round = value => Math.round(value * 10000) / 10000;

function weightedMean(parts) {
    const total = parts.reduce((sum, part) => sum + part.weight, 0);
    return total === 0 ? null : parts.reduce((sum, part) => sum + part.weight * part.score, 0) / total;
}

const fail = message => { throw new CatalogError(message); };
const knownExecuted = value => value === undefined || value === 'not-run' || value in EXECUTED_SCORE;

function validateItem(item, weights) {
    if (!(item.class in weights)) fail(`${item.id}: class "${item.class}" has no weight`);
    if (!(item.status in MODEL_SCORE)) fail(`${item.id}: unknown status "${item.status}"`);
    if (!knownExecuted(item.executed)) fail(`${item.id}: unknown executed "${item.executed}"`);
    if (item.weight !== undefined && !(item.weight > 0)) fail(`${item.id}: weight must be positive`);
}

function validateCatalog(catalog) {
    if (!Array.isArray(catalog?.items) || catalog.items.length === 0) fail('catalog needs a non-empty "items" array');
    const weights = catalog.weights ?? DEFAULT_WEIGHTS;
    const sum = Object.values(weights).reduce((total, value) => total + value, 0);
    if (Math.abs(sum - 1) > TOLERANCE) fail(`class weights sum to ${sum}, not 1`);
    const ids = catalog.items.map(item => item.id);
    if (ids.some(id => !id) || new Set(ids).size !== ids.length) fail('item ids must be present and unique');
    catalog.items.forEach(item => validateItem(item, weights));
    const empty = Object.entries(weights).find(([name, weight]) => weight > 0 && !catalog.items.some(item => item.class === name));
    if (empty) fail(`class "${empty[0]}" has weight ${empty[1]} but no items`);
    return weights;
}

function behaviorScores(catalog, weights) {
    const byClass = {};
    for (const name of Object.keys(weights)) {
        const items = catalog.items.filter(item => item.class === name);
        if (items.length === 0) continue;
        const parts = executed => items.map(item => ({
            weight: item.weight ?? CLASS_ITEM_WEIGHTS[name] ?? 1,
            score: executed && item.executed in EXECUTED_SCORE ? EXECUTED_SCORE[item.executed] : MODEL_SCORE[item.status]
        }));
        byClass[name] = { items: items.length, model: weightedMean(parts(false)), executed: weightedMean(parts(true)) };
    }
    const total = key => Object.entries(byClass).reduce((sum, [name, value]) => sum + weights[name] * value[key], 0);
    const activeWeight = Object.keys(byClass).reduce((sum, name) => sum + weights[name], 0);
    const notRun = catalog.items.filter(item => !(item.executed in EXECUTED_SCORE)).length;
    return {
        byClass,
        behaviorModel: total('model') / activeWeight,
        behaviorExecuted: total('executed') / activeWeight,
        items: catalog.items.length,
        notRun,
        executedFailedButExpressed: catalog.items.filter(item => item.status === 'expressed' && item.executed === 'fail').map(item => item.id),
        assemblySourced: catalog.items.filter(item => item.source === 'assembly').length
    };
}

function categoryScores(items, table, weights, categories) {
    const parts = [];
    for (const category of categories) {
        const own = items.filter(item => item.category === category);
        if (own.length === 0) continue;
        parts.push({ weight: weights[category], score: own.reduce((sum, item) => sum + table[item.result], 0) / own.length });
    }
    return weightedMean(parts);
}

function namingScores(naming) {
    const weights = naming.weights ?? NAMING_WEIGHTS;
    for (const item of naming.items ?? []) {
        if (!(item.category in weights)) throw new CatalogError(`naming: unknown category "${item.category}"`);
        if (!(item.result in LEGACY_RESULT)) throw new CatalogError(`naming: unknown result "${item.result}" for ${item.name}`);
    }
    const all = naming.items ?? [];
    const nonLeaked = all.filter(item => !item.leaked);
    const categories = Object.keys(weights);
    return {
        legacyStructure: categoryScores(all, LEGACY_RESULT, weights, STRUCTURE),
        legacyRules: categoryScores(all, LEGACY_RESULT, weights, RULES),
        namingAll: categoryScores(all, NAMING_RESULT, weights, categories),
        namingNonLeaked: categoryScores(nonLeaked, NAMING_RESULT, weights, categories),
        leakShare: all.length === 0 ? null : all.filter(item => item.leaked).length / all.length,
        namingItems: all.length,
        namingNonLeakedItems: nonLeaked.length
    };
}

export function score(catalog, naming) {
    const weights = validateCatalog(catalog);
    const result = { behavior: behaviorScores(catalog, weights) };
    if (naming) result.naming = namingScores(naming);
    return result;
}

function format(result) {
    const fixed = value => (value === null || value === undefined ? 'n/a' : value.toFixed(2));
    const { behavior, naming } = result;
    const lines = [
        `Behavior (model read): ${fixed(behavior.behaviorModel)}`,
        `Behavior (executed where run): ${fixed(behavior.behaviorExecuted)}; not-run items: ${behavior.notRun}/${behavior.items}`,
        ...Object.entries(behavior.byClass).map(([name, value]) => `  ${name}: ${value.items} items, model ${fixed(value.model)}, executed ${fixed(value.executed)}`)
    ];
    if (behavior.executedFailedButExpressed.length > 0) lines.push(`Expressed but failed when executed: ${behavior.executedFailedButExpressed.join(', ')}`);
    if (behavior.assemblySourced > 0) lines.push(`Items sourced from assembly: ${behavior.assemblySourced}`);
    if (naming) {
        lines.push(
            `Structure (legacy): ${fixed(naming.legacyStructure)}`,
            `Rules (legacy): ${fixed(naming.legacyRules)}`,
            `Naming (all): ${fixed(naming.namingAll)}`,
            `Naming (non-leaked): ${fixed(naming.namingNonLeaked)} over ${naming.namingNonLeakedItems} of ${naming.namingItems} items`,
            `Leak share: ${fixed(naming.leakShare)}`);
    }
    return lines.join('\n');
}

function roundDeep(value) {
    if (typeof value === 'number') return round(value);
    if (Array.isArray(value)) return value.map(roundDeep);
    if (value && typeof value === 'object') return Object.fromEntries(Object.entries(value).map(([key, inner]) => [key, roundDeep(inner)]));
    return value;
}

function parseArguments(args) {
    const options = {};
    for (let index = 0; index < args.length; index++) {
        if (args[index] === '--json') options.json = true;
        else if (args[index] === '--self-test') options.selfTest = true;
        else if (args[index] === '--naming' && args[index + 1]) options.naming = args[++index];
        else if (!args[index].startsWith('--') && !options.catalog) options.catalog = args[index];
        else throw new CatalogError('Usage: node score-fidelity.mjs <catalog.json> [--naming <naming.json>] [--json] | --self-test');
    }
    return options;
}

function readCatalog(path) {
    try {
        return JSON.parse(readFileSync(path, 'utf8'));
    } catch (error) {
        throw new CatalogError(`cannot read ${path}: ${error.message}`);
    }
}

function writeScore(options) {
    const result = score(readCatalog(options.catalog), options.naming ? readCatalog(options.naming) : undefined);
    console.log(options.json ? JSON.stringify(roundDeep(result)) : format(result));
}

async function main() {
    try {
        const options = parseArguments(process.argv.slice(2));
        if (options.selfTest) {
            const { selfTest } = await import('./score-fidelity-self-test.mjs');
            selfTest(score, CatalogError);
            return;
        }
        if (!options.catalog) throw new CatalogError('Usage: node score-fidelity.mjs <catalog.json> [--naming <naming.json>] [--json] | --self-test');
        writeScore(options);
    } catch (error) {
        console.error(`score-fidelity: ${error.message}`);
        process.exitCode = error instanceof CatalogError ? 2 : 1;
    }
}

if (process.argv[1] && realpathSync(process.argv[1]) === realpathSync(fileURLToPath(import.meta.url))) await main();
