// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { Button, Divider, FormControl, Grid, InputLabel, MenuItem, Select, Stack, Toolbar, Typography } from '@mui/material';
import { DataGrid, GridColDef, GridRowSelectionModel, GridValueGetterParams } from '@mui/x-data-grid';
import { AllTenants } from 'API/configuration/tenants/AllTenants';
import { TenantInfo } from 'API/configuration/tenants/TenantInfo';
import { AllJobs } from 'API/events/store/jobs/AllJobs';
import { JobState } from 'API/events/store/jobs/JobState';
import { useRouteParams } from './RouteParams';
import { AllJobSteps } from 'API/events/store/jobs/AllJobSteps';
import { JobStatus } from 'API/events/store/jobs/JobStatus';
import { JobStepState } from 'API/events/store/jobs/JobStepState';
import { JobStepStatus } from 'API/events/store/jobs/JobStepStatus';
import * as icons from '@mui/icons-material';
import { StopJob } from 'API/events/store/jobs/StopJob';
import { DeleteJob } from 'API/events/store/jobs/DeleteJob';
import { ResumeJob } from 'API/events/store/jobs/ResumeJob';

const getJobStatusText = (status: JobStatus) => {
    switch (status) {
        case JobStatus.none: return 'None';
        case JobStatus.preparing: return 'Preparing job';
        case JobStatus.preparingSteps: return 'Preparing steps';
        case JobStatus.running: return 'Running';
        case JobStatus.completedSuccessfully: return 'Completed successfully';
        case JobStatus.completedWithFailures: return 'Completed with failures';
        case JobStatus.paused: return 'Paused';
        case JobStatus.stopped: return 'Stopped';
    }
}

const getJobStepStatusText = (status: JobStepStatus) => {
    switch (status) {
        case JobStepStatus.unknown: return 'None';
        case JobStepStatus.scheduled: return 'Scheduled';
        case JobStepStatus.running: return 'Running';
        case JobStepStatus.succeeded: return 'Succeeded';
        case JobStepStatus.failed: return 'Failed';
        case JobStepStatus.paused: return 'Paused';
        case JobStepStatus.stopped: return 'Stopped';
    }
}

const jobColumns: GridColDef[] = [
    {
        headerName: 'Name',
        field: 'name',
        width: 300,
    },
    {
        headerName: 'Details',
        field: 'details',
        width: 300,
    },
    {
        headerName: 'Status',
        field: 'status',
        width: 200,
        valueGetter: (params: GridValueGetterParams<JobState>) => {
            return getJobStatusText(params.row.status);
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
    },
    {
        headerName: 'Start time',
        field: 'startTime',
        width: 200,
        valueGetter: (params: GridValueGetterParams<JobState>) => {
            return params.row.statusChanges[0].occurred.toLocaleString();
        }
    },
    {
        headerName: 'Completed time',
        field: 'completedTime',
        width: 200,
        valueGetter: (params: GridValueGetterParams<JobState>) => {
            if (params.row.statusChanges.length > 1) {
                const lastStatusChange = params.row.statusChanges.at(-1)!;
                if (lastStatusChange.status == JobStatus.completedSuccessfully ||
                    lastStatusChange.status == JobStatus.stopped ||
                    lastStatusChange.status == JobStatus.completedWithFailures) {
                    return lastStatusChange.occurred.toLocaleString();
                }
            }

            return '[N/A]';
        }
    }

];

const jobStepColumns: GridColDef[] = [
    {
        headerName: 'Name',
        field: 'name',
        width: 300,
    },
    {
        headerName: 'Status',
        field: 'status',
        width: 200,
        valueGetter: (params: GridValueGetterParams<JobStepState>) => {
            return getJobStepStatusText(params.row.status);
        }
    },
]


export const Jobs = () => {
    const { microserviceId } = useRouteParams();
    const [tenants] = AllTenants.use();
    const [selectedTenant, setSelectedTenant] = useState<TenantInfo>();
    const [resumeJob, setResumeJobValues] = ResumeJob.use()
    const [stopJob, setStopJobValues] = StopJob.use()
    const [deleteJob, setDeleteJobValues] = DeleteJob.use()

    const [jobs] = AllJobs.use({
        microserviceId,
        tenantId: selectedTenant?.id || undefined!,
    });
    const [selectedJob, setSelectedJob] = useState<JobState | undefined>(undefined);
    const [jobSteps] = AllJobSteps.use({
        microserviceId,
        tenantId: selectedTenant?.id || undefined!,
        jobId: selectedJob?.id!
    });

    useEffect(() => {
        if (tenants.data.length > 0) {
            setSelectedTenant(tenants.data[0]);
        }
    }, [tenants.data]);


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

            <Toolbar>
                <FormControl size="small" sx={{ m: 1, minWidth: 120 }}>
                    <InputLabel>Tenant</InputLabel>
                    <Select
                        label="Tenant"
                        autoWidth
                        value={selectedTenant?.id || ''}
                        onChange={e => setSelectedTenant(tenants.data.find(_ => _.id == e.target.value))}>

                        {tenants.data.map(tenant => {
                            return (
                                <MenuItem key={tenant.id} value={tenant.id}>{tenant.name}</MenuItem>
                            );
                        })}
                    </Select>
                </FormControl>

                {(selectedJob && (selectedJob.status == JobStatus.paused)) &&
                    <Button
                        startIcon={<icons.PlayArrow />}
                        onClick={async () => {
                            setResumeJobValues({
                                microserviceId,
                                tenantId: selectedTenant?.id || undefined!,
                                jobId: selectedJob.id
                            });

                            await resumeJob.execute();
                        }}
                    >Resume</Button>
                }

                {(selectedJob && (selectedJob.status == JobStatus.running ||
                        selectedJob.status == JobStatus.preparing ||
                        selectedJob.status == JobStatus.preparingSteps)) &&
                    <Button
                        startIcon={<icons.Stop />}
                        onClick={async () => {
                            setStopJobValues({
                                microserviceId,
                                tenantId: selectedTenant?.id || undefined!,
                                jobId: selectedJob.id
                            });

                            await stopJob.execute();
                        }}
                    >Stop</Button>
                }

                {selectedJob &&
                    <Button
                        startIcon={<icons.Delete />}
                        onClick={async () => {
                            setDeleteJobValues({
                                microserviceId,
                                tenantId: selectedTenant?.id || undefined!,
                                jobId: selectedJob.id
                            });

                            await deleteJob.execute();
                        }}
                    >Delete</Button>
                }

            </Toolbar>

            <Grid container spacing={2} sx={{ height: '100%' }}>
                <Grid item xs={8}>
                    <DataGrid
                        columns={jobColumns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: JobState) => row.id}
                        onRowSelectionModelChange={jobSelected}
                        rows={jobs.data}
                    />
                </Grid>

                <Grid item xs={4} >
                    <DataGrid
                        columns={jobStepColumns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: JobStepState) => row.id.jobStepId}
                        onRowSelectionModelChange={() => { }}
                        rows={jobSteps.data}
                    />
                </Grid>
            </Grid>
        </Stack>
    );
};
