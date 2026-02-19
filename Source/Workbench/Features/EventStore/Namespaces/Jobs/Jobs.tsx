// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { DataPage, MenuItem } from 'Components';
import { Column } from 'primereact/column';
import * as faIcons from 'react-icons/fa6';
import { AllJobs, AllJobsParameters } from 'Api/Jobs';
import { Job, JobStatus } from 'Api/Jobs';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { JobsViewModel } from './JobsViewModel';

const jobStatus = (job: Job) => {
    switch (job.status) {
        case JobStatus.none:
            return strings.eventStore.namespaces.jobs.status.none;
        case JobStatus.preparingJob:
            return strings.eventStore.namespaces.jobs.status.preparingJob;
        case JobStatus.preparingSteps:
            return strings.eventStore.namespaces.jobs.status.preparingSteps;
        case JobStatus.startingSteps:
            return strings.eventStore.namespaces.jobs.status.startingSteps;
        case JobStatus.running:
            return strings.eventStore.namespaces.jobs.status.running;
        case JobStatus.completedSuccessfully:
            return strings.eventStore.namespaces.jobs.status.completedSuccessfully;
        case JobStatus.completedWithFailures:
            return strings.eventStore.namespaces.jobs.status.completedWithFailures;
        case JobStatus.stopped:
            return strings.eventStore.namespaces.jobs.status.stopped;
        case JobStatus.failed:
            return strings.eventStore.namespaces.jobs.status.failed;
        case JobStatus.removing:
            return strings.eventStore.namespaces.jobs.status.removing;
    }
    return strings.eventStore.namespaces.jobs.status.none;
};

const progress = (job: Job) => {
    const completedSteps = job.progress.failedSteps + job.progress.successfulSteps;
    const progress = (completedSteps / job.progress.totalSteps) * 100;
    return `${Math.abs(progress).toFixed()}%`;
};

export const Jobs = withViewModel(JobsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AllJobsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.jobs.title}
            query={AllJobs}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.jobs.empty}
            dataKey='id'
            onSelectionChange={e => viewModel.selectedJob = e.value as Job}>
            <DataPage.MenuItems>
                <MenuItem
                    id="stop"
                    label={strings.eventStore.namespaces.jobs.actions.stop} icon={faIcons.FaStop}
                    disableOnUnselected
                    command={() => viewModel.stop()} />
                <MenuItem
                    id="resume"
                    label={strings.eventStore.namespaces.jobs.actions.resume} icon={faIcons.FaPlay}
                    disableOnUnselected
                    command={() => viewModel.resume()} />
                <MenuItem
                    id="delete"
                    label={strings.eventStore.namespaces.jobs.actions.delete} icon={faIcons.FaDeleteLeft}
                    disableOnUnselected
                    command={() => viewModel.delete()} />
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column field='type' header={strings.eventStore.namespaces.jobs.columns.type} sortable />
                <Column field='name' header={strings.eventStore.namespaces.jobs.columns.name} sortable />
                <Column field='details' header={strings.eventStore.namespaces.jobs.columns.details} sortable />
                <Column
                    field='status'
                    header={strings.eventStore.namespaces.jobs.columns.status}
                    sortable
                    body={jobStatus} />

                <Column
                    field='progress'
                    header={strings.eventStore.namespaces.jobs.columns.progress}
                    sortable
                    body={progress} />
            </DataPage.Columns>
        </DataPage>
    );
});
