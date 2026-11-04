// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
const fs = require('node:fs');
const phases = ['reducerDiscovery', 'projectionDiscovery', 'dispatch', 'release'];

function verdict(pairs, report = console.log) {
    if (!Array.isArray(pairs) || pairs.length !== 2) throw new Error('Two complete pairs required');
    const timingFailures = new Map(phases.map(phase => [phase, 0]));
    let allocationFail = false;
    for (const [index, pair] of pairs.entries()) {
        for (const sample of [pair.base, pair.head]) {
            if (!sample || !Number.isInteger(sample.handlerCount) || sample.handlerCount <= 0) throw new Error('Missing handler count');
            for (const phase of phases) {
                const cost = sample[phase];
                if (!cost || !Number.isFinite(cost.threadBytesMedian) || cost.threadBytesMedian < 0 ||
                    !Number.isFinite(cost.millisecondsMedian) || cost.millisecondsMedian <= 0) throw new Error(`Missing/invalid sample: ${phase}`);
            }
        }
        if (pair.base.handlerCount !== pair.head.handlerCount) allocationFail = true;
        for (const phase of phases) {
            const b = pair.base[phase], h = pair.head[phase];
            const delta = h.threadBytesMedian - b.threadBytesMedian;
            const limit = Math.max(b.threadBytesMedian * 0.01, 64);
            const ratio = h.millisecondsMedian / b.millisecondsMedian;
            if (delta > limit) allocationFail = true;
            if (ratio > 1.05) timingFailures.set(phase, timingFailures.get(phase) + 1);
            report(`pair ${index + 1} ${phase}: allocation delta=${delta} limit=${limit}; time ratio=${ratio}`);
        }
    }
    const timeFail = [...timingFailures.values()].some(count => count === 2);
    report(`allocations: ${allocationFail ? 'FAIL' : 'PASS'}; time (same phase >5% in both pairs): ${timeFail ? 'FAIL' : 'PASS'}`);
    return allocationFail || timeFail ? 1 : 0;
}

module.exports = { verdict, phases };
if (require.main === module) {
    try {
        const pairs = [1, 2].map(pair => Object.fromEntries(['base', 'head'].map(label =>
            [label, JSON.parse(fs.readFileSync(`${process.argv[2]}/${pair}-${label}.json`, 'utf8'))])));
        process.exitCode = verdict(pairs);
    } catch (error) {
        console.error(error.message);
        process.exitCode = 2;
    }
}
