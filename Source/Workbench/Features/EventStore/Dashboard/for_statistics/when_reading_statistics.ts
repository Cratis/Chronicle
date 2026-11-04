// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readStatistics, StatisticsResultLike } from '../statistics';

const read = (result: StatisticsResultLike | undefined | null) => readStatistics(result);

describe('when reading statistics that are still loading', () => {
    const result = read({ isReady: false, isSuccess: false, data: {} });

    it('should have no total', () => (result.totalEvents === undefined).should.be.true);
    it('should have no event type counts', () => (result.perEventType === undefined).should.be.true);
    it('should not be failed', () => result.failed.should.be.false);
});

describe('when reading statistics from a failed query such as a missing read model', () => {
    const result = read({ isReady: true, isSuccess: false, data: undefined });

    it('should have no total', () => (result.totalEvents === undefined).should.be.true);
    it('should have no event type counts', () => (result.perEventType === undefined).should.be.true);
    it('should be failed', () => result.failed.should.be.true);
});

describe('when reading a missing result', () => {
    const result = read(undefined);

    it('should have no event type counts', () => (result.perEventType === undefined).should.be.true);
    it('should not be failed', () => result.failed.should.be.false);
});

describe('when reading a successful result without data', () => {
    const result = read({ isReady: true, isSuccess: true, data: null });

    it('should have a total of zero', () => result.totalEvents!.should.equal(0));
    it('should have empty event type counts', () => result.perEventType!.length.should.equal(0));
    it('should not be failed', () => result.failed.should.be.false);
});

describe('when reading a successful result with missing fields', () => {
    const result = read({ isReady: true, isSuccess: true, data: {} });

    it('should have a total of zero', () => result.totalEvents!.should.equal(0));
    it('should have empty event type counts', () => result.perEventType!.length.should.equal(0));
});

describe('when reading a successful result with null event type counts', () => {
    const result = read({ isReady: true, isSuccess: true, data: { totalEvents: 0, perEventType: null } });

    it('should have empty event type counts', () => result.perEventType!.length.should.equal(0));
});

describe('when reading a successful result with data', () => {
    const counts = [{ eventType: 'A', count: 3 }, { eventType: 'B', count: 2 }];
    const result = read({ isReady: true, isSuccess: true, data: { totalEvents: 5, perEventType: counts } });

    it('should have the total', () => result.totalEvents!.should.equal(5));
    it('should have the event type counts', () => result.perEventType!.should.deep.equal(counts));
    it('should not be failed', () => result.failed.should.be.false);
});

describe('when reading a successful result with a bigint total', () => {
    const result = read({ isReady: true, isSuccess: true, data: { totalEvents: 7n, perEventType: [] } });

    it('should have the total as a number', () => result.totalEvents!.should.equal(7));
});
