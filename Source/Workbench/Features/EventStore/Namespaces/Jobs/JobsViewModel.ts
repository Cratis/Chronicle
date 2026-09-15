// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { DeleteJob, JobSummary, ResumeJob, StopJob } from 'Features/Jobs';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Guid } from '@cratis/fundamentals';

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

    async stop() {
        const command = new StopJob();
        command.eventStore = this._params.eventStore!;
        command.namespace = this._params.namespace!;
        command.jobId = this.selectedJob?.id || Guid.empty;
        await command.execute();
    }

    async resume() {
        const command = new ResumeJob();
        command.eventStore = this._params.eventStore!;
        command.namespace = this._params.namespace!;
        command.jobId = this.selectedJob?.id || Guid.empty;
        await command.execute();
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
