// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { DataPage, MenuItem } from 'Components';
import { Column } from 'primereact/column';
import * as faIcons from 'react-icons/fa6';
import { AllJobs, AllJobsArguments, JobInformation, JobStatus } from 'Api/Jobs';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

const jobStatus = (job: JobInformation) => {
    switch (job.status) {
        case JobStatus.none:
            return strings.eventStore.namespaces.jobs.status.none;
        case JobStatus.preparing:
            return strings.eventStore.namespaces.jobs.status.preparing;
        case JobStatus.preparingSteps:
            return strings.eventStore.namespaces.jobs.status.preparingSteps;
        case JobStatus.preparingStepsForRunning:
            return strings.eventStore.namespaces.jobs.status.preparingStepsForRunning;
        case JobStatus.startingSteps:
            return strings.eventStore.namespaces.jobs.status.startingSteps;
        case JobStatus.running:
            return strings.eventStore.namespaces.jobs.status.running;
        case JobStatus.completedSuccessfully:
            return strings.eventStore.namespaces.jobs.status.completedSuccessfully;
        case JobStatus.completedWithFailures:
            return strings.eventStore.namespaces.jobs.status.completedWithFailures;
        case JobStatus.paused:
            return strings.eventStore.namespaces.jobs.status.paused;
        case JobStatus.stopped:
            return strings.eventStore.namespaces.jobs.status.stopped;
        case JobStatus.failed:
            return strings.eventStore.namespaces.jobs.status.failed;
    }
    return strings.eventStore.namespaces.jobs.status.none;
};

const progress = (job: JobInformation) => {
    const completedSteps = job.progress.failedSteps + job.progress.successfulSteps;
    const progress = completedSteps / job.progress.totalSteps;
    return `${Math.abs(progress)}%`;
};

export const Jobs = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AllJobsArguments = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.jobs.title}
            query={AllJobs}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.jobs.empty}
            dataKey='observerId'>
            <DataPage.MenuItems>
                <MenuItem
                    id="stop"
                    label={strings.eventStore.namespaces.jobs.actions.pause} icon={faIcons.FaPause}
                    disableOnUnselected
                    command={() => { }} />
                <MenuItem
                    id="stop"
                    label={strings.eventStore.namespaces.jobs.actions.stop} icon={faIcons.FaStop}
                    disableOnUnselected
                    command={() => { }} />
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
};
