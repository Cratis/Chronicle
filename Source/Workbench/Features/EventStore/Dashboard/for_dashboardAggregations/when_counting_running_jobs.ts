// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { JobSummary } from 'Features/Jobs';
import { JobStatus } from 'Features/Contracts/Jobs';
import { countRunningJobs } from '../dashboardAggregations';

const jobWithStatus = (status: JobStatus) => {
    const job = new JobSummary();
    job.status = status;
    return job;
};

describe('when counting running jobs', () => {
    const jobs = [
        jobWithStatus(JobStatus.running),
        jobWithStatus(JobStatus.running),
        jobWithStatus(JobStatus.completedSuccessfully),
        jobWithStatus(JobStatus.failed)
    ];

    it('should only count jobs that are running', () => countRunningJobs(jobs).should.equal(2));
});

describe('when counting running jobs in an empty list', () => {
    it('should return zero', () => countRunningJobs([]).should.equal(0));
});
