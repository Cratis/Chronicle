// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { an_observer } from './given/an_observer';
import { buildObserverSummaryRows } from '../../buildObserverSummaryRows';
import { ObserverSummaryRow } from '../../ObserverSummaryRow';
import strings from 'Strings';

describe('when building summary rows and observer is neither subscribed nor replayable', given(an_observer, (context) => {
    let result: ObserverSummaryRow[];

    beforeEach(() => {
        context.observer.isSubscribed = false;
        context.observer.isReplayable = false;
        result = buildObserverSummaryRows(context.observer);
    });

    it('should render the subscribed flag as No', () => {
        const subscribedRow = result.find(row => row.label === strings.eventStore.namespaces.observers.details.summary.isSubscribed);
        subscribedRow!.value.should.equal('No');
    });

    it('should render the replayable flag as No', () => {
        const replayableRow = result.find(row => row.label === strings.eventStore.namespaces.observers.details.summary.isReplayable);
        replayableRow!.value.should.equal('No');
    });
}));
