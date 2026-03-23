// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { an_event_store_path } from './given/an_event_store_path';
import { replaceEventStoreNamespaceInPath } from '../../replaceEventStoreNamespaceInPath';

describe('when replacing namespace and the current namespace does not exist after the event store', given(an_event_store_path, (context) => {
    let result: string;

    beforeEach(() => {
        result = replaceEventStoreNamespaceInPath('/event-store/event-store/observers', context.eventStore, context.currentNamespace, context.nextNamespace);
    });

    it('should return the original path', () => {
        result.should.equal('/event-store/event-store/observers');
    });
}));
