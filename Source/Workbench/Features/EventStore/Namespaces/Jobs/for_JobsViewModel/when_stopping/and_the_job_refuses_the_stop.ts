// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { JobCommandRefusal } from 'Features/Jobs';
import { a_view_model_with_a_selected_job } from './given/a_view_model_with_a_selected_job';

describe('when stopping and the job refuses the stop', given(a_view_model_with_a_selected_job, (context) => {
    let error: Error | undefined;

    beforeAll(async () => {
        context.answerStopWith(false, JobCommandRefusal.couldNotPersist);
        try {
            await context.viewModel.stop();
        } catch (e) {
            error = e as Error;
        }
    });

    it('should report an error', () => (error !== undefined).should.be.true);
    it('should say why it was refused', () => error!.message.should.contain('could not be saved'));
}));
