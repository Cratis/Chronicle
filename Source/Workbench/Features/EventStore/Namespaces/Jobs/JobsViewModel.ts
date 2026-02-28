// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { DeleteJob, Job, ResumeJob, StopJob } from 'Api/Jobs';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Guid } from '@cratis/fundamentals';

@injectable()
export class JobsViewModel {

    constructor(@inject('params') private readonly _params: EventStoreAndNamespaceParams) {
    }

    selectedJob: Job | undefined;

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

    async delete() {
        const command = new DeleteJob();
        command.eventStore = this._params.eventStore!;
        command.namespace = this._params.namespace!;
        command.jobId = this.selectedJob?.id || Guid.empty;
        await command.execute();
    }
}
