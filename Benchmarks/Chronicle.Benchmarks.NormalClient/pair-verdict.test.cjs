// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { verdict, phases } = require('./pair-verdict.cjs');
const sample = () => ({ handlerCount: 12, ...Object.fromEntries(phases.map(phase =>
    [phase, { threadBytesMedian: 10000, millisecondsMedian: 100 }])) });
const pairs = () => [1, 2].map(() => ({ base: sample(), head: sample() }));
const check = value => verdict(value, () => {});

test('same phase exceeding 5% in both pairs fails', () => {
    const data = pairs();
    for (const pair of data) pair.head.dispatch.millisecondsMedian = 106;
    assert.equal(check(data), 1);
});
test('different phases exceeding 5% in different pairs pass', () => {
    const data = pairs();
    data[0].head.dispatch.millisecondsMedian = 106;
    data[1].head.release.millisecondsMedian = 106;
    assert.equal(check(data), 0);
});
test('exactly 5% passes', () => {
    const data = pairs();
    for (const pair of data) pair.head.dispatch.millisecondsMedian = 105;
    assert.equal(check(data), 0);
});
test('allocations use the larger of 1% and 64 bytes, in either pair', () => {
    for (const baseline of [1000, 10000]) {
        for (const index of [0, 1]) {
            const data = pairs();
            const limit = Math.max(baseline * 0.01, 64);
            data[index].base.release.threadBytesMedian = baseline;
            data[index].head.release.threadBytesMedian = baseline + limit;
            assert.equal(check(data), 0);
            data[index].head.release.threadBytesMedian++;
            assert.equal(check(data), 1);
        }
    }
});
test('missing pairs, samples, phases and invalid numeric samples cannot pass', () => {
    assert.throws(() => check([]));
    const mutations = [
        data => { delete data[1].head; },
        data => { delete data[1].head.release; },
        data => { delete data[0].base.dispatch.threadBytesMedian; },
        data => { data[0].base.dispatch.millisecondsMedian = 0; },
        data => { data[1].head.release.millisecondsMedian = NaN; },
        data => { data[1].head.release.threadBytesMedian = Infinity; }
    ];
    for (const mutate of mutations) {
        const data = pairs();
        mutate(data);
        assert.throws(() => check(data));
    }
});
test('handler count mismatch fails', () => {
    const data = pairs();
    data[0].head.handlerCount++;
    assert.equal(check(data), 1);
});
