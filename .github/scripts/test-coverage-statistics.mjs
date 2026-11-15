// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Specs for collect-coverage.sh and the statistics page status: a run without a usable coverage report
// must never advance the measurement date.
import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs';
import { createRequire } from 'node:module';
import { createHash } from 'node:crypto';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const script = path.join(here, 'collect-coverage.sh');
const pagePath = path.join(here, '..', '..', 'Documentation', 'statistics', 'coverage-page.js');
const NOW = Date.parse('2026-11-01T15:29:06Z');
const SHA = 'abcdef1234567890';
const PREFIX = 'window.COVERAGE_DATA = ';

const legacyEntry = { date: '2026-07-23', week: '2026-M07-W4', lineCoverage: 70, version: '19.100.0', commit: '1111111' };
const existing = {
    lastUpdate: Date.parse('2026-07-23T10:00:00Z'),
    repoUrl: 'https://github.com/Cratis/Chronicle',
    measurement: { measuredAt: Date.parse('2026-07-23T10:00:00Z'), commit: '1111111', version: '19.100.0' },
    entries: { 'Cratis.Chronicle': [legacyEntry] }
};

function summary(assemblies, generatedon) {
    return { summary: generatedon ? { generatedon } : {}, coverage: { assemblies } };
}

const goodAssemblies = [{ name: 'Cratis.Chronicle', coverage: 72.5 }, { name: 'Foo.Specs', coverage: 10 }];

function run({ summaryJson, existingData, storedText, reportName = 'reports', env = {}, feedContext }) {
    const dir = mkdtempSync(path.join(tmpdir(), 'coverage-stats-'));
    try {
        const reports = path.join(dir, reportName);
        mkdirSync(reports);
        if (summaryJson !== undefined) writeFileSync(path.join(reports, 'Summary.json'), JSON.stringify(summaryJson));
        const out = path.join(dir, 'coverage-data.js');
        if (storedText !== undefined) writeFileSync(out, storedText);
        else if (existingData) writeFileSync(out, `${PREFIX}${JSON.stringify(existingData, null, 2)};\n`);
        const contextPath = path.join(dir, 'context.json');
        if (feedContext !== undefined) writeFileSync(contextPath, JSON.stringify(feedContext));
        const result = spawnSync('bash', [script, reports, out, '19.116.0', SHA, 'msg', ...(feedContext !== undefined ? [contextPath] : [])], {
            encoding: 'utf8',
            env: { ...process.env, COVERAGE_NOW_MS: String(NOW), COVERAGE_MEASURED_AT_MS: '', ...env }
        });
        const outputText = existsSync(out) ? readFileSync(out, 'utf8') : null;
        const data = result.status === 0 && outputText !== null
            ? JSON.parse(outputText.trim().slice(PREFIX.length).replace(/;$/, '')) : null;
        return { result, data, outputText };
    } finally {
        rmSync(dir, { recursive: true, force: true });
    }
}

const feedSummary = summary(goodAssemblies);
function feedContext(measuredAt = NOW) {
    return {
        state: 'measured', attemptedAt: NOW,
        observer: { runId: 99, runAttempt: 1, executionSha: 'a'.repeat(40), requestedVersion: '19.116.0', releaseContext: 'unverified' },
        source: {
            schemaVersion: 1, repository: 'Cratis/Chronicle', repositoryId: 11,
            workflowPath: '.github/workflows/dotnet-build.yml', workflowId: 22, event: 'schedule', ref: 'refs/heads/main',
            runId: 33, runAttempt: 2, headSha: 'a'.repeat(40), measuredAt,
            summarySha256: createHash('sha256').update(JSON.stringify(feedSummary)).digest('hex'),
            scope: 'dotnet-build/coverage-merge-and-cache-v1', artifactId: 44, artifactName: 'coverage-nightly-v1-33-2',
            artifactDigest: `sha256:${'d'.repeat(64)}`, artifactSize: 1000,
            runCreatedAt: new Date(measuredAt - 3600000).toISOString(), runUpdatedAt: new Date(measuredAt + 1000).toISOString(),
            artifactCreatedAt: new Date(measuredAt + 1000).toISOString(), artifactExpiresAt: new Date(measuredAt + 28 * 86400000).toISOString()
        }
    };
}

test('nightly context separates observer even for equal SHAs and re-observation does not remeasure', () => {
    const context = feedContext();
    const first = run({ summaryJson: feedSummary, feedContext: context });
    assert.equal(first.result.status, 0, first.result.stderr);
    assert.equal(first.data.measurement.version, null);
    assert.equal(first.data.entries['Cratis.Chronicle'][0].version, null);
    assert.equal(first.data.measurement.source.headSha, 'a'.repeat(40));
    context.observer.runId = 100;
    const second = run({ summaryJson: feedSummary, feedContext: context, existingData: first.data });
    assert.equal(second.result.status, 0, second.result.stderr);
    assert.deepEqual(second.data.measurement, first.data.measurement);
    assert.deepEqual(second.data.entries, first.data.entries);
    assert.equal(second.data.lastUpdate, first.data.lastUpdate);
    assert.equal(second.data.latestAttempt.observer.runId, 100);
});

test('rejected and unavailable feed preserves history despite a stale Summary.json', () => {
    for (const state of ['missing', 'incomplete']) {
        const context = { state, reason: 'feed unavailable', attemptedAt: NOW, observer: feedContext().observer };
        const { result, data } = run({ summaryJson: feedSummary, feedContext: context, existingData: existing });
        assert.equal(result.status, 0, result.stderr);
        assert.deepEqual(data.measurement, existing.measurement);
        assert.deepEqual(data.entries, existing.entries);
        assert.equal(data.lastUpdate, existing.lastUpdate);
        assert.equal(data.latestAttempt.state, state);
        assert.equal(data.latestAttempt.observer.releaseContext, 'unverified');
    }
});

test('older feed preserves history and newer same-week feed updates date', () => {
    const first = run({ summaryJson: feedSummary, feedContext: feedContext(NOW - 2 * 86400000) }).data;
    const older = run({ summaryJson: feedSummary, feedContext: feedContext(NOW - 3 * 86400000), existingData: first });
    assert.equal(older.result.status, 0, older.result.stderr);
    assert.equal(older.data.latestAttempt.state, 'incomplete');
    assert.deepEqual(older.data.measurement, first.measurement);
    const newer = run({ summaryJson: feedSummary, feedContext: feedContext(NOW - 86400000), existingData: first });
    assert.equal(newer.result.status, 0, newer.result.stderr);
    assert.equal(newer.data.entries['Cratis.Chronicle'][0].date, '2026-10-31');
});

test('malformed context or changed verified report never modifies output', () => {
    for (const context of [{}, { ...feedContext(), source: {} }, feedContext()]) {
        const { result, outputText } = run({ summaryJson: summary([{ name: 'Other', coverage: 10 }]), feedContext: context, existingData: existing });
        assert.notEqual(result.status, 0);
        assert.equal(outputText, `${PREFIX}${JSON.stringify(existing, null, 2)};\n`);
    }
});

test('nightly status names unverified observer safely without claiming coverage for a release', () => {
    const context = feedContext();
    const data = run({ summaryJson: feedSummary, feedContext: context }).data;
    const status = describeCoverageStatus(data, NOW);
    assert.match(status.message, /nightly measurement/i);
    assert.match(status.message, /Publish run 99/);
    assert.match(status.message, /release context unverified/);
    assert.equal(describeCoverageStatus(data, NOW + 8 * 86400000).state, 'stale');
});

test('observer text remains literal and coverage status is rendered only as text', () => {
    const context = feedContext();
    context.observer.requestedVersion = '<img src=x onerror=alert(1)>';
    const { result, data } = run({ summaryJson: feedSummary, feedContext: context });
    assert.equal(result.status, 0, result.stderr);
    assert.match(describeCoverageStatus(data, NOW).message, /<img src=x onerror=alert\(1\)>/);
    const page = readFileSync(new URL('../../Documentation/statistics/coverage-page.js', import.meta.url), 'utf8');
    assert.match(page, /statusElement\.textContent = status\.message/);
    assert.doesNotMatch(page, /innerHTML\s*=\s*status\.message/);
});

test('missing report leaves measurement untouched and records a missing attempt', () => {
    const { result, data } = run({ existingData: existing });
    assert.equal(result.status, 0);
    assert.match(result.stdout, /::warning::/);
    assert.deepEqual(data.lastUpdate, existing.lastUpdate);
    assert.deepEqual(data.measurement, existing.measurement);
    assert.deepEqual(data.entries, existing.entries);
    assert.equal(data.latestAttempt.state, 'missing');
    assert.equal(data.latestAttempt.attemptedAt, NOW);
    assert.equal(data.latestAttempt.version, '19.116.0');
    assert.equal(data.latestAttempt.commit, 'abcdef1');
    assert.ok(data.latestAttempt.reason);
});

test('missing report without an output file creates an empty structure with a missing attempt', () => {
    const { result, data } = run({});
    assert.equal(result.status, 0);
    assert.equal(data.lastUpdate, 0);
    assert.deepEqual(data.entries, {});
    assert.equal(data.measurement, undefined);
    assert.equal(data.latestAttempt.state, 'missing');
});

for (const [label, assemblies] of [
    ['only filtered assemblies', [{ name: 'Foo.Specs', coverage: 10 }, { name: 'xunit.core', coverage: 5 }]],
    ['no assemblies', []]
]) {
    test(`report with ${label} is incomplete and changes nothing else`, () => {
        const { result, data } = run({ summaryJson: summary(assemblies, '2026-10-30T10:00:00Z'), existingData: existing });
        assert.equal(result.status, 0);
        assert.equal(data.lastUpdate, existing.lastUpdate);
        assert.deepEqual(data.measurement, existing.measurement);
        assert.deepEqual(data.entries, existing.entries);
        assert.equal(data.latestAttempt.state, 'incomplete');
    });
}

test('valid report records the measurement time from the report, not the publish time', () => {
    const { result, data } = run({ summaryJson: summary(goodAssemblies, '2026-07-20T10:00:00Z'), existingData: existing });
    const measuredAt = Date.parse('2026-07-20T10:00:00Z');
    assert.equal(result.status, 0);
    assert.equal(data.measurement.measuredAt, measuredAt);
    assert.equal(data.measurement.commit, 'abcdef1');
    assert.equal(data.measurement.version, '19.116.0');
    assert.equal(data.lastUpdate, measuredAt);
    assert.equal(data.latestAttempt.state, 'measured');
    assert.equal(data.latestAttempt.attemptedAt, NOW);
    const added = data.entries['Cratis.Chronicle'].find(e => e.date === '2026-07-20');
    assert.equal(added.week, '2026-M07-W3');
    assert.equal(added.lineCoverage, 72.5);
    assert.equal(data.entries['Foo.Specs'], undefined);
});

for (const generatedon of [undefined, 'not a date', '2026-02-31T10:00:00Z', '2026-07-20T10:00:00+02:00']) {
    test(`report with untrusted generatedon ${generatedon} preserves measurement as incomplete`, () => {
        const { result, data } = run({ summaryJson: summary(goodAssemblies, generatedon), existingData: existing });
        assert.equal(result.status, 0);
        assert.equal(data.lastUpdate, existing.lastUpdate);
        assert.deepEqual(data.measurement, existing.measurement);
        assert.deepEqual(data.entries, existing.entries);
        assert.equal(data.latestAttempt.state, 'incomplete');
        assert.equal(data.latestAttempt.attemptedAt, NOW);
    });
}

for (const generatedon of ['2026-07-20T10:00:00', '2026-07-20T10:00:00.123Z', '2026-07-20T10:00:00+00:00']) {
    test(`valid UTC report date ${generatedon} supplies measurement time`, () => {
        const { result, data } = run({ summaryJson: summary(goodAssemblies, generatedon) });
        assert.equal(result.status, 0);
        assert.equal(data.measurement.measuredAt, Date.parse('2026-07-20T10:00:00Z'));
    });
}

for (const summaryJson of [undefined, summary(goodAssemblies, '2026-07-20T10:00:00Z')]) {
    test(`malformed stored JSON preserves file on ${summaryJson ? 'measurement' : 'missing'} path`, () => {
        const storedText = `${PREFIX}{broken;\n`;
        const { result, outputText } = run({ summaryJson, storedText });
        assert.notEqual(result.status, 0);
        assert.equal(outputText, storedText);
    });
    for (const name of ['COVERAGE_NOW_MS', 'COVERAGE_MEASURED_AT_MS']) {
        for (const value of ['not-a-number', 'NaN', '-1', '1.5', '1e999']) {
            test(`invalid ${name}=${value} preserves file on ${summaryJson ? 'measurement' : 'missing'} path`, () => {
                const storedText = `${PREFIX}${JSON.stringify(existing, null, 2)};\n`;
                const { result, outputText } = run({ summaryJson, storedText, env: { [name]: value } });
                assert.notEqual(result.status, 0);
                assert.equal(outputText, storedText);
            });
        }
    }
}

test('warning does not incorporate report paths containing workflow command newlines', () => {
    const { result, data } = run({ reportName: 'reports\r\n::error::injected' });
    assert.equal(result.status, 0);
    assert.equal(data.latestAttempt.state, 'missing');
    assert.match(result.stdout, /::warning::Coverage missing: no Summary.json found; measurement date unchanged/);
    assert.doesNotMatch(data.latestAttempt.reason, /[\r\n]/);
});

test('COVERAGE_MEASURED_AT_MS wins over generatedon', () => {
    const override = Date.parse('2026-06-15T00:00:00Z');
    const { data } = run({
        summaryJson: summary(goodAssemblies, '2026-07-20T10:00:00Z'),
        existingData: existing,
        env: { COVERAGE_MEASURED_AT_MS: String(override) }
    });
    assert.equal(data.measurement.measuredAt, override);
    assert.equal(data.lastUpdate, override);
});

test('week of month is not read as octal for day 08', () => {
    const { result, data } = run({ summaryJson: summary(goodAssemblies, '2026-07-08T10:00:00Z'), existingData: existing });
    assert.equal(result.status, 0);
    assert.equal(data.entries['Cratis.Chronicle'].some(e => e.week === '2026-M07-W2'), true);
});

const require = createRequire(import.meta.url);
const describeCoverageStatus = (...args) => require(pagePath).describeCoverageStatus(...args);
const DAY = 24 * 60 * 60 * 1000;

for (const summaryJson of [undefined, summary(goodAssemblies)]) {
    test(`first ${summaryJson ? 'incomplete' : 'missing'} attempt is visible without any measurement`, () => {
        const { result, data } = run({ summaryJson });
        assert.equal(result.status, 0);
        const status = describeCoverageStatus(data, NOW);
        assert.equal(status.state, 'missing');
        assert.equal(status.measuredAt, null);
        assert.match(status.message, /19\.116\.0/);
        assert.ok(status.message.includes(data.latestAttempt.state));
    });
}

test('status is unavailable without data', () => {
    assert.equal(describeCoverageStatus(null, NOW).state, 'unavailable');
});

test('status is none without measurement or entries', () => {
    assert.equal(describeCoverageStatus({ lastUpdate: NOW, entries: {} }, NOW).state, 'none');
});

test('legacy data reports the newest entry date as stale, not the polluted lastUpdate', () => {
    const legacy = { lastUpdate: Date.parse('2026-11-01T15:29:06Z'), entries: { A: [{ date: '2026-07-23' }, { date: '2026-07-16' }] } };
    const status = describeCoverageStatus(legacy, Date.parse('2026-11-02T00:00:00Z'));
    assert.equal(status.state, 'stale');
    assert.equal(status.measuredAt, Date.parse('2026-07-23'));
});

test('recent measurement is current', () => {
    const data = { ...existing, latestAttempt: { state: 'measured', attemptedAt: existing.measurement.measuredAt } };
    assert.equal(describeCoverageStatus(data, existing.measurement.measuredAt + DAY).state, 'current');
});

test('failed newer attempt is missing and names the attempt version', () => {
    for (const state of ['missing', 'incomplete']) {
        const data = { ...existing, latestAttempt: { state, attemptedAt: existing.measurement.measuredAt + DAY, version: '19.116.0', commit: 'abcdef1' } };
        const status = describeCoverageStatus(data, existing.measurement.measuredAt + 2 * DAY);
        assert.equal(status.state, 'missing');
        assert.match(status.message, /19\.116\.0/);
    }
});
