// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { an_observer } from './given/an_observer';
import { buildObserverSummaryRows } from '../../buildObserverSummaryRows';
import { ObserverSummaryRow } from '../../ObserverSummaryRow';
import strings from 'Strings';

describe('when building summary rows and observer is subscribed and replayable', given(an_observer, (context) => {
    let result: ObserverSummaryRow[];

    beforeEach(() => {
        context.observer.isSubscribed = true;
        context.observer.isReplayable = true;
        result = buildObserverSummaryRows(context.observer);
    });

    it('should produce ten rows in order', () => {
        result.should.have.lengthOf(10);
    });

    it('should expose the observer identifier', () => {
        result[0].label.should.equal(strings.eventStore.namespaces.observers.details.summary.id);
        result[0].value.should.equal('observer-id');
    });

    it('should render the subscribed flag as Yes', () => {
        const subscribedRow = result.find(row => row.label === strings.eventStore.namespaces.observers.details.summary.isSubscribed);
        subscribedRow!.value.should.equal('Yes');
    });

    it('should render the replayable flag as Yes', () => {
        const replayableRow = result.find(row => row.label === strings.eventStore.namespaces.observers.details.summary.isReplayable);
        replayableRow!.value.should.equal('Yes');
    });
}));
