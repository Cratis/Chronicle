// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverOwner } from 'Api/Observation';
import { renderObserverOwner } from '../../renderObserverOwner';
import strings from 'Strings';

describe('when rendering observer owner with none owner', () => {
    let result: string;

    beforeEach(() => {
        const observer = new ObserverInformation();
        observer.owner = ObserverOwner.none;
        result = renderObserverOwner(observer);
    });

    it('should return the none label', () => {
        result.should.equal(strings.eventStore.namespaces.observers.owners.none);
    });
});
