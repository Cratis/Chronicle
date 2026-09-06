// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorPattern } from 'Api/Patterns/BehaviorPattern';
import { isInSlot } from '../PatternHeatmapState';
import { aPattern } from './given/a_pattern';

describe('when checking whether a pattern is in a slot', () => {
    let mondayMorning: BehaviorPattern;

    beforeEach(() => {
        mondayMorning = aPattern({ Day: 'Monday', TimeBucket: 'Morning' }, 0.9);
    });

    it('should match its own slot', () => isInSlot(mondayMorning, { day: 'Monday', timeBucket: 'Morning' }).should.be.true);
    it('should not match another day', () => isInSlot(mondayMorning, { day: 'Tuesday', timeBucket: 'Morning' }).should.be.false);
    it('should not match another time of day', () => isInSlot(mondayMorning, { day: 'Monday', timeBucket: 'Evening' }).should.be.false);
});
