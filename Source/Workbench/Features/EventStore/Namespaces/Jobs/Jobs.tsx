// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import * as faIcons from 'react-icons/fa6';
import { ObserveJobs, ObserveJobsParameters } from 'Features/Jobs';
import { JobSummary } from 'Features/Jobs';
import { JobStatus } from 'Features/Contracts/Jobs';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { JobsViewModel } from './JobsViewModel';
import { Page } from 'Components/Common/Page';
import { SelectionCheckbox } from 'Components/Common/SelectionCheckbox';

const jobStatus = (job: JobSummary) => {
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

const progress = (job: JobSummary) => {
    const completedSteps = job.progress.failedSteps + job.progress.successfulSteps;
    const progress = (completedSteps / job.progress.totalSteps) * 100;
    return `${Math.abs(progress).toFixed()}%`;
};

export const Jobs = withViewModel(JobsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [showConfirmation] = useConfirmationDialog();
    const queryArgs: ObserveJobsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };
    const [jobs] = ObserveJobs.use(queryArgs);

    const handleDelete = async () => {
        const jobIds = viewModel.selectedJobIds;
        if (jobIds.length === 0) {
            return;
        }

        if (jobIds.length > 1) {
            const result = await showConfirmation(
                strings.eventStore.namespaces.jobs.dialogs.deleteJobs.title,
                strings.eventStore.namespaces.jobs.dialogs.deleteJobs.message.replace('{count}', jobIds.length.toString()),
                DialogButtons.YesNo);

            if (result !== DialogResult.Yes) {
                return;
            }
        }

        try {
            await viewModel.deleteJobs(jobIds);
        } catch (error) {
            console.error('Failed to delete jobs:', error);
        }
    };

    return (
        <Page title={strings.eventStore.namespaces.jobs.title}>
        <DataPage
            title={strings.eventStore.namespaces.jobs.title}
            query={ObserveJobs}
            queryArguments={queryArgs}
            emptyMessage={strings.eventStore.namespaces.jobs.empty}
            dataKey='id'
            onSelectionChange={e => viewModel.selectedJob = e.value as JobSummary}>
            <DataPage.MenuItems>
                <MenuItem
                    label={strings.eventStore.namespaces.jobs.actions.selectAll} icon={faIcons.FaSquareCheck}
                    command={() => viewModel.selectAllJobs(jobs.data.map(job => job.id))} />
                <MenuItem
                    label={strings.eventStore.namespaces.jobs.actions.stop} icon={faIcons.FaStop}
                    disableOnUnselected
                    command={() => viewModel.stop()} />
                <MenuItem
                    label={strings.eventStore.namespaces.jobs.actions.resume} icon={faIcons.FaPlay}
                    disableOnUnselected
                    command={() => viewModel.resume()} />
                <MenuItem
                    label={strings.eventStore.namespaces.jobs.actions.delete} icon={faIcons.FaDeleteLeft}
                    disabled={viewModel.selectedJobIds.length === 0}
                    command={() => handleDelete()} />
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column
                    body={(job: JobSummary) => (
                        <SelectionCheckbox
                            checked={viewModel.isJobSelected(job.id)}
                            onToggle={() => viewModel.toggleJobSelection(job.id)} />
                    )} />
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
        </Page>
    );
});
