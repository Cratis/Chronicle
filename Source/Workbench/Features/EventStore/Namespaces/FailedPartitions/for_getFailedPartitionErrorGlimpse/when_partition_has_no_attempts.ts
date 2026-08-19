// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartitionDetails as FailedPartition } from 'Features/Observation';
import { getFailedPartitionErrorGlimpse } from '../getFailedPartitionErrorGlimpse';

describe('when partition has no attempts', () => {
    let result: string;

    beforeEach(() => {
        const failedPartition = Object.assign(new FailedPartition(), { attempts: [] });
        result = getFailedPartitionErrorGlimpse(failedPartition);
    });

    it('should be an empty string', () => result.should.equal(''));
});
