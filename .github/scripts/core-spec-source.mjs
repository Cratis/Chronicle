// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const fail = message => { throw new Error(`${message}; refresh the Core spec partition`); };
const ordinal = (a, b) => a < b ? -1 : a > b ? 1 : 0;
const tests = /(?:\[|,)\s*(?:global::)?(?:Xunit\.)?(?:\w*Fact|\w*Theory|Test|TestCase|TestMethod)(?:Attribute)?\b/g;
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
    if (!scopes.length) fail('C# type without namespace');
    return scopes.map(scope => scope[1]).join('.');
}
// Balanced removal handles nested generic arguments as well as primary constructors.
function withoutArguments(header) {
    let depth = 0, result = '';
    for (const char of header) {
        if ('<(['.includes(char)) depth++;
        else if ('>)]'.includes(char)) depth--;
        else if (depth === 0) result += char;
    }
    if (depth !== 0) fail('Unbalanced C# type header');
    return result;
}
function typeHeader(text, match) {
    const from = match.index + match[0].length;
    const end = text.slice(from).search(/[;{]/);
    if (end < 0) fail('Unterminated C# type declaration');
    const start = from + end;
    const header = text.slice(from, start);
    const signature = withoutArguments(header).split(/\bwhere\b/)[0];
    const bases = (signature.match(/^\s*:\s*(.*)$/s)?.[1] ?? '').split(',').map(base => base.trim().replace(/global::/g, '')).filter(Boolean);
    const parameters = header.match(/^\s*<([^<>]*)>/)?.[1];
    const name = match[3].replace(/^@/, '');
    return { start, end: text[start] === '{' ? closingBrace(text, start) : start, bases, header,
        name, runtimeName: parameters ? `${name}\`${parameters.split(',').length}` : name };
}
function ownBody(text, info, types) {
    let body = text.slice(info.start + 1, info.end);
    for (const child of types.filter(other => other.index > info.start && other.index < info.end)) {
        body = body.replace(text.slice(child.index, child.end + 1), '');
    }
    return body;
}
function rejectUnsupportedDeclarations(text, file) {
    const heads = text.matchAll(/(?<![@\w])(?:class|record|struct|interface|enum|namespace)\s+(@?[\p{ID_Start}_][\p{ID_Continue}]*(?:\.@?[\p{ID_Start}_][\p{ID_Continue}]*)*)/gu);
    if ([...heads].some(head => /[^\x00-\x7f]/.test(head[1]))) fail(`Unsupported Unicode type/namespace declaration in ${file}`);
    if (/(?<![@\w])namespace\s+[^;{]*@/.test(text)) fail(`Unsupported escaped namespace in ${file}`);
    if (/\bdelegate\s+(?![({])\S/.test(text)) fail(`Unsupported named delegate declaration in ${file}`);
}
function typeDeclarations(source, file) {
    const text = declarations(source);
    rejectUnsupportedDeclarations(text, file);
    const pattern = /(?<![@\w])((?:(?:public|internal|private|protected|abstract|sealed|static|partial|new|readonly|ref|file)\s+)*)(class|record(?:\s+(?:class|struct))?|struct|interface|enum)\s+(@?\w+)/g;
    const types = [...text.matchAll(pattern)].map(match => ({ ...typeHeader(text, match), index: match.index,
        abstract: /\babstract\b/.test(match[1]), kind: match[2] }));
    const infos = types.map(info => typeInfo(text, info, types, file));
    for (const fact of text.matchAll(tests)) {
        if (!infos.some(info => fact.index > info.start && fact.index < info.end)) fail(`Unrecognized test declaration in ${file}`);
    }
    return infos;
}
function typeInfo(text, info, types, file) {
    const parents = types.filter(other => other.index < info.index && other.end > info.end);
    const namespace = namespaceAt(text, info.index);
    const name = [...parents.map(parent => parent.name), info.name].join('.');
    const runtimeName = [...parents.map(parent => parent.runtimeName), info.runtimeName].join('+');
    const body = ownBody(text, info, types);
    const aliases = [...text.matchAll(/\busing\s+(\w+)\s*=\s*(?:global::)?([\w.]+)\s*;/g)].map(using => [using[1], using[2]]);
    const baseNames = info.header.match(/\b[A-Za-z_]\w*\b/g) ?? [];
    return { ...info, name, namespace, file, fqn: `${namespace}.${runtimeName}`, lookupName: `${namespace}.${name}`,
        ownTests: [...body.matchAll(tests)].length > 0,
        testSignals: [...text.slice(info.start + 1, info.end).matchAll(tests)].length > 0 || /\b(?:should_|test_)\w*\s*\(/.test(body),
        baseNames, aliasNames: aliases.filter(([name]) => baseNames.includes(name)).flatMap(([, target]) => target.split('.')),
        usings: [...text.matchAll(/\busing\s+([\w.]+)\s*;/g)].map(using => using[1]),
        aliases: new Map(aliases) };
}
async function sourceTypes(directory) {
    const types = [];
    for (const entry of await readdir(directory, { withFileTypes: true })) {
        if (['bin', 'obj', '.ai-work'].includes(entry.name)) continue;
        const file = path.join(directory, entry.name);
        if (entry.isSymbolicLink()) fail(`Unsupported Core spec source symlink ${file}`);
        if (entry.isDirectory()) types.push(...await sourceTypes(file));
        else if (entry.name.endsWith('.cs')) types.push(...typeDeclarations(await readFile(file, 'utf8'), file));
    }
    return types;
}
function resolveBase(info, base, byName) {
    const [first, ...rest] = base.split('.');
    if (info.aliases.has(first)) base = [info.aliases.get(first), ...rest].join('.');
    // Lexical enclosing types come before namespace scopes (Outer.when_x : Base).
    const scopes = info.lookupName.split('.').slice(0, -1);
    for (let count = scopes.length; count >= 0; count--) {
        const candidate = [...scopes.slice(0, count), base].join('.');
        if (byName.has(candidate)) return byName.get(candidate);
    }
    return info.usings.flatMap(using => byName.get(`${using}.${base}`) ?? []);
}
function testedTypes(types) {
    const byName = new Map();
    for (const info of types) byName.set(info.lookupName, [...(byName.get(info.lookupName) ?? []), info]);
    const tested = new Set(types.filter(info => info.ownTests).map(info => info.fqn));
    let changed = true;
    while (changed) {
        changed = false;
        for (const info of types) {
            if (!tested.has(info.fqn) && info.bases.flatMap(base => resolveBase(info, base, byName)).some(base => tested.has(base.fqn))) {
                tested.add(info.fqn);
                changed = true;
            }
        }
    }
    return tested;
}
export async function discoverTypes(root) {
    const types = await sourceTypes(path.join(root, 'Source/Kernel/Core.Specs'));
    if (!types.length) fail(`No Core source types under ${root}`);
    return types.sort((a, b) => ordinal(a.fqn, b.fqn));
}
export async function discoverSpecClasses(root) {
    const types = await discoverTypes(root);
    const tested = testedTypes(types);
    const specs = [...new Map(types.filter(info => !info.abstract && tested.has(info.fqn)).map(info => [info.fqn, info])).values()];
    if (!specs.length) fail(`No Core spec classes under ${root}`);
    return specs;
}
export async function discoverNamespaces(root) {
    return [...new Set((await discoverSpecClasses(root)).map(info => info.namespace))].sort(ordinal);
}
