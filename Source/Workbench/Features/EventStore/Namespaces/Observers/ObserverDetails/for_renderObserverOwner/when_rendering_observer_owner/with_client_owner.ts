// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverOwner } from 'Api/Observation';
import { renderObserverOwner } from '../../renderObserverOwner';
import strings from 'Strings';

describe('when rendering observer owner with client owner', () => {
    let result: string;

    beforeEach(() => {
        const observer = new ObserverInformation();
        observer.owner = ObserverOwner.client;
        result = renderObserverOwner(observer);
    });

    it('should return the client label', () => {
        result.should.equal(strings.eventStore.namespaces.observers.owners.client);
    });
});
