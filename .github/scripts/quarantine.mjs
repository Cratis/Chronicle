// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile, writeFile, readdir } from 'node:fs/promises';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';
import path from 'node:path';

const day = 86400000;
const matching = (spec, entry) => spec === entry.spec || spec.startsWith(`${entry.spec}.`);
const active = (entry, now) => Date.parse(`${entry.expires}T00:00:00Z`) + day > now;

function validDate(value) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
    const parsed = Date.parse(`${value}T00:00:00Z`);
    return Number.isFinite(parsed) && new Date(parsed).toISOString().slice(0, 10) === value;
}

function validateText(entry, field) {
    if (typeof entry[field] !== 'string' || !entry[field].trim()) throw new Error(`${field} must be nonempty`);
}

function validateIssue(issue) {
    if (!Number.isSafeInteger(issue) || issue < 1) throw new Error('issue must be a positive number');
}

function validateExpiry(entry, now) {
    if (!validDate(entry.expires)) throw new Error(`invalid expiry: ${entry.expires}`);
    if (!active(entry, now)) throw new Error(`expired quarantine: ${entry.spec}`);
    if (Date.parse(entry.expires) > now + 60 * day) throw new Error(`expiry exceeds 60 days: ${entry.spec}`);
}

function validateEntry(entry, now) {
    for (const field of ['spec', 'owner', 'reason']) validateText(entry, field);
    validateIssue(entry.issue);
    validateExpiry(entry, now);
}

export function validate(config, now = Date.now()) {
    if (!Array.isArray(config.entries)) throw new Error('entries must be an array');
    const seen = new Set();
    for (const entry of config.entries) {
        validateEntry(entry, now);
        if (seen.has(entry.spec)) throw new Error(`duplicate spec: ${entry.spec}`);
        seen.add(entry.spec);
    }
    return config;
}

export function unquarantined(names, config, now = Date.now()) {
    return names.filter(name => !config.entries.some(entry => active(entry, now) && matching(name, entry)));
}

function failedName(result) {
    if (!/outcome=["']Failed["']/.test(result)) return undefined;
    return result.match(/testName=["']([^"']+)["']/)?.[1];
}

function attemptOneLines(xml) {
    const results = xml.match(/<(?:\w+:)?UnitTestResult\b[^>]*>/g) ?? [];
    return results.map(failedName).filter(Boolean).map(name => `attempt-1: ${name}`);
}

async function firstFailures(directory) {
    const lines = [];
    const first = path.join(directory, 'attempt-1');
    for (const file of (await readdir(first)).filter(file => file.endsWith('.trx'))) {
        lines.push(...attemptOneLines(await readFile(path.join(first, file), 'utf8')));
    }
    return lines;
}

function quarantineMessage(name, config) {
    const entry = config.entries.find(item => matching(name, item));
    return `Quarantined ${name}; issue #${entry.issue}; owner ${entry.owner}; expires ${entry.expires}`;
}

function warn(message) {
    // Escape workflow-command data; names/reasons must never inject new commands.
    console.log(`::warning::${message.replaceAll('%', '%25').replaceAll('\r', '%0D').replaceAll('\n', '%0A')}`);
}

async function recordFailures(names, config, directory) {
    const lines = await firstFailures(directory);
    for (const name of names) {
        const message = quarantineMessage(name, config);
        warn(message);
        lines.push(message);
    }
    await writeFile(path.join(directory, 'quarantined-failures.log'), `${lines.join('\n')}\n`);
}

async function readNames() {
    const chunks = [];
    for await (const chunk of process.stdin) chunks.push(chunk);
    return Buffer.concat(chunks).toString().split(/\r?\n/).filter(Boolean);
}

function printNames(names) {
    if (names.length) console.log(names.join('\n'));
}

function requireRecordNames(command, names, config) {
    const message = 'record requires only active quarantined failures';
    if (command !== 'record') throw new Error(message);
    if (!names.length) throw new Error(message);
    if (unquarantined(names, config).length) throw new Error(message);
}

async function main() {
    const { values, positionals } = parseArgs({ allowPositionals: true, options: { 'results-dir': { type: 'string' } } });
    const config = JSON.parse(await readFile('.github/quarantine.json', 'utf8'));
    if (positionals[0] === 'lint') { validate(config); console.log(`Validated ${config.entries.length} quarantine entries`); return; }
    const names = await readNames();
    if (positionals[0] === 'unquarantined') {
        printNames(unquarantined(names, config));
        return;
    }
    requireRecordNames(positionals[0], names, config);
    await recordFailures(names, config, values['results-dir']);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    try { await main(); } catch (error) { console.error(error.message); process.exitCode = 1; }
}
