// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { closeSync, mkdirSync, openSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

function requiredMatch(source, pattern, description) {
    const match = source?.match(pattern);
    if (!match) throw new Error(`Cannot derive legacy contracts inputs: ${description}`);
    return match;
}

function matrixArtifactNames(job, template) {
    const [, axis] = requiredMatch(template, /\$\{\{ matrix\.(\w+) \}\}/, `unsupported artifact name ${template}`);
    const [, matrix] = requiredMatch(job, /^      matrix:\n((?:        .*\n)+)/m, `missing matrix for ${template}`);
    // Only the current single-axis, inline matrices are supported. Fail closed if their shape changes.
    const declaration = matrix.replace(/^\s*#.*\n/gm, '').trim();
    const [, values] = requiredMatch(declaration, new RegExp(`^${axis}: \\[([\\w, '"-]+)\\]$`), `unsupported matrix for ${template}`);
    return values.split(',').map(value => {
        const shard = value.trim().replace(/^(['"])(.*)\1$/, '$2');
        requiredMatch(shard, /^[\w-]+$/, `invalid shard in ${template}`);
        const name = template.replace(`\u0024{{ matrix.${axis} }}`, shard);
        requiredMatch(name, /^coverage-results-specs[\w-]+$/, `invalid artifact name ${name}`);
        return name;
    });
}

export function expectedSpecArtifacts(workflow) {
    const jobs = new Map([...workflow.matchAll(/^  ([\w-]+):\n([\s\S]*?)(?=^  [\w-]+:|$(?![\s\S]))/gm)]
        .map(match => [match[1], match[2]]));
    const [, dependencies] = requiredMatch(jobs.get('legacy-contracts'), /^    needs: \[([^\]]+)\]/m, 'missing legacy-contracts dependencies');
    const names = dependencies.split(',').flatMap(value => {
        const dependency = value.trim();
        const job = jobs.get(dependency);
        if (!job) throw new Error(`Cannot derive legacy contracts inputs: missing job ${dependency}`);
        const uploads = [...job.matchAll(/^          name: (coverage-results-specs[^\n]+)$/gm)];
        if (dependency.startsWith('specs-') && uploads.length === 0) {
            throw new Error(`Cannot derive legacy contracts inputs: missing spec result upload in ${dependency}`);
        }
        return uploads.flatMap(match => matrixArtifactNames(job, match[1]));
    });
    if (names.length === 0 || new Set(names).size !== names.length) {
        throw new Error('Cannot derive legacy contracts inputs: expected a nonempty, unique artifact set');
    }
    return names.sort();
}

function newestArtifact(current, candidate) {
    if (!current) return candidate;
    const age = Date.parse(candidate.created_at) - Date.parse(current.created_at);
    if (age !== 0) return age > 0 ? candidate : current;
    return candidate.id > current.id ? candidate : current;
}

export function selectSpecArtifacts(pages, expected) {
    const latest = new Map();
    for (const artifact of pages.flatMap(page => page.artifacts)) {
        if (!expected.includes(artifact.name)) continue;
        latest.set(artifact.name, newestArtifact(latest.get(artifact.name), artifact));
    }
    const missing = expected.filter(name => !latest.has(name) || latest.get(name).expired);
    if (missing.length > 0) {
        throw new Error(`Legacy contracts input incomplete: missing spec result artifacts: ${missing.join(', ')}`);
    }
    return expected.map(name => latest.get(name));
}

export function downloadSpecArtifacts({ workflow, repository, runId, directory }, run = execFileSync) {
    requiredMatch(repository, /^[\w.-]+\/[\w.-]+$/, 'invalid repository');
    requiredMatch(runId, /^\d+$/, 'invalid run ID');
    const expected = expectedSpecArtifacts(workflow);
    // gh follows every Link page, including the final page skipped by download-artifact v4.
    const pages = JSON.parse(run('gh', ['api', `repos/${repository}/actions/runs/${runId}/artifacts?per_page=100`, '--paginate', '--slurp'], {
        encoding: 'utf8', maxBuffer: 16 * 1024 * 1024
    }));
    const artifacts = selectSpecArtifacts(pages, expected);
    mkdirSync(directory, { recursive: true });
    for (const artifact of artifacts) downloadArtifact(artifact, { repository, directory, run });
    return artifacts;
}

function downloadArtifact(artifact, { repository, directory, run }) {
    const destination = join(directory, artifact.name);
    const archive = join(directory, `${artifact.id}.zip`);
    const file = openSync(archive, 'w');
    try {
        // Download by ID directly: download-artifact's artifact-ids input still lists artifacts first.
        run('gh', ['api', `repos/${repository}/actions/artifacts/${artifact.id}/zip`], { stdio: ['ignore', file, 'inherit'] });
    } finally {
        closeSync(file);
    }
    try {
        run('unzip', ['-q', archive, '-d', destination]);
    } finally {
        rmSync(archive);
    }
    if (!readdirSync(destination, { recursive: true }).some(name => name.toLowerCase().endsWith('.trx'))) {
        throw new Error(`Legacy contracts input incomplete: no TRX results in ${artifact.name}`);
    }
}

export function reportInputError(error, report = console.error) {
    const message = error.message.replaceAll('%', '%25').replaceAll('\r', '%0D').replaceAll('\n', '%0A');
    report(`::error::${message}`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    try {
        const artifacts = downloadSpecArtifacts({
            workflow: readFileSync('.github/workflows/dotnet-build.yml', 'utf8'),
            repository: process.env.REPOSITORY,
            runId: process.env.RUN_ID,
            directory: 'legacy-contract-inputs/trx'
        });
        console.log(`Downloaded ${artifacts.length} complete spec result artifacts (latest per name).`);
    } catch (error) {
        reportInputError(error);
        process.exitCode = 1;
    }
}
