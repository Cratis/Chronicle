// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartition, FailedPartitionAttempt } from 'Api/Observation';
import { getFailedPartitionAttemptsNewestFirst } from '../getFailedPartitionAttemptsNewestFirst';

const attempt = (sequenceNumber: number): FailedPartitionAttempt =>
    Object.assign(new FailedPartitionAttempt(), { occurred: new Date(), sequenceNumber, messages: [], stackTrace: '' });

describe('when partition has multiple attempts', () => {
    let original: FailedPartitionAttempt[];
    let result: FailedPartitionAttempt[];

    beforeEach(() => {
        original = [attempt(1), attempt(2), attempt(3)];
        const failedPartition = Object.assign(new FailedPartition(), { attempts: original });
        result = getFailedPartitionAttemptsNewestFirst(failedPartition);
    });

    it('should order the most recent attempt first', () => result[0].sequenceNumber.should.equal(3));
    it('should order the oldest attempt last', () => result[2].sequenceNumber.should.equal(1));
    it('should not mutate the source attempts order', () => original[0].sequenceNumber.should.equal(1));
});
