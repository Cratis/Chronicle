// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { an_event_store_path } from './given/an_event_store_path';
import { replaceEventStoreNamespaceInPath } from '../../replaceEventStoreNamespaceInPath';

describe('when replacing namespace and the event store does not exist in the path', given(an_event_store_path, (context) => {
    let result: string;

    beforeEach(() => {
        result = replaceEventStoreNamespaceInPath('/event-store/another-store/Default/observers', context.eventStore, context.currentNamespace, context.nextNamespace);
    });

    it('should return the original path', () => {
        result.should.equal('/event-store/another-store/Default/observers');
    });
}));
