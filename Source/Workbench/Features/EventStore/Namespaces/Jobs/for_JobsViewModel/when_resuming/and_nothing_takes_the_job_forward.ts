// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { JobCommandRefusal } from 'Features/Jobs';
import { a_view_model_with_a_selected_job } from '../when_stopping/given/a_view_model_with_a_selected_job';

describe('when resuming and nothing takes the job forward', given(a_view_model_with_a_selected_job, (context) => {
    let error: Error | undefined;

    beforeAll(async () => {
        context.answerResumeWith(false, JobCommandRefusal.notResumed);
        try {
            await context.viewModel.resume();
        } catch (e) {
            error = e as Error;
        }
    });

    it('should report an error', () => (error !== undefined).should.be.true);
    it('should say nothing took it forward', () => error!.message.should.contain('nothing took it forward'));
}));
