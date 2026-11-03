// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { FailureKind } from 'Features/Concepts/Observation';
import { getFailureKindLabel } from '../getFailureKindLabel';

describe('when getting the label for an unrecognized kind', () => {
    let result: string;

    beforeEach(() => {
        result = getFailureKindLabel(42 as FailureKind);
    });

    it('should fall back to the unknown label', () => result.should.equal(strings.eventStore.namespaces.failedPartitionGroups.kinds.unknown));
});
