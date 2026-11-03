// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying a partition with template characters in the id', given(a_view_model, context => {
    beforeEach(async () => {
        context.viewModel.partitionEventSourceId = 'Id-{namespace}-$&';
        await context.request();
    });

    it('should confirm the literal id', () => (context.dialogs.showConfirmation.firstCall.args[1] as string).should.contain('Id-{namespace}-$&'));
    it('should execute with the literal id', () => context.replayPartition.partition.should.equal('Id-{namespace}-$&'));
}));
