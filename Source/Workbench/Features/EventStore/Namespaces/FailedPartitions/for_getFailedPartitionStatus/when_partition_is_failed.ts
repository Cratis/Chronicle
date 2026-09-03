// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartition } from 'Api/Observation';
import { FailedPartitionStatus } from '../FailedPartitionStatus';
import { getFailedPartitionStatus } from '../getFailedPartitionStatus';

describe('when partition is failed', () => {
    let result: FailedPartitionStatus;

    beforeEach(() => {
        const failedPartition = Object.assign(new FailedPartition(), {
            isResolved: false,
            isQuarantined: false,
        });
        result = getFailedPartitionStatus(failedPartition);
    });

    it('should report failed', () => result.should.equal(FailedPartitionStatus.Failed));
});
