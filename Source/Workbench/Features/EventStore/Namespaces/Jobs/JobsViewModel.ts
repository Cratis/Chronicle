// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { DeleteJob, JobCommandRefusal, JobSummary, ResumeJobWithResult, StopJobWithResult } from 'Features/Jobs';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Guid } from '@cratis/fundamentals';

const describeRefusal = (refusal: JobCommandRefusal | undefined): string => {
    switch (refusal) {
        case JobCommandRefusal.notFound: return 'it could not be found';
        case JobCommandRefusal.jobIsCompleted: return 'it has already completed';
        case JobCommandRefusal.jobIsNotRunning: return 'it is not running';
        case JobCommandRefusal.couldNotPersist: return 'the change could not be saved, so it was left as it was';
        case JobCommandRefusal.notResumed: return 'nothing took it forward';
        default: return 'it was refused';
    }
};

@injectable()
export class JobsViewModel {

    constructor(@inject('params') private readonly _params: EventStoreAndNamespaceParams) {
    }

    selectedJob: JobSummary | undefined;
    selectedJobIds: Guid[] = [];

    isJobSelected(id: Guid): boolean {
        return this.selectedJobIds.some(_ => _.equals(id));
    }

    toggleJobSelection(id: Guid) {
        this.selectedJobIds = this.isJobSelected(id)
            ? this.selectedJobIds.filter(_ => !_.equals(id))
            : [...this.selectedJobIds, id];
    }

    selectAllJobs(ids: Guid[]) {
        this.selectedJobIds = [...ids];
    }

    clearJobSelection() {
        this.selectedJobIds = [];
    }

    /**
     * Asks the selected job to stop. A job that refuses is reported as an error carrying the reason rather
     * than being shown as stopped.
     */
    async stop() {
        const command = this.createStopJob();
        command.eventStore = this._params.eventStore!;
        command.namespace = this._params.namespace!;
        command.jobId = this.selectedJob?.id || Guid.empty;
        const result = await command.execute();
        this.throwIfRefused('stop', result.response?.accepted, result.response?.refusal);
    }

    /**
     * Asks the selected job to resume. A job that nothing took forward is reported as an error carrying the
     * reason rather than being shown as resumed.
     */
    async resume() {
        const command = this.createResumeJob();
        command.eventStore = this._params.eventStore!;
        command.namespace = this._params.namespace!;
        command.jobId = this.selectedJob?.id || Guid.empty;
        const result = await command.execute();
        this.throwIfRefused('resume', result.response?.accepted, result.response?.refusal);
    }

    protected createStopJob(): StopJobWithResult {
        return new StopJobWithResult();
    }

    protected createResumeJob(): ResumeJobWithResult {
        return new ResumeJobWithResult();
    }

    private throwIfRefused(action: string, accepted: boolean | undefined, refusal: JobCommandRefusal | undefined) {
        // A kernel older than the response carries none; the command succeeding is all it can say.
        if (accepted === undefined || accepted) {
            return;
        }

        throw new Error(`The job did not ${action}: ${describeRefusal(refusal)}.`);
    }

    /**
     * Deletes every job in the given selection. Partial failures are collected and thrown as a
     * single aggregate error once every deletion has been attempted, so one failing job among many
     * does not swallow the others - and does not swallow itself either.
     * @param jobIds The identifiers of the jobs to delete.
     */
    async deleteJobs(jobIds: Guid[]) {
        const failures: string[] = [];

        for (const jobId of jobIds) {
            const command = new DeleteJob();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.jobId = jobId;

            // eslint-disable-next-line no-await-in-loop
            const result = await command.execute();
            result.onFailed(() => {
                const messages = [...result.validationResults.map(_ => _.message), ...result.exceptionMessages];
                failures.push(`${jobId.toString()}: ${messages.join(', ') || 'Unknown error'}`);
            });
        }

        this.clearJobSelection();

        if (failures.length > 0) {
            throw new Error(`Failed to delete ${failures.length} of ${jobIds.length} job(s):\n${failures.join('\n')}`);
        }
    }
}
