// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { FailureKind } from 'Features/Concepts/Observation';
import { getFailureKindLabel } from '../getFailureKindLabel';

describe('when getting the label for a known kind', () => {
    let result: string;

    beforeEach(() => {
        result = getFailureKindLabel(FailureKind.timeout);
    });

    it('should use the label of the kind', () => result.should.equal(strings.eventStore.namespaces.failedPartitionGroups.kinds.timeout));
});
