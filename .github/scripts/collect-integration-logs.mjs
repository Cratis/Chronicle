// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { isUtf8 } from 'node:buffer';
import { spawnSync } from 'node:child_process';
import { mkdir, readdir, readFile, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

// No gitleaks action is used by this repository. Conservatively omit the whole file on a match;
// never print matched text, docker output, or exception messages from untrusted logs.
const rules = [
    ['private-key', /-----BEGIN (?:[A-Z]+ )*PRIVATE KEY-----/i],
    ['credential-assignment', /\b(?:[\w-]{0,64}(?:password|passwd|pwd|secret|token)|api[_-]?key|access[_-]?key|accountkey)[\w-]{0,32}\b["']?\s*[:=]\s*["']?[^\s"';,<}\]]+/i],
    ['authorization', /\b(?:bearer|basic)\s+[a-z0-9+/_.=-]+/i],
    ['uri-credentials', /[a-z][a-z0-9+.-]{0,31}:\/\/[^\s/@]+:[^\s/@]+@/i],
    ['provider-token', /\b(?:gh[pousr]_[a-z0-9_]+|github_pat_[a-z0-9_]+|AKIA[A-Z0-9]{16}|xox[baprs]-[a-z0-9-]+)\b/i],
    ['jwt', /\beyJ[a-z0-9_-]+\.[a-z0-9_-]+\.[a-z0-9_-]+/i],
    ['signed-url', /[?&](?:sig|signature|x-amz-signature)=[^\s&"'<>]+/i],
    ['xml-credential', /<(?:[\w-]{0,64}(?:password|passwd|pwd|secret|token)|api[_-]?key|access[_-]?key|accountkey)[\w-]{0,32}\b[^>]*>\s*[^\s<]+/i]
];
const maxBytes = 32 * 1024 * 1024;
const maxTotalBytes = 500 * 1024 * 1024;
const containerLogBytes = logs => Buffer.concat([logs.stdout, Buffer.from('\n'), logs.stderr]);

export function sizeRule(bytes, retainedBytes) {
    return bytes > maxBytes ? 'size-limit' : retainedBytes + bytes > maxTotalBytes ? 'total-size-limit' : undefined;
}

export function selfTest() {
    const violations = [
        ['private-key', '-----BEGIN PRIVATE KEY-----'],
        ['credential-assignment', 'Password=example-value;'],
        ['credential-assignment', 'MSSQL_SA_PASSWORD=example-value'],
        ['credential-assignment', '"CertificatePassword": "example-value"'],
        ['credential-assignment', 'DefaultAdminPassword=example-value'],
        ['xml-credential', '<CertificatePassword>example-value</CertificatePassword>'],
        ['credential-assignment', '"ClientSecret": "example-value"'],
        ['credential-assignment', '{\\"api_key\\":\\"example-value\\"}'],
        ['credential-assignment', '&quot;token&quot;: &quot;example-value&quot;'],
        ['credential-assignment', 'password&#61;example-value'],
        ['credential-assignment', 'password1=example-value'],
        ['credential-assignment', 'PasswordHash: example-value'],
        ['xml-credential', '<SecretKey>example-value</SecretKey>'],
        ['credential-assignment', 'token1=example-value'],
        ['credential-assignment', 'api_key1: example-value'],
        ['credential-assignment', 'AccessKeyId=example-value'],
        ['xml-credential', '<Token1>example-value</Token1>'],
        ['xml-credential', '<ApiKey2>example-value</ApiKey2>'],
        ['xml-credential', '<AccountKey>example-value</AccountKey>'],
        ['authorization', 'Authorization: Bearer example-value'],
        ['uri-credentials', 'mongodb://example:example@localhost/'],
        ['provider-token', 'ghp_examplevalue'],
        ['jwt', 'eyJleGFtcGxl.example.example'],
        ['signed-url', 'https://example.invalid/?sig=example-value'],
        ['xml-credential', '<Password>example-value</Password>'],
        ['xml-credential', '<ClientSecret>example-value</ClientSecret>']
    ];
    for (const [rule, text] of violations) assert.equal(secretRule(text), rule, `Missed rule: ${rule}`);
    assert.equal(secretRule('Information CatchUpObserver prepared no job steps'), undefined);
    assert.equal(secretRule('a-'.repeat(20000)), undefined);
    assert.equal(secretRule(`${'a-'.repeat(20000)}password=example-value`), 'credential-assignment');
    assert.equal(secretRule(containerLogBytes({ stdout: Buffer.from('no-newline'), stderr: Buffer.from('Bearer example-value') }).toString()), 'authorization');
    assert.equal(sizeRule(maxBytes + 1, 0), 'size-limit');
    assert.equal(sizeRule(maxBytes, maxTotalBytes - maxBytes), undefined);
    assert.equal(sizeRule(1, maxTotalBytes), 'total-size-limit');
    console.log(`Secret-screen self-test checked ${violations.length} planted violations and one clean diagnostic.`);
}

export function secretRule(text) {
    // TRX escapes XML, and captured console output can contain escaped JSON quotes.
    const normalized = text.replaceAll('&quot;', '"').replaceAll('&apos;', "'")
        .replaceAll('&amp;', '&').replaceAll('\\"', '"')
        .replace(/&#(?:x([0-9a-f]+)|(\d+));/gi, (_, hex, decimal) =>
            String.fromCodePoint(Math.min(parseInt(hex ?? decimal, hex ? 16 : 10), 0x10ffff)));
    return rules.find(([, pattern]) => pattern.test(normalized))?.[0];
}

async function logFiles(directory) {
    let entries;
    try { entries = await readdir(directory, { withFileTypes: true }); }
    catch (error) { if (error.code === 'ENOENT') return []; throw error; }
    const files = [];
    for (const entry of entries) {
        const file = path.join(directory, entry.name);
        if (entry.isDirectory() && !['obj', 'node_modules', 'backups', '.git', '.ai-work'].includes(entry.name)) files.push(...await logFiles(file));
        if (entry.isFile() && /\.(?:log|trx)$/i.test(entry.name)) files.push(file);
    }
    return files;
}

export async function collect({ root, output, docker = spawnSync, report = console.log }) {
    // Must be a fresh directory: stale or partially scanned files must never be uploaded.
    await mkdir(output);
    let examined = 0, retained = 0, retainedBytes = 0;
    // Names are printed as JSON so file names cannot inject workflow commands or new lines.
    const omit = (name, rule) => report(`Omitted file=${JSON.stringify(name)} rule=${rule}`);
    const screen = async (name, bytes) => {
        examined++;
        // Recheck actual bytes too, in case a file grew between stat and readFile.
        const rule = sizeRule(bytes.length, retainedBytes) ?? (bytes.includes(0) ? 'binary-log' :
            !isUtf8(bytes) ? 'invalid-utf8' : secretRule(bytes.toString('utf8')));
        if (rule) {
            omit(name, rule);
            return;
        }
        const destination = path.join(output, name);
        await mkdir(path.dirname(destination), { recursive: true });
        await writeFile(destination, bytes);
        retained++;
        retainedBytes += bytes.length;
    };
    const directories = ['Integration', 'Source/Clients/XUnit.Integration', 'Source/Clients/XUnit.Integration.v3', 'coverage-results'];
    const files = (await Promise.all(directories.map(directory => logFiles(path.join(root, directory))))).flat();
    // The xUnit runner uses the checkout as cwd; AddFile's timestamped logs land here.
    for (const entry of await readdir(root, { withFileTypes: true })) {
        if (entry.isFile() && /\.log$/i.test(entry.name)) files.push(path.join(root, entry.name));
    }
    for (const file of files) {
        const name = path.relative(root, file);
        const rule = sizeRule((await stat(file)).size, retainedBytes);
        if (rule) {
            examined++;
            omit(name, rule);
            continue;
        }
        await screen(name, await readFile(file));
    }

    // DisposeAsync normally removes Testcontainers. Capture any survivors after a crash, including
    // stopped containers, without inspecting environment/configuration (which can contain secrets).
    const options = { timeout: 10000, maxBuffer: maxBytes, stdio: ['ignore', 'pipe', 'pipe'] };
    const listed = docker('docker', ['ps', '-aq', '--no-trunc'], options);
    if (listed.status !== 0) {
        report('Container logs unavailable: docker-list-failed (no docker output printed).');
    } else {
        const ids = listed.stdout.toString().trim().split(/\s+/).filter(Boolean);
        if (ids.length > 100 || ids.some(id => !/^[a-f0-9]{64}$/.test(id))) throw new Error('Invalid container inventory');
        report(`Remaining containers: ${ids.length}; disposed Testcontainers cannot be recovered.`);
        for (const id of ids) {
            const logs = docker('docker', ['logs', '--timestamps', id], options);
            if (logs.status !== 0) {
                report(`Omitted file="containers/${id}.log" rule=docker-logs-failed`);
                continue;
            }
            await screen(`containers/${id}.log`, containerLogBytes(logs));
        }
    }
    report(`Examined ${examined} log files; retained ${retained} (${retainedBytes}/${maxTotalBytes} bytes).`);
    if (!examined) report('No log files available; no diagnostic artifact will be uploaded.');
    return { examined, retained };
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
    if (process.argv.includes('--self-test')) selfTest();
    else {
        selfTest();
        collect({ root: process.cwd(), output: path.join(process.env.RUNNER_TEMP, 'integration-logs') })
            .catch(() => { console.error('Log collection/screening failed; diagnostic upload must be skipped.'); process.exitCode = 2; });
    }
}
