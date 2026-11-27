// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Looks up the exact version of the package in a directory on the npm registry and writes `exists=true|false`
// to GITHUB_OUTPUT. E404 means absent; an existing exact version means "skip publishing this package"; any
// other lookup or parse failure fails the job - an uncertain answer is never treated as "already published".

import { spawnSync } from 'node:child_process';
import { appendFileSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * Interprets the result of `npm view <name>@<version> version --json`.
 * @param {{error?: Error, status: number | null, stdout: string}} lookup The finished process.
 * @param {string} version The version that was looked up.
 * @returns {boolean} True when the exact version is already on the registry.
 * @throws {Error} When the lookup could not give a certain answer.
 */
export const versionExists = ({ error, status, stdout }, version) => {
    if (error || status === null) {
        throw new Error('could not complete');
    }

    let response;
    try {
        response = stdout.trim() ? JSON.parse(stdout) : [];
    } catch {
        throw new Error(`returned invalid JSON (exit ${status})`);
    }
    if (status !== 0 && response?.error?.code !== 'E404') {
        throw new Error(`failed (exit ${status})`);
    }

    // npm can return multiple versions with the same SemVer precedence.
    const versions = [response].flat();
    if (status === 0 && versions.some(value => typeof value !== 'string')) {
        throw new Error('returned an unexpected response');
    }
    return status === 0 && versions.includes(version);
};

if (process.argv[1] === fileURLToPath(import.meta.url)) {
    const directory = path.resolve(process.argv[2] ?? '.');
    const { name, version } = JSON.parse(readFileSync(path.join(directory, 'package.json'), 'utf8'));
    const spec = `${name}@${version}`;
    const lookup = spawnSync('npm', ['view', spec, 'version', '--json', '--loglevel=silent', '--prefer-online'], { encoding: 'utf8' });

    let exists;
    try {
        exists = versionExists(lookup, version);
    } catch (failure) {
        console.error(`::error::npm version lookup for ${spec} ${failure.message}`);
        process.exit(1);
    }
    if (exists) {
        console.log(`::notice::${spec} already exists on npm; skipping publish.`);
    }
    appendFileSync(process.env.GITHUB_OUTPUT, `exists=${exists}\n`);
}
