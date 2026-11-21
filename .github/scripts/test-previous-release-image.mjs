// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, writeFile, readFile, rm } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';

const imageTag = '1.2.3-development-slim';
const published = { name: imageTag, images: [{ architecture: 'arm64', os: 'linux' }] };

async function lookupFixture(t, responses) {
    const dir = await mkdtemp(path.join(os.tmpdir(), 'previous-release-image-test-'));
    t.after(() => rm(dir, { recursive: true, force: true }));
    await writeFile(path.join(dir, 'curl'), `#!/usr/bin/env node
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
const countFile = process.env.LOOKUP_COUNT;
const count = existsSync(countFile) ? Number(readFileSync(countFile, 'utf8')) : 0;
writeFileSync(countFile, String(count + 1));
const responses = ${JSON.stringify(responses)};
const response = responses[Math.min(count, responses.length - 1)];
console.log(JSON.stringify(response.metadata));
process.exitCode = response.status;
`, { mode: 0o755 });
    const script = pathToFileURL(path.resolve('.github/scripts/resolve-previous-release-image.mjs')).href;
    const result = spawnSync(process.execPath, ['--input-type=module', '-e',
        `import { waitForPreviousReleaseImage } from ${JSON.stringify(script)};
process.exitCode = await waitForPreviousReleaseImage(${JSON.stringify(imageTag)}, async () => {}) ? 0 : 1;`
    ], { encoding: 'utf8', timeout: 10000, env: { ...process.env, PATH: `${dir}${path.delimiter}${process.env.PATH}`, LOOKUP_COUNT: path.join(dir, 'count') } });
    return { result, attempts: Number(await readFile(path.join(dir, 'count'), 'utf8')) };
}

test('Docker Hub lookup retries two curl failures before accepting a published image', async t => {
    const { result, attempts } = await lookupFixture(t, [{ status: 22 }, { status: 22 }, { status: 0, metadata: published }]);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.equal(attempts, 3);
    assert.equal(result.stdout.match(/::warning::/g)?.length, 2);
});

test('Docker Hub lookup retries empty metadata and a mismatched tag', async t => {
    const { result, attempts } = await lookupFixture(t, [
        { status: 0, metadata: { name: imageTag, images: [] } },
        { status: 0, metadata: { ...published, name: 'wrong-tag' } },
        { status: 0, metadata: published }
    ]);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.equal(attempts, 3);
    assert.equal(result.stdout.match(/::warning::/g)?.length, 2);
});

test('Docker Hub lookup exhausts ten curl failures and preserves the blocking error', async t => {
    const { result, attempts } = await lookupFixture(t, [{ status: 22 }]);
    assert.equal(result.status, 1);
    assert.equal(attempts, 10);
    assert.equal(result.stdout.match(/::warning::/g)?.length, 10);
    assert.equal(result.stderr.trim(), `::error::Previous release image cratis/chronicle:${imageTag} could not be resolved on Docker Hub.`);
});

test('Docker Hub lookup exhausts empty image metadata and preserves the blocking error', async t => {
    const { result, attempts } = await lookupFixture(t, [{ status: 0, metadata: { name: imageTag, images: [] } }]);
    assert.equal(result.status, 1);
    assert.equal(attempts, 10);
    assert.equal(result.stdout.match(/::warning::/g)?.length, 10);
    assert.equal(result.stderr.trim(), `::error::Docker Hub returned no image for cratis/chronicle:${imageTag}.`);
});
