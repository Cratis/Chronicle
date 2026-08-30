// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorPattern } from 'Api/Patterns/BehaviorPattern';
import { patternsInSlot } from '../PatternHeatmapState';
import { aPattern } from './given/a_pattern';

describe('when listing the patterns in a slot', () => {
    let strong: BehaviorPattern;
    let elsewhere: BehaviorPattern;
    let result: BehaviorPattern[];

    beforeEach(() => {
        const weak = aPattern({ Day: 'Monday', TimeBucket: 'Morning', CommandType: 'RejectExpenseReport' }, 0.4);
        strong = aPattern({ Day: 'Monday', TimeBucket: 'Morning', CommandType: 'ApproveExpenseReport' }, 0.9);
        elsewhere = aPattern({ Day: 'Friday', TimeBucket: 'Evening', CommandType: 'SubmitExpenseReport' }, 0.7);
        result = patternsInSlot([weak, strong, elsewhere], { day: 'Monday', timeBucket: 'Morning' });
    });

    it('should return only the patterns in the slot', () => result.length.should.equal(2));
    it('should rank the most confident first', () => result[0].should.equal(strong));
    it('should not reach into another slot', () => result.includes(elsewhere).should.be.false);
});
