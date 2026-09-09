// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartitionDetails as FailedPartition } from 'Features/Observation';
import { FailedPartitionStatus } from '../FailedPartitionStatus';
import { getFailedPartitionStatus } from '../getFailedPartitionStatus';

describe('when partition state is not provided', () => {
    let result: FailedPartitionStatus;

    beforeEach(() => {
        result = getFailedPartitionStatus(new FailedPartition());
    });

    it('should report unknown', () => result.should.equal(FailedPartitionStatus.Unknown));
});
