// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartition, FailedPartitionAttempt } from 'Api/Observation';
import { getFailedPartitionErrorGlimpse } from '../getFailedPartitionErrorGlimpse';

const attempt = (sequenceNumber: number, messages: string[]): FailedPartitionAttempt =>
    Object.assign(new FailedPartitionAttempt(), { occurred: new Date(), sequenceNumber, messages, stackTrace: '' });

describe('when partition has attempts', () => {
    let result: string;

    beforeEach(() => {
        const failedPartition = Object.assign(new FailedPartition(), {
            attempts: [attempt(1, ['old error']), attempt(2, ['first line', 'second line'])]
        });
        result = getFailedPartitionErrorGlimpse(failedPartition);
    });

    it('should use the messages of the most recent attempt', () => result.should.contain('first line'));
    it('should not use the messages of older attempts', () => result.should.not.contain('old error'));
    it('should join multiple messages with a new line', () => result.should.equal('first line\nsecond line'));
});
