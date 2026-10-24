// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { parseRecords, summarize, unionDuration, markdown } from './integration-timing-summary.mjs';
import { buildTargets, trxDurations } from './integration-timings.mjs';

const record = (overrides = {}) => ({ schema: 1, run: 'run', sha: 'sha', process: 1, instance: 'fixture', type: 'fixture',
    mode: 'inprocess', provider: 'MongoDB', attempt: '1', filter: 'filter', sequence: 1,
    phase: 'scenario', startMs: 0, elapsedMs: 100, count: 1, polls: 0, retries: 0,
    outcome: 'success', completeness: 'complete', outputFailed: false, ...overrides });
const line = (value, kind = 'TIMING') => `CHRONICLE_INTEGRATION_${kind} ${JSON.stringify(value)}\n`;
const summaryLine = overrides => line(record({ sequence: 99, phase: 'instance', outcome: 'finished', ...overrides }), 'SUMMARY');
const parse = text => parseRecords(text);

test('deduplicates start, terminal, sidecar and TRX replays without losing final duration', () => {
    const start = line(record({ outcome: 'started', completeness: 'partial', elapsedMs: 0 }));
    const end = line(record());
    const log = start + end + summaryLine();
    const xml = `<StdOut>${log.replaceAll('"', '&quot;')}</StdOut>`;
    const result = summarize([parse(log), parseRecords(xml, true), parse(end)]);
    assert.equal(result.completeness, 'complete');
    assert.equal(result.rows[0].count, 1);
    assert.equal(result.rows[0].inclusiveMs, 100);
});

test('reads CDATA, XML escaping, numeric entities and closing tags on the record line', () => {
    const json = line(record({ type: 'A&B<C' })).trimEnd();
    const cdata = parseRecords(`<StdOut><![CDATA[${json}]]></StdOut>`, true);
    const escaped = parseRecords(`<StdOut>${json.replaceAll('&', '&amp;').replaceAll('"', '&#34;').replaceAll('<', '&lt;')}</StdOut>`, true);
    assert.deepEqual(cdata.records, escaped.records);
    assert.equal(cdata.records[0].type, 'A&B<C');
});

test('takes the union of overlapping child spans for exclusive accounting', () => {
    const text = line(record()) + line(record({ sequence: 2, parent: 1, parentInstance: 'fixture', phase: 'wait', startMs: 10, elapsedMs: 50 })) +
        line(record({ sequence: 3, parent: 1, parentInstance: 'fixture', phase: 'wait', startMs: 30, elapsedMs: 50 })) + summaryLine();
    const result = summarize([parse(text)]);
    const parent = result.rows.find(row => row.phase === 'scenario');
    const wait = result.rows.find(row => row.phase === 'wait');
    assert.equal(parent.exclusiveMs, 30);
    assert.equal(wait.inclusiveMs, 100);
    assert.equal(wait.unionMs, 70);
    assert.equal(unionDuration([]), 0);
});

test('does not align clocks between processes or merge attempts and filters', () => {
    const text = [1, 2].map(process => line(record({ process })) + summaryLine({ process })).join('') +
        line(record({ attempt: '2', instance: 'retry' })) + summaryLine({ attempt: '2', instance: 'retry' }) +
        line(record({ filter: 'other', instance: 'other' })) + summaryLine({ filter: 'other', instance: 'other' });
    const result = summarize([parse(text)]);
    assert.equal(result.rows.length, 3);
    assert.equal(result.rows[0].unionMs, 200);
});

test('missing, corrupt, conflicting, interrupted and diagnostic-failure records are visibly incomplete', () => {
    for (const text of ['', line(record()), line(record({ instance: 'wrapper', phase: 'test' }), 'RUN'), 'CHRONICLE_INTEGRATION_TIMING {"schema":1',
        line(record({ outcome: 'started', completeness: 'partial' })) + summaryLine(),
        line(record()) + line(record({ elapsedMs: 101 })) + summaryLine(),
        line(record()) + summaryLine({ outputFailed: true }),
        line(record()) + summaryLine() + 'CHRONICLE_INTEGRATION_TIMING_WARNING unavailable']) {
        const result = summarize([parse(text)]);
        assert.equal(result.completeness, 'partial', text);
        assert.ok(result.errors.length > 0);
        assert.match(markdown(result), /partial/);
    }
});

test('keeps cross-instance parentage and clamps child intervals to the parent', () => {
    const text = line(record()) + line(record({ instance: 'child', parentInstance: 'fixture', parent: 1, phase: 'wait',
        startMs: 90, elapsedMs: 50 })) + summaryLine() + summaryLine({ instance: 'child' });
    assert.equal(summarize([parse(text)]).rows[0].exclusiveMs, 90);
});

test('reports retries, polls, failures and percentiles separately from wall time', () => {
    const text = line(record({ retries: 2, polls: 3, outcome: 'failure' })) +
        line(record({ sequence: 2, elapsedMs: 200 })) + summaryLine() + line(record({ instance: 'wrapper', phase: 'test', elapsedMs: 250 }), 'RUN');
    const result = summarize([parse(text)]);
    assert.equal(result.rows[0].retries, 2);
    assert.equal(result.rows[0].polls, 3);
    assert.equal(result.rows[0].failures, 1);
    assert.equal(result.rows[0].p50Ms, 100);
    assert.equal(result.rows[0].p95Ms, 200);
    assert.equal(result.runs[0].elapsedMs, 250);
    assert.equal(result.runs[0].unaccountedMs, null);
});

test('parses MSBuild inclusive worker subsets without calling them wall time', () => {
    const result = buildTargets('  123 ms GenerateProtoFiles 2 calls\n  42.5 ms GrpcClients 1 calls\n  99 ms Build 1 calls\n');
    assert.equal(result.length, 2);
    assert.equal(result[0].count, 2);
    assert.equal(result[0].accounting, 'inclusive-worker-subset');
});

test('keeps EF command aggregates out of interval and percentile accounting', () => {
    const text = line(record()) + line(record({ sequence: 2, parent: 1, parentInstance: 'fixture', phase: 'ef-command-duration',
        accounting: 'aggregate-command-duration', elapsedMs: 300, count: 3, failures: 1 })) + summaryLine();
    const result = summarize([parse(text)]);
    const commands = result.rows.find(row => row.phase === 'ef-command-duration');
    assert.equal(commands.count, 3);
    assert.equal(commands.inclusiveMs, 300);
    assert.equal(commands.failures, 1);
    for (const field of ['exclusiveMs', 'unionMs', 'p50Ms', 'p95Ms', 'maxMs']) assert.equal(commands[field], null);
    assert.equal(result.rows[0].exclusiveMs, 100);
    assert.match(markdown(result), /unavailable/);
});

test('does not merge clocks of containers that have the same operating-system PID', () => {
    const text = ['kernel-a', 'kernel-b'].map(clockDomain => line(record({ instance: clockDomain, clockDomain })) +
        summaryLine({ instance: clockDomain, clockDomain })).join('');
    const result = summarize([parse(text)]);
    assert.equal(result.rows[0].unionMs, 200);
});

test('enforces the supplied phase inventory independently in each producer partition', () => {
    const text = line(record()) + summaryLine() + line(record({ instance: 'mssql', provider: 'mssql', phase: 'boot' })) + summaryLine({ instance: 'mssql', provider: 'mssql' });
    const result = summarize([parse(text)], ['scenario', 'boot']);
    assert.equal(result.completeness, 'partial');
    assert.equal(result.errors.filter(error => error.startsWith('Missing required phase')).length, 2);
    assert.equal(summarize([parse(line(record()) + summaryLine())], ['scenario']).completeness, 'complete');
});

test('extracts TRX inclusive test durations without treating their sum as wall time', () => {
    const result = trxDurations('<UnitTestResult duration="00:00:01.250"/><UnitTestResult duration="00:00:02" />');
    assert.equal(result.count, 2);
    assert.equal(result.totalInclusiveMs, 3250);
    assert.equal(result.p50Ms, 1250);
    assert.equal(result.p95Ms, 2000);
    assert.equal(result.accounting, 'inclusive-test-duration-not-wall');
    assert.deepEqual(result.errors, []);
    assert.ok(trxDurations('<UnitTestResult/>').errors.length);
    assert.ok(trxDurations('').errors.length);
});

const cli = fileURLToPath(new URL('./integration-timings.mjs', import.meta.url));
function childRun(source) {
    const env = { ...process.env };
    delete env.CHRONICLE_INTEGRATION_TIMINGS_FILE;
    return spawnSync(process.execPath, [cli, 'run', '--phase', 'test', '--', process.execPath, '-e', source], { env, encoding: 'utf8' });
}

test('wrapper tees output and preserves successful and failed exit codes', () => {
    for (const code of [0, 7]) {
        const result = childRun(`console.log('child stdout'); console.error('child stderr'); process.exitCode = ${code}`);
        assert.equal(result.status, code);
        assert.match(result.stdout, /child stdout/);
        assert.match(result.stderr, /child stderr/);
        const run = parse(result.stdout).records[0];
        assert.equal(run.kind, 'RUN');
        assert.equal(run.outcome, code === 0 ? 'success' : 'failure');
    }
});

test('wrapper preserves child termination signal', { skip: process.platform === 'win32' }, () => {
    const result = childRun("process.kill(process.pid, 'SIGTERM')");
    assert.equal(result.signal, 'SIGTERM');
    assert.match(result.stdout, /CHRONICLE_INTEGRATION_RUN/);
});
