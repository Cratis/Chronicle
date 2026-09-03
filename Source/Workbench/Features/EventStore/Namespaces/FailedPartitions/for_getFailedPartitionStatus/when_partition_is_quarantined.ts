// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartition } from 'Api/Observation';
import { FailedPartitionStatus } from '../FailedPartitionStatus';
import { getFailedPartitionStatus } from '../getFailedPartitionStatus';

describe('when partition is quarantined', () => {
    let result: FailedPartitionStatus;

    beforeEach(() => {
        const failedPartition = Object.assign(new FailedPartition(), {
            isResolved: false,
            isQuarantined: true,
        });
        result = getFailedPartitionStatus(failedPartition);
    });

    it('should report quarantined', () =>
        result.should.equal(FailedPartitionStatus.Quarantined));
});
