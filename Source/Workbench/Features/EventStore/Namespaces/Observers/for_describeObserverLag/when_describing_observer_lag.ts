// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describeObserverLag } from '../describeObserverLag';

describe('when describing observer lag', () => {
    it('should say it is loading before the first read has completed', () => describeObserverLag(undefined, false).should.equal('Loading…'));
    it('should say it is unavailable when the read completed without a usable result', () => describeObserverLag(undefined, true).should.equal('Unavailable'));

    it('should say caught up with the distance and age', () => {
        describeObserverLag({ state: 'caughtUp', distance: '0', ageSeconds: 45 }, true).should.equal('Caught up · 0 slots · sampled 45s ago');
    });

    it('should not say caught up when no sample exists', () => {
        describeObserverLag({ state: 'noSample' }, true).should.equal('No sample yet');
    });

    it('should not show a distance it does not have', () => {
        describeObserverLag({ state: 'lagging', ageSeconds: 5 }, true).should.equal('Lagging · sampled 5s ago');
    });
});
