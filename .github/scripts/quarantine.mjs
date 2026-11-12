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

function validateEntry(entry, now) {
    for (const field of ['spec', 'owner', 'reason']) {
        if (typeof entry[field] !== 'string' || !entry[field].trim()) throw new Error(`${field} must be nonempty`);
    }
    if (!Number.isSafeInteger(entry.issue) || entry.issue < 1) throw new Error('issue must be a positive number');
    if (!validDate(entry.expires)) throw new Error(`invalid expiry: ${entry.expires}`);
    if (!active(entry, now)) throw new Error(`expired quarantine: ${entry.spec}`);
    if (Date.parse(entry.expires) > now + 60 * day) throw new Error(`expiry exceeds 60 days: ${entry.spec}`);
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

async function recordFailures(names, config, directory) {
    const lines = [];
    const first = path.join(directory, 'attempt-1');
    for (const file of (await readdir(first)).filter(file => file.endsWith('.trx'))) {
        const xml = await readFile(path.join(first, file), 'utf8');
        const results = xml.match(/<(?:\w+:)?UnitTestResult\b[^>]*>/g) ?? [];
        for (const result of results) {
            if (!/outcome=["']Failed["']/.test(result)) continue;
            const name = result.match(/testName=["']([^"']+)["']/)?.[1];
            if (name) lines.push(`attempt-1: ${name}`);
        }
    }
    for (const name of names) {
        const entry = config.entries.find(item => matching(name, item));
        const message = `Quarantined ${name}; issue #${entry.issue}; owner ${entry.owner}; expires ${entry.expires}`;
        // Escape workflow-command data; names/reasons must never inject new commands.
        console.log(`::warning::${message.replaceAll('%', '%25').replaceAll('\r', '%0D').replaceAll('\n', '%0A')}`);
        lines.push(message);
    }
    await writeFile(path.join(directory, 'quarantined-failures.log'), `${lines.join('\n')}\n`);
}

async function main() {
    const { values, positionals } = parseArgs({ allowPositionals: true, options: { 'results-dir': { type: 'string' } } });
    const config = JSON.parse(await readFile('.github/quarantine.json', 'utf8'));
    if (positionals[0] === 'lint') { validate(config); console.log(`Validated ${config.entries.length} quarantine entries`); return; }
    const chunks = [];
    for await (const chunk of process.stdin) chunks.push(chunk);
    const names = Buffer.concat(chunks).toString().split(/\r?\n/).filter(Boolean);
    if (positionals[0] === 'unquarantined') {
        const remaining = unquarantined(names, config);
        if (remaining.length) console.log(remaining.join('\n'));
        return;
    }
    if (positionals[0] !== 'record' || !names.length || unquarantined(names, config).length) throw new Error('record requires only active quarantined failures');
    await recordFailures(names, config, values['results-dir']);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    try { await main(); } catch (error) { console.error(error.message); process.exitCode = 1; }
}
