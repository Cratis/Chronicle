// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, mkdtemp, copyFile, writeFile, readFile, rm } from 'node:fs/promises';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { validate, unquarantined } from './quarantine.mjs';

const now = Date.parse('2026-11-12T12:00:00Z');
const entry = { spec: 'Example.Spec.should_work', issue: 5008, owner: 'woksin', expires: '2026-12-12', reason: 'Unproven race; reproduce before fixing.' };
const config = { entries: [entry] };

test('lint rejects expired, impossible, distant and malformed entries', () => {
    validate(config, now);
    for (const extra of [{ expires: '2026-11-11' }, { expires: '2026-02-30' }, { expires: '2027-03-12' }, { owner: '' }, { issue: 0 }, { reason: '' }]) {
        assert.throws(() => validate({ entries: [{ ...entry, ...extra }] }, now));
    }
});

test('matching is exact or dot-delimited prefix, never substring; expired entries cannot excuse failures', () => {
    assert.deepEqual(unquarantined(['Example.Spec.should_work', 'Example.Spec.should_work.case', 'XExample.Spec.should_work', 'Example.Spec.should_work_extra'], config, now), ['XExample.Spec.should_work', 'Example.Spec.should_work_extra']);
    assert.deepEqual(unquarantined([entry.spec], config, Date.parse('2026-12-13')), [entry.spec]);
});

async function wrapperFixture(t, names, attempts = 3) {
    await mkdir('.ai-work/r1', { recursive: true });
    const dir = await mkdtemp(path.resolve('.ai-work/r1/quarantine-test-'));
    t.after(() => rm(dir, { recursive: true, force: true }));
    await mkdir(path.join(dir, '.github/scripts'), { recursive: true });
    for (const file of ['quarantine.mjs', 'run-tests-with-retry.sh']) await copyFile(`.github/scripts/${file}`, path.join(dir, '.github/scripts', file));
    await writeFile(path.join(dir, '.github/quarantine.json'), JSON.stringify({ entries: [{ ...entry, expires: new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10) }] }));
    await writeFile(path.join(dir, '.github/scripts/run-spec-project.mjs'), `import {mkdirSync,writeFileSync} from 'node:fs';
const dir=process.argv[process.argv.indexOf('--results-directory')+1];
mkdirSync(dir,{recursive:true});
writeFileSync(dir+'/results.trx', '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>${names.map(name => `<UnitTestResult testName="${name}" outcome="Failed"/>`).join('')}</Results></TestRun>');
process.exitCode=1;`);
    const result = spawnSync('bash', ['.github/scripts/run-tests-with-retry.sh', 'Example', 'FullyQualifiedName~Example'], { cwd: dir, encoding: 'utf8', env: { ...process.env, TEST_MAX_ATTEMPTS: String(attempts), TEST_RESULTS_DIR: path.join(dir, 'results') } });
    return { dir, result };
}

for (const attempts of [1, 2, 3]) test(`quarantine keeps first failure and warns at retry exit (${attempts} attempts)`, async t => {
    const { dir, result } = await wrapperFixture(t, [entry.spec], attempts);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.match(result.stdout, /::warning::.*5008.*woksin/);
    assert.match(await readFile(path.join(dir, 'results/quarantined-failures.log'), 'utf8'), /attempt-1.*Example.Spec.should_work/);
    assert.match(await readFile(path.join(dir, 'results/attempt-1/results.trx'), 'utf8'), /outcome="Failed"/);
});

test('systemic failures stay blocking even if every recorded name matches quarantine', async t => {
    const systemic = await wrapperFixture(t, Array.from({ length: 21 }, (_, index) => `${entry.spec}.case${index}`));
    assert.equal(systemic.result.status, 1);
    assert.match(systemic.result.stdout, /systemic failure/);
});

test('mixed failures and infrastructure failures stay blocking', async t => {
    const mixed = await wrapperFixture(t, [entry.spec, 'Other.should_fail']);
    assert.equal(mixed.result.status, 1);
    const crash = await wrapperFixture(t, []);
    assert.equal(crash.result.status, 1);
});
