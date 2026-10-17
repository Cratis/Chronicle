// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { JobCommandRefusal } from 'Features/Jobs';
import { a_view_model_with_a_selected_job } from './given/a_view_model_with_a_selected_job';

describe('when stopping and the job accepts the stop', given(a_view_model_with_a_selected_job, (context) => {
    let error: unknown;

    beforeAll(async () => {
        context.answerStopWith(true, JobCommandRefusal.notRefused);
        try {
            await context.viewModel.stop();
        } catch (e) {
            error = e;
        }
    });

    it('should not report an error', () => (error === undefined).should.be.true);
}));
