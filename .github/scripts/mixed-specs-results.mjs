// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { access, readdir } from 'node:fs/promises';
import path from 'node:path';

const fail = message => { throw new Error(message); };
const entities = { amp: '&', lt: '<', gt: '>', quot: '"', apos: "'" };
function decodeEntity(entity) {
    if (!entity.startsWith('#')) return entities[entity];
    const hex = entity[1] === 'x';
    return String.fromCodePoint(parseInt(entity.slice(hex ? 2 : 1), hex ? 16 : 10));
}
export function decode(text) {
    return text.replace(/&(#x[\da-f]+|#\d+|amp|lt|gt|quot|apos);/gi, (_, entity) => decodeEntity(entity));
}
function xmlAttributes(text) {
    const attributes = {};
    while (text.trim()) {
        const match = /^\s+([\w:.-]+)\s*=\s*("[^"]*"|'[^']*')/.exec(text);
        if (!match) fail('Malformed XML attribute');
        if (Object.hasOwn(attributes, match[1])) fail('Duplicate XML attribute');
        if (/&(?!#x[\da-f]+;|#\d+;|amp;|lt;|gt;|quot;|apos;)/i.test(match[2])) fail('Unknown XML entity');
        attributes[match[1]] = decode(match[2].slice(1, -1));
        text = text.slice(match[0].length);
    }
    return attributes;
}
function closeXml(token, stack) {
    if (!/^<\/[\w:.-]+\s*>$/.test(token)) fail('Malformed XML close');
    if (stack.length === 1) fail('Unexpected XML close');
    if (stack.pop().name !== token.slice(2, -1).trim()) fail('Mismatched XML close');
}
function xmlText(token, stack) {
    if (token.includes('>')) fail('Malformed XML text');
    if (stack.length === 1 && token.trim()) fail('XML text outside root');
}
function xmlTag(token, stack) {
    const match = /^<([\w:.-]+)([\s\S]*?)(\/?)>$/.exec(token);
    if (!match) fail('Malformed XML tag');
    const node = { name: match[1], attrs: xmlAttributes(match[2]), children: [] };
    stack.at(-1).children.push(node);
    if (!match[3]) stack.push(node);
}
function xmlTokens(xml) {
    if (/<!DOCTYPE|<!ENTITY|<!\[CDATA\[/i.test(xml)) fail('Unsupported XML declaration');
    const content = xml.replace(/<\?xml[^?]*\?>/g, '').replace(/<!--[\s\S]*?-->/g, '');
    if (content.replace(/<[^>]*>|[^<]+/g, '').trim()) fail('Malformed XML');
    return { content, tokens: content.match(/<[^>]*>|[^<]+/g) ?? [] };
}
function xmlRoot(root, stack, content) {
    if (stack.length !== 1) fail('Unclosed XML elements');
    if (root.children.length !== 1) fail('Expected one XML root');
    if (!content.trim().endsWith('>')) fail('Incomplete XML');
    return root.children[0];
}
// Minimal strict XML reader for TRX/Cobertura; DTDs/entities are never loaded.
export function parseXml(xml) {
    const { content, tokens } = xmlTokens(xml);
    const root = { name: '#document', children: [] }, stack = [root];
    for (const token of tokens) {
        if (!token.startsWith('<')) xmlText(token, stack);
        else if (token.startsWith('</')) closeXml(token, stack);
        else xmlTag(token, stack);
    }
    return xmlRoot(root, stack, content);
}
const nodes = (node, name) => [...(node.name === name ? [node] : []), ...node.children.flatMap(child => nodes(child, name))];
const number = value => /^\d+$/.test(value ?? '') ? Number(value) : fail('Missing/invalid XML counter');
function validateExecution(c, results) {
    if (number(c.executed) > number(c.total) || number(c.executed) < number(c.passed)) fail('Inconsistent TRX execution counters');
    if (number(c.total) !== results.length) fail('TRX total does not match results');
    if (number(c.executed) === 0) fail('TRX executed no tests');
}
function validateCounters(c, results) {
    if (['timeout', 'aborted', 'inconclusive', 'passedButRunAborted', 'notRunnable', 'disconnected', 'inProgress', 'pending'].some(key => number(c[key] ?? '0') !== 0)) fail('Incomplete TRX outcome counters');
    validateExecution(c, results);
    if (['failed', 'error'].some(key => number(c[key]) !== 0)) fail('Failed TRX');
    if (number(c.passed) === 0 || results.filter(x => x.attrs.outcome === 'Passed').length !== number(c.passed)) fail('No passing/mismatched TRX results');
}
function validateDefinition(id, definitions) {
    const matches = definitions.filter(x => x.attrs.id === id);
    if (matches.length !== 1) fail('Missing TRX definition');
    const methods = nodes(matches[0], 'TestMethod');
    if (methods.length !== 1) fail('Missing TRX method');
    if (!methods[0].attrs.className || !methods[0].attrs.name) fail('Missing TRX method identity');
}
function validateIdentity(result, definitions, seen) {
    const id = result.attrs.testId;
    if (![id, result.attrs.testName].every(Boolean)) fail('Missing TRX identity');
    if (seen.has(id)) fail('Duplicate TRX identity');
    if (!['Passed', 'NotExecuted'].includes(result.attrs.outcome)) fail('Invalid TRX outcome');
    seen.add(id);
    validateDefinition(id, definitions);
}
export function validateTrx(xml) {
    const root = parseXml(xml);
    if (root.name !== 'TestRun' || root.attrs.xmlns !== 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010') fail('Unsupported TRX namespace');
    const results = nodes(root, 'UnitTestResult'), definitions = nodes(root, 'UnitTest');
    const counters = nodes(root, 'Counters');
    if (counters.length !== 1 || !results.length) fail('Missing TRX results/counters');
    validateCounters(counters[0].attrs, results);
    const seen = new Set();
    results.forEach(result => validateIdentity(result, definitions, seen));
    return results.map(x => x.attrs.testName);
}
function productionFile(filename) {
    const normalized = filename.replaceAll('\\', '/');
    const relative = normalized.startsWith('Source/') ? normalized : normalized.match(/(?:^|\/)Source\/(.+)$/)?.[0].replace(/^\//, '');
    if (!relative) return;
    if (!relative.endsWith('.cs')) return;
    const excluded = part => ['..', 'obj', 'bin'].includes(part) || /(?:\.Specs$|^Integration$|^given$|^when_|^for_)/.test(part);
    if (!relative.split('/').some(excluded)) return relative;
}
export async function validateCoverage(xml, root) {
    const coverage = parseXml(xml);
    if (coverage.name !== 'coverage' || !nodes(coverage, 'package').length) fail('Empty/missing Cobertura packages');
    for (const item of nodes(coverage, 'package').flatMap(item => nodes(item, 'class'))) {
        const relative = productionFile(item.attrs.filename ?? '');
        if (!relative) continue;
        if (!nodes(item, 'line').some(x => number(x.attrs.hits) > 0)) continue;
        await access(path.join(root, relative));
        return relative;
    }
    fail('Cobertura has no actual production-source hits');
}
export async function coverageFiles(directory) {
    const entries = await readdir(directory, { withFileTypes: true });
    return (await Promise.all(entries.map(entry => entry.isDirectory() ? coverageFiles(path.join(directory, entry.name)) : entry.name === 'coverage.cobertura.xml' ? [path.join(directory, entry.name)] : []))).flat();
}
