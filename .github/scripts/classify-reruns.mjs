#!/usr/bin/env node
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';

const workflows = new Map([
    ['Requested Integration', 'requested-integration.yml'],
    ['Hot Core Gate', 'hot-core-gate.yml'],
    ['.NET Build & Integration', 'dotnet-build.yml'],
]);
const causes = ['gate-refresh', 'flake', 'deliberate', 'unclassified'];
const counts = () => Object.fromEntries(causes.map(cause => [cause, 0]));
const knownConclusions = new Set(['success', 'failure', 'cancelled', 'skipped']);

function complete(attempt) {
    return Boolean(attempt?.head_sha && attempt.triggering_actor?.login && knownConclusions.has(attempt.conclusion));
}

function human(attempt) {
    return attempt.triggering_actor.type === 'User';
}

function deliberate(previous, current) {
    return human(current) && (current.conclusion === 'failure' || ['success', 'cancelled', 'skipped'].includes(previous.conclusion));
}

function sameHeadCause(previous, current) {
    if (previous.head_sha !== current.head_sha) return 'unclassified';
    if (previous.conclusion === 'failure' && current.conclusion === 'success') return 'flake';
    return deliberate(previous, current) ? 'deliberate' : 'unclassified';
}

export function classifyAttempt(workflow, previous, current) {
    if (!complete(previous) || !complete(current)) return 'unclassified';
    if (workflow === 'Hot Core Gate' && current.triggering_actor.login === 'github-actions[bot]') return 'gate-refresh';
    return sameHeadCause(previous, current);
}

function gh(endpoint) {
    return JSON.parse(execFileSync('gh', ['api', endpoint], { encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 }));
}

async function workflowRuns(context, workflow) {
    const runs = [];
    for (let page = 1; ; page++) {
        const endpoint = `repos/${context.repo}/actions/workflows/${workflow}/runs?created=%3E%3D${context.since.slice(0, 10)}&per_page=100&page=${page}`;
        const response = await context.fetcher(endpoint);
        if (!Array.isArray(response?.workflow_runs)) throw new Error(`Missing workflow_runs for ${workflow}`);
        runs.push(...response.workflow_runs);
        if (response.workflow_runs.length < 100) return runs;
    }
}

async function readAttempt(context, id, number) {
    try {
        return await context.fetcher(`repos/${context.repo}/actions/runs/${id}/attempts/${number}`);
    } catch {
        return null; // A missing attempt has a visible unclassified count, never a guessed cause.
    }
}

function inWindow(run, context) {
    const created = Date.parse(run.created_at);
    return created >= Date.parse(context.since) && created <= context.now && Number.isInteger(run.run_attempt) && run.run_attempt > 1;
}

async function runAttempts(context, workflow, run) {
    const results = [];
    let previous = await readAttempt(context, run.id, 1);
    for (let number = 2; number <= run.run_attempt; number++) {
        const current = await readAttempt(context, run.id, number);
        results.push({ workflow, run_id: run.id, attempt: number, cause: classifyAttempt(workflow, previous, current) });
        previous = current;
    }
    return results;
}

export async function collectReport({ repo = 'Cratis/Chronicle', days = 14, now = Date.now(), fetcher = gh } = {}) {
    const context = { repo, now, since: new Date(now - days * 86400000).toISOString(), fetcher };
    const report = { repo, since: context.since, until: new Date(now).toISOString(), counts: counts(), workflows: {}, attempts: [] };
    for (const [name, file] of workflows) {
        report.workflows[name] = counts();
        const runs = await workflowRuns(context, file);
        for (const run of runs.filter(run => inWindow(run, context))) {
            const attempts = await runAttempts(context, name, run);
            for (const attempt of attempts) {
                report.counts[attempt.cause]++;
                report.workflows[name][attempt.cause]++;
                report.attempts.push(attempt);
            }
        }
    }
    return report;
}

export function markdown(report) {
    const row = (name, totals) => `| ${name} | ${causes.map(cause => totals[cause]).join(' | ')} |`;
    return [
        `# Rerun causes: ${report.repo}`,
        '',
        `Runs created ${report.since} through ${report.until}; flake means probable flake, not a diagnosis.`,
        '',
        '| Workflow | gate-refresh | probable flake | deliberate | unclassified |',
        '| --- | ---: | ---: | ---: | ---: |',
        ...Object.entries(report.workflows).map(([name, totals]) => row(name, totals)),
        row('Total', report.counts),
    ].join('\n');
}

function optionValue(args, index) {
    if (!args[index + 1]) throw new Error(`Missing value for ${args[index]}`);
    return args[index + 1];
}

function validateOptions(options) {
    if (!Number.isInteger(options.days) || options.days < 1) throw new Error('--days must be a positive integer');
    if (!/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(options.repo)) throw new Error('--repo must be owner/name');
    return options;
}

export function parseArgs(args) {
    const options = { repo: 'Cratis/Chronicle', days: 14, json: false };
    for (let index = 0; index < args.length; index++) {
        const option = args[index];
        if (option === '--json') options.json = true;
        else if (option === '--days') options.days = Number(optionValue(args, index++));
        else if (option === '--repo') options.repo = optionValue(args, index++);
        else throw new Error(`Unknown option: ${option}`);
    }
    return validateOptions(options);
}

async function main() {
    const options = parseArgs(process.argv.slice(2));
    const report = await collectReport(options);
    console.log(options.json ? JSON.stringify(report, null, 2) : markdown(report));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
