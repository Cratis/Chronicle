// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Divider, Grid, Stack, Typography } from '@mui/material';
import { DataGrid, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import { AllJobs } from 'API/jobs/AllJobs';
import { JobState } from 'API/jobs/JobState';
import { useRouteParams } from './RouteParams';
import { AllJobSteps } from 'API/jobs/AllJobSteps';
import { JobStatus } from 'API/jobs/JobStatus';

const getStatusText = (status: JobStatus) => {
    switch (status) {
        case JobStatus.none: return 'None';
        case JobStatus.running: return 'Running';
        case JobStatus.completed: return 'Completed';
        case JobStatus.completedWithFailures: return 'Completed with failures';
        case JobStatus.paused: return 'Paused';
        case JobStatus.stopped: return 'Stopped';
    }
}

const columns: GridColDef[] = [
    {
        headerName: 'Name',
        field: 'name',
        width: 300,
    },
    {
        headerName: 'Status',
        field: 'status',
        width: 100,
        valueGetter: (params: GridValueGetterParams<JobState>) => {
            return getStatusText(params.row.status);
        }
    },
    {
        headerName: 'Total steps',
        field: 'progress.totalSteps',
        width: 130,
        valueGetter: (params: GridValueGetterParams<JobState>) => {
            return params.row.progress.totalSteps;
        }
    },
    {
        headerName: 'Successful steps',
        field: 'progress.successFulSteps',
        width: 130,
        valueGetter: (params: GridValueGetterParams<JobState>) => {
            return params.row.progress.successfulSteps;
        }
    },
    {
        headerName: 'Failed steps',
        field: 'progress.failedSteps',
        width: 130,
        valueGetter: (params: GridValueGetterParams<JobState>) => {
            return params.row.progress.failedSteps;
        }
    }
];


export const Jobs = () => {
    const { microserviceId } = useRouteParams();
    const [jobs] = AllJobs.use({ microserviceId });
    const [selectedJob, setSelectedJob] = useState<JobState | undefined>(undefined);
    const [jobSteps] = AllJobSteps.use({ microserviceId, jobId: selectedJob?.id! });

    const jobSelected = (selection: GridRowSelectionModel) => {
        const selectedJobs = selection.map(id => jobs.data.find(job => job.id === id));
        if (selectedJobs.length == 1) {
            setSelectedJob(selectedJobs[0]);
        }
    }

    return (
        <Stack direction="column" style={{ height: '100%' }}>
            <Typography variant='h4'>Jobs</Typography>
            <Divider sx={{ mt: 1, mb: 3 }} />
            <Grid container spacing={2} sx={{ height: '100%' }}>
                <Grid item xs={8}>
                    <DataGrid
                        columns={columns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: JobState) => row.id}
                        onRowSelectionModelChange={jobSelected}
                        rows={jobs.data}
                    />
                </Grid>

                <Grid item xs={4} >
                    {/* <DataGrid
                        columns={columns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: any) => row.identifier}
                        onRowSelectionModelChange={jobSelected}
                        rows={jobs.data}
                    /> */}
                </Grid>
            </Grid>
        </Stack>
    );
};
