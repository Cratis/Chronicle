// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';
import { restTransport, paginate } from './nightly-build-health.mjs';
import { findTracker, isBot, safeText } from './last-green-watchdog.mjs';

const decode = value => value.replaceAll('&quot;', '"').replaceAll('&apos;', "'").replaceAll('&lt;', '<').replaceAll('&gt;', '>').replaceAll('&amp;', '&');

function failedNames(xml) {
    const results = xml.match(/<(?:\w+:)?UnitTestResult\b[^>]*>/g) ?? [];
    return results.flatMap(result => {
        if (!/\boutcome=["']Failed["']/.test(result)) return [];
        const name = result.match(/\btestName=(?:"([^"]*)"|'([^']*)')/);
        return name ? [decode(name[1] ?? name[2])] : [];
    });
}

async function scan(directory, lane) {
    for (const file of await readdir(directory, { withFileTypes: true })) {
        const full = path.join(directory, file.name);
        if (file.isDirectory()) await scan(full, lane);
        else if (file.isFile() && file.name.endsWith('.trx')) {
            const attempt = Number(full.match(/[/\\]attempt-(\d+)[/\\]/)?.[1] ?? 1);
            const names = failedNames(await readFile(full, 'utf8'));
            lane.attempts.set(attempt, [...new Set([...(lane.attempts.get(attempt) ?? []), ...names])].sort());
            lane.files++;
        } else if (file.isFile() && file.name === 'quarantined-failures.log') {
            lane.quarantined = (await readFile(full, 'utf8')).split(/\r?\n/).filter(line => line.startsWith('Quarantined '));
        }
    }
}

export async function collect(root) {
    const lanes = [];
    for (const file of await readdir(root, { withFileTypes: true })) {
        if (!file.isDirectory() || !file.name.startsWith('coverage-results-')) continue;
        const lane = { name: file.name, attempts: new Map(), files: 0, quarantined: [] };
        await scan(path.join(root, file.name), lane);
        lanes.push(lane);
    }
    return lanes.sort((a, b) => a.name.localeCompare(b.name));
}

function namesSection(label, names, artifact, cap) {
    const displayed = names.slice(0, cap);
    const lines = [label, ...displayed.map(name => `- ${safeText(name)}`)];
    if (!names.length) lines.push('- none recorded');
    if (names.length > displayed.length) lines.push(`+${names.length - displayed.length} more; see ${artifact}`);
    return lines.join('\n');
}

function laneSection(lane, full) {
    const artifact = `artifact ${safeText(lane.name)}`;
    if (!lane.files) return `### ${artifact}\nno TRX found for ${safeText(lane.name)}\n`;
    const attempts = [...lane.attempts.keys()].sort((a, b) => a - b);
    const first = lane.attempts.get(1) ?? [];
    const last = lane.attempts.get(attempts.at(-1)) ?? [];
    return [`### ${artifact}`, namesSection('failed on attempt 1:', first, artifact, full ? Infinity : 100), namesSection('still failing after the last attempt:', last, artifact, full ? Infinity : 75), namesSection('quarantined-but-recorded (also retained in raw TRX):', lane.quarantined ?? [], artifact, full ? Infinity : 25)].join('\n\n');
}

export function render(lanes, { repo, runId, attempt }, { full = false } = {}) {
    const url = `https://github.com/${repo}/actions/runs/${Number(runId)}`;
    let body = `<!-- failed-specs:run-${Number(runId)}-attempt-${Number(attempt)} -->\n## Failed specs: [run ${Number(runId)}, attempt ${Number(attempt)}](${url})\n\nFull TRX and coverage artifacts: ${url}#artifacts\n\n`;
    if (!lanes.length) body += 'no TRX found (no coverage-results-* artifacts downloaded)\n';
    for (let i = 0; i < lanes.length; i++) {
        const section = `${laneSection(lanes[i], full)}\n\n`;
        if (!full && body.length + section.length > 60000) {
            body += `+${lanes.length - i} more lanes; see coverage-results-* artifacts at ${url}#artifacts\n`;
            break;
        }
        body += section;
    }
    return body;
}

const positiveInteger = value => Number.isSafeInteger(Number(value)) && Number(value) > 0;

function validateContext(repo, runId, attempt) {
    if (!/^[\w.-]+\/[\w.-]+$/.test(repo)) throw new Error('invalid-repository');
    if (![runId, attempt].every(positiveInteger)) throw new Error('invalid-run-context');
}

export async function record({ root, out, repo, runId, attempt, transport }) {
    validateContext(repo, runId, attempt);
    const lanes = await collect(root);
    const body = render(lanes, { repo, runId, attempt });
    // The durable artifact is complete even when GitHub's comment-size cap is reached.
    await writeFile(out, render(lanes, { repo, runId, attempt }, { full: true }));
    const api = `/repos/${repo}`;
    const issue = await findTracker(transport, api);
    const comments = await paginate(transport, `${api}/issues/${issue.number}/comments?per_page=100&page=1`);
    const marker = body.split('\n')[0];
    if (!comments.some(comment => isBot(comment) && comment.body?.startsWith(marker))) {
        await transport('POST', `${api}/issues/${issue.number}/comments`, { body });
    }
    return { status: 'ok', body };
}

export async function main({ environment = process.env, argv = process.argv.slice(2), fetchImpl = globalThis.fetch, output = console.log } = {}) {
    try {
        const { values } = parseArgs({ args: argv, options: { root: { type: 'string' }, out: { type: 'string' } } });
        await record({ root: values.root, out: values.out, repo: environment.GITHUB_REPOSITORY, runId: environment.GITHUB_RUN_ID, attempt: environment.GITHUB_RUN_ATTEMPT,
            transport: restTransport({ token: environment.GITHUB_TOKEN, fetchImpl }) });
        output('Failed-spec record saved and reconciled');
        return 0;
    } catch { output('Failed-spec reporting failed'); return 1; }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) process.exitCode = await main();
