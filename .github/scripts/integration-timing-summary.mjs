// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const prefix = /CHRONICLE_INTEGRATION_(TIMING|SUMMARY|RUN) ([^\r\n]*)/g;
const identity = record => JSON.stringify([record.run, record.sha, record.process, record.instance, record.sequence]);
const instanceIdentity = record => JSON.stringify([record.run, record.sha, record.process, record.instance]);
const partition = record => JSON.stringify([record.run, record.sha, record.attempt, record.filter, record.mode, record.provider]);

function decodeXml(text) {
    return text.replace(/<!\[CDATA\[([\s\S]*?)\]\]>/g, '$1')
        .replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&lt;/g, '<')
        .replace(/&gt;/g, '>').replace(/&#x([\da-f]+);/gi, (_, value) => String.fromCodePoint(parseInt(value, 16)))
        .replace(/&#(\d+);/g, (_, value) => String.fromCodePoint(Number(value))).replace(/&amp;/g, '&');
}

export function parseRecords(text, xml = false) {
    const records = [];
    const errors = [];
    const source = xml ? decodeXml(text) : text;
    if (source.includes('CHRONICLE_INTEGRATION_TIMING_WARNING')) errors.push('Diagnostic output was unavailable');
    for (const match of source.matchAll(prefix)) {
        try {
            // TRX may place the closing StdOut tag on the same line as the last JSON record.
            const end = match[2].lastIndexOf('}');
            const record = JSON.parse(match[2].slice(0, end + 1));
            validateRecord(record, match[1]);
            records.push({ ...record, kind: match[1] });
        } catch {
            errors.push('Malformed or truncated timing record');
        }
    }
    return { records, errors };
}

function requireSchema(valid) {
    if (!valid) throw new Error('Invalid schema');
}

function validateRecord(record, kind) {
    requireSchema(record.schema === 1);
    requireSchema(typeof record.run === 'string');
    requireSchema(typeof record.instance === 'string');
    requireSchema(Number.isFinite(record.sequence));
    requireSchema(Number.isFinite(record.elapsedMs));
    requireSchema(record.elapsedMs >= 0);
    if (kind === 'RUN') return;
    requireSchema(Number.isFinite(record.startMs));
    requireSchema(typeof record.phase === 'string');
}

export function unionDuration(intervals) {
    let end = -Infinity;
    let duration = 0;
    for (const [start, stop] of intervals.toSorted((a, b) => a[0] - b[0])) {
        duration += Math.max(0, stop - Math.max(start, end));
        end = Math.max(end, stop);
    }
    return duration;
}

const interval = record => [record.startMs, record.startMs + record.elapsedMs];
const clockIdentity = record => record.clockDomain ?? `legacy-process-${record.process}`;
const percentile = (sorted, fraction) => sorted[Math.max(0, Math.ceil(sorted.length * fraction) - 1)];

function deduplicate(inputs, errors) {
    const records = new Map();
    for (const record of inputs.flatMap(input => input.records)) {
        const key = identity(record);
        const prior = records.get(key);
        if (!prior || prior.outcome === 'started') records.set(key, record);
        else if (record.outcome !== 'started' && JSON.stringify(prior) !== JSON.stringify(record)) errors.push('Conflicting duplicate record');
    }
    return [...records.values()];
}

function validatePhasePresence(all, phases, errors) {
    if (phases.length) return;
    const runPhases = all.filter(record => record.kind === 'RUN').map(record => record.phase);
    if (!runPhases.includes('build')) errors.push('No instrumented phases found');
    if (runPhases.includes('test')) errors.push('Test invocation has no instrumented phases');
}

function incompleteRecord(record) {
    return record.completeness !== 'complete' || record.outputFailed;
}

function validateTerminalSummaries(all, phases, errors) {
    const finished = new Set(all.filter(record => record.kind === 'SUMMARY').map(instanceIdentity));
    for (const record of phases) {
        if (!finished.has(instanceIdentity(record))) errors.push('Instance has no terminal summary');
    }
}

function validateCompleteness(all, phases, errors) {
    if (!all.length) errors.push('No timing records found');
    validatePhasePresence(all, phases, errors);
    errors.push(...all.filter(incompleteRecord).map(record => `Incomplete ${record.kind.toLowerCase()} record`));
    validateTerminalSummaries(all, phases, errors);
}

function validateInventory(finalPhases, requiredPhases, errors) {
    const partitions = Map.groupBy(finalPhases, partition);
    for (const [key, group] of partitions) {
        for (const phase of requiredPhases) {
            if (!group.some(record => record.phase === phase)) errors.push(`Missing required phase ${phase} in ${key}`);
        }
    }
    if (requiredPhases.length && !partitions.size) errors.push('Required phase inventory has no instrumented partition');
}

function aggregateStatistics(group) {
    return { count: group.reduce((sum, record) => sum + record.count, 0),
        exclusiveMs: null, unionMs: null, meanMs: null, p50Ms: null, p95Ms: null, maxMs: null,
        failures: group.reduce((sum, record) => sum + record.failures, 0) };
}

function intervalStatistics(group, phases, durations, inclusiveMs) {
    // Only compare monotonic intervals from the same process. Cross-process clocks are not aligned.
    const byProcess = Map.groupBy(group, clockIdentity);
    return { count: group.length,
        unionMs: [...byProcess.values()].reduce((sum, values) => sum + unionDuration(values.map(interval)), 0),
        exclusiveMs: group.reduce((sum, record) => sum + exclusiveDuration(record, phases), 0),
        meanMs: inclusiveMs / group.length, p50Ms: percentile(durations, 0.5),
        p95Ms: percentile(durations, 0.95), maxMs: durations.at(-1),
        failures: group.filter(record => record.outcome === 'failure').length };
}

function phaseRow(group, phases) {
    const first = group[0];
    const durations = group.map(record => record.elapsedMs).sort((a, b) => a - b);
    const inclusiveMs = durations.reduce((sum, value) => sum + value, 0);
    const statistics = first.accounting === 'aggregate-command-duration' ? aggregateStatistics(group)
        : intervalStatistics(group, phases, durations, inclusiveMs);
    return { run: first.run, sha: first.sha, attempt: first.attempt, filter: first.filter, mode: first.mode, provider: first.provider,
        phase: first.phase, accounting: first.accounting ?? 'interval', inclusiveMs, ...statistics,
        retries: group.reduce((sum, record) => sum + (record.retries ?? 0), 0),
        polls: group.reduce((sum, record) => sum + (record.polls ?? 0), 0) };
}

function isChild(child, record) {
    if (clockIdentity(child) !== clockIdentity(record) || partition(child) !== partition(record)) return false;
    if (child.accounting === 'aggregate-command-duration' || child.outcome === 'started') return false;
    return child.parentInstance === record.instance && child.parent === record.sequence;
}

function exclusiveDuration(record, phases) {
    const [start, end] = interval(record);
    const covered = phases.filter(child => isChild(child, record))
        .map(child => [Math.max(start, child.startMs), Math.min(end, child.startMs + child.elapsedMs)])
        .filter(([a, b]) => b >= a);
    return record.elapsedMs - unionDuration(covered);
}

export function summarize(inputs, requiredPhases = []) {
    const errors = inputs.flatMap(input => input.errors);
    const all = deduplicate(inputs, errors);
    const phases = all.filter(record => record.kind === 'TIMING');
    validateCompleteness(all, phases, errors);
    const finalPhases = phases.filter(record => record.outcome !== 'started');
    validateInventory(finalPhases, requiredPhases, errors);
    const groups = Map.groupBy(finalPhases, record => JSON.stringify([partition(record), record.phase, record.accounting ?? 'interval']));
    const rows = [...groups.values()].map(group => phaseRow(group, phases));
    // Wall time comes only from the wrapper, never from a sum of overlapping instance/phase durations.
    const runs = all.filter(record => record.kind === 'RUN');
    return { schema: 1, completeness: errors.length ? 'partial' : 'complete', errors: [...new Set(errors)], rows,
        runs: runs.map(record => ({ phase: record.phase, elapsedMs: record.elapsedMs, outcome: record.outcome,
            unaccountedMs: null, attribution: 'unavailable: process clocks cannot be aligned to wrapper wall time' })) };
}

export function markdown(summary) {
    const escape = value => String(value).replaceAll('|', '\\|').replace(/[\r\n]/g, ' ');
    const milliseconds = value => value === null ? 'unavailable' : value.toFixed(1);
    const lines = [`Timing completeness: **${summary.completeness}**`, '',
        '| Run / SHA / attempt / filter | Mode / provider | Phase | Count | Inclusive ms | Exclusive ms | Process-union ms | Mean / p50 / p95 / max ms | Retries / polls / failures |',
        '| --- | --- | --- | ---: | ---: | ---: | ---: | --- | --- |'];
    for (const row of summary.rows) {
        lines.push(`| ${[row.run, row.sha, row.attempt, row.filter].map(escape).join(' / ')} | ${escape(row.mode)} / ${escape(row.provider)} | ${escape(row.phase)} | ${row.count} | ${row.inclusiveMs.toFixed(1)} | ${milliseconds(row.exclusiveMs)} | ${milliseconds(row.unionMs)} | ${[row.meanMs, row.p50Ms, row.p95Ms, row.maxMs].map(milliseconds).join(' / ')} | ${row.retries} / ${row.polls} / ${row.failures} |`);
    }
    lines.push('', 'Inclusive phases and process unions overlap; do not add them to obtain wall time.',
        'EF command aggregates are reported separately; their sums are not wall time and have no interval union or percentiles.');
    for (const run of summary.runs) lines.push(`${escape(run.phase)} wall: ${run.elapsedMs.toFixed(1)} ms (${escape(run.outcome)}); unaccounted: unavailable.`);
    if (summary.buildTargets?.length) {
        lines.push('', 'Code generation (inclusive worker subset, not wall time):', '', '| Target | Calls | Worker ms |', '| --- | ---: | ---: |');
        for (const target of summary.buildTargets) lines.push(`| ${escape(target.target)} | ${target.count} | ${target.elapsedMs.toFixed(1)} |`);
    } else {
        lines.push('', 'Code generation: unavailable from these inputs (not inferred as zero); --no-build test invocations do not generate code.');
    }
    for (const error of summary.errors) lines.push(`- ${error}`);
    return `${lines.join('\n')}\n`;
}
