// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { summaryOf } from '../patternSummary';
import { aPattern } from '../for_PatternHeatmapState/given/a_pattern';

describe('when summarizing a pattern', () => {
    it('should read what happened before when it happened', () =>
        summaryOf(aPattern({ Day: 'Monday', TimeBucket: 'Morning', CommandType: 'ApproveExpenseReport' }, 1))
            .should.equal('ApproveExpenseReport · Monday · Morning'));

    it('should describe a pattern that constrains only the time', () =>
        summaryOf(aPattern({ TimeBucket: 'Night', Day: 'Tuesday' }, 1)).should.equal('Tuesday · Night'));

    it('should put the initiator after the command', () =>
        summaryOf(aPattern({ InitiatorType: 'Agent', CommandType: 'ApproveExpenseReport' }, 1))
            .should.equal('ApproveExpenseReport · Agent'));

    it('should still show a facet it has no opinion on the order of', () =>
        summaryOf(aPattern({ SomethingNew: 'Value', CommandType: 'ApproveExpenseReport' }, 1))
            .should.equal('ApproveExpenseReport · Value'));

    // The broadest patterns constrain nothing, and a wall of cards that all read the same is what this avoids.
    it('should say so when the pattern constrains nothing', () => summaryOf(aPattern({}, 1)).should.equal('Any context'));
});
