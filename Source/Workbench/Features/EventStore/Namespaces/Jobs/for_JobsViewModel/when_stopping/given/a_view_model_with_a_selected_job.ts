// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { JobCommandRefusal, JobSummary, ResumeJob, ResumeJobOutcome, StopJob, StopJobOutcome } from 'Features/Jobs';
import { JobsViewModel } from '../../../JobsViewModel';

class TestableJobsViewModel extends JobsViewModel {
    stopJob = new StopJob();
    resumeJob = new ResumeJob();

    protected createStopJob(): StopJob {
        return this.stopJob;
    }

    protected createResumeJob(): ResumeJob {
        return this.resumeJob;
    }
}

const executed = (response: object) => ({ response }) as never;

export class a_view_model_with_a_selected_job {
    viewModel: TestableJobsViewModel;

    constructor() {
        this.viewModel = new TestableJobsViewModel({ eventStore: 'some-event-store', namespace: 'some-namespace' });
        this.viewModel.selectedJob = Object.assign(new JobSummary(), { id: Guid.parse('11111111-1111-1111-1111-111111111111') });
    }

    answerStopWith(accepted: boolean, refusal: JobCommandRefusal) {
        sinon.stub(this.viewModel.stopJob, 'execute').resolves(executed(Object.assign(new StopJobOutcome(), { accepted, refusal })));
    }

    answerResumeWith(accepted: boolean, refusal: JobCommandRefusal) {
        sinon.stub(this.viewModel.resumeJob, 'execute').resolves(executed(Object.assign(new ResumeJobOutcome(), { accepted, refusal })));
    }
}
