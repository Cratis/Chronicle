/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { JobProgress } from './JobProgress';
import { JobStatus } from './JobStatus';
import { JobStatusChanged } from './JobStatusChanged';

export class Job {

    @field(String)
    id!: string;

    @field(String)
    details!: string;

    @field(String)
    type!: string;

    @field(Number)
    status!: JobStatus;

    @field(Date)
    created!: Date;

    @field(JobStatusChanged, true)
    statusChanges!: JobStatusChanged[];

    @field(JobProgress)
    progress!: JobProgress;
}
