// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Specs for collect-coverage.sh and the statistics page status: a run without a usable coverage report
// must never advance the measurement date.
import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs';
import { createRequire } from 'node:module';
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

function run({ summaryJson, existingData, storedText, reportName = 'reports', env = {} }) {
    const dir = mkdtempSync(path.join(tmpdir(), 'coverage-stats-'));
    try {
        const reports = path.join(dir, reportName);
        mkdirSync(reports);
        if (summaryJson !== undefined) writeFileSync(path.join(reports, 'Summary.json'), JSON.stringify(summaryJson));
        const out = path.join(dir, 'coverage-data.js');
        if (storedText !== undefined) writeFileSync(out, storedText);
        else if (existingData) writeFileSync(out, `${PREFIX}${JSON.stringify(existingData, null, 2)};\n`);
        const result = spawnSync('bash', [script, reports, out, '19.116.0', SHA, 'msg'], {
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
