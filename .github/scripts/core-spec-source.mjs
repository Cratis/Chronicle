// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const fail = message => { throw new Error(`${message}; refresh the Core spec partition`); };
const ordinal = (a, b) => a < b ? -1 : a > b ? 1 : 0;
const tests = /(?:\[|,)\s*(?:global::)?(?:Xunit\.)?(?:Fact|Theory)(?:Attribute)?\b/g;
// Mask literals/comments while retaining offsets and braces outside them.
function declarations(source) {
    return source.replace(/\/\/[^\n]*|\/\*[\s\S]*?\*\/|(?:\$*)"{3,}[\s\S]*?"{3,}|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'/g,
        value => ' '.repeat(value.length));
}
function closingBrace(source, start) {
    let depth = 1;
    for (let index = start + 1; index < source.length; index++) {
        if (source[index] === '{') depth++;
        if (source[index] === '}' && --depth === 0) return index;
    }
    fail('Unclosed C# declaration');
}
function namespaceAt(source, index) {
    const scopes = [...source.matchAll(/\bnamespace\s+([\w.]+)\s*([;{])/g)].filter(match =>
        match.index < index && (match[2] === ';' || closingBrace(source, match.index + match[0].lastIndexOf('{')) > index));
    if (!scopes.length) fail('C# class without namespace');
    return scopes.map(scope => scope[1]).join('.');
}
function classDeclarations(source, file) {
    const text = declarations(source);
    const matches = [...text.matchAll(/\b((?:(?:public|internal|private|protected|abstract|sealed|static|partial|new)\s+)*)class\s+(\w+)(?:<[^>{}]+>)?\s*(?:\([^{}]*?\))?\s*(?::\s*([^{}]+?))?\s*\{/g)];
    const classes = matches.map(match => classDeclaration(text, match, matches, file));
    for (const fact of text.matchAll(tests)) {
        if (!classes.some(info => fact.index > info.start && fact.index < info.end)) fail(`Unrecognized test declaration in ${file}`);
    }
    return classes;
}
function ownBody(text, start, end, matches) {
    let body = text.slice(start + 1, end);
    for (const child of matches.filter(other => other.index > start && other.index < end)) {
        body = body.replace(text.slice(child.index, closingBrace(text, child.index + child[0].lastIndexOf('{')) + 1), '');
    }
    return body;
}
function classDeclaration(text, match, matches, file) {
    const start = match.index + match[0].lastIndexOf('{');
    const end = closingBrace(text, start);
    const parents = matches.filter(other => other.index < match.index && closingBrace(text, other.index + other[0].lastIndexOf('{')) > end);
    const namespace = namespaceAt(text, match.index);
    return { name: [...parents.map(parent => parent[2]), match[2]].join('.'), namespace, file, start, end,
        abstract: /\babstract\b/.test(match[1]), ownTests: [...ownBody(text, start, end, matches).matchAll(tests)].length > 0,
        base: (match[3] ?? '').replace(/<[^<>]*>/g, '').split(/[,\s(]/)[0].replace(/^global::/, ''),
        usings: [...text.matchAll(/\busing\s+([\w.]+)\s*;/g)].map(using => using[1]),
        aliases: new Map([...text.matchAll(/\busing\s+(\w+)\s*=\s*(?:global::)?([\w.]+)\s*;/g)].map(using => [using[1], using[2]])) };
}
async function sourceClasses(directory) {
    const classes = [];
    for (const entry of await readdir(directory, { withFileTypes: true })) {
        if (['bin', 'obj', '.ai-work'].includes(entry.name)) continue;
        const file = path.join(directory, entry.name);
        if (entry.isSymbolicLink()) fail(`Unsupported Core spec source symlink ${file}`);
        if (entry.isDirectory()) classes.push(...await sourceClasses(file));
        else if (entry.name.endsWith('.cs')) classes.push(...classDeclarations(await readFile(file, 'utf8'), file));
    }
    return classes;
}
function baseClasses(info, byName) {
    if (!info.base) return [];
    const [first, ...rest] = info.base.split('.');
    const base = info.aliases.has(first) ? [info.aliases.get(first), ...rest].join('.') : info.base;
    const namespaces = info.namespace.split('.');
    for (let count = namespaces.length; count >= 0; count--) {
        const candidate = [...namespaces.slice(0, count), base].join('.');
        if (byName.has(candidate)) return byName.get(candidate);
    }
    return info.usings.flatMap(using => byName.get(`${using}.${base}`) ?? []);
}
function testedClasses(classes, byName) {
    const tested = new Set(classes.filter(info => info.ownTests).map(info => info.fqn));
    let changed = true;
    while (changed) {
        changed = false;
        for (const info of classes) {
            if (!tested.has(info.fqn) && baseClasses(info, byName).some(base => tested.has(base.fqn))) {
                tested.add(info.fqn);
                changed = true;
            }
        }
    }
    return tested;
}
export async function discoverSpecClasses(root) {
    const classes = await sourceClasses(path.join(root, 'Source/Kernel/Core.Specs'));
    const byName = new Map();
    for (const info of classes) {
        info.fqn = `${info.namespace}.${info.name}`;
        byName.set(info.fqn, [...(byName.get(info.fqn) ?? []), info]);
    }
    // Fixed point includes concrete derived classes with inherited tests, including partial bases.
    const tested = testedClasses(classes, byName);
    const specs = [...new Map(classes.filter(info => !info.abstract && tested.has(info.fqn)).map(info => [info.fqn, info])).values()];
    if (!specs.length) fail(`No Core spec classes under ${root}`);
    if (specs.some(info => info.name.includes('.'))) fail('Nested Core spec classes require explicit adapter name handling');
    return specs.sort((a, b) => ordinal(a.fqn, b.fqn));
}
export async function discoverNamespaces(root) {
    return [...new Set((await discoverSpecClasses(root)).map(info => info.namespace))].sort(ordinal);
}
