// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverType } from 'Api/Observation';
import { renderObserverType } from '../../renderObserverType';
import strings from 'Strings';

describe('when rendering observer type with projection type', () => {
    let result: string;

    beforeEach(() => {
        const observer = new ObserverInformation();
        observer.type = ObserverType.projection;
        result = renderObserverType(observer);
    });

    it('should return the projection label', () => {
        result.should.equal(strings.eventStore.namespaces.observers.types.projection);
    });
});
