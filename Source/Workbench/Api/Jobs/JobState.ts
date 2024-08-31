/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { JobStatus } from './JobStatus';
import { JobStatusChanged } from './JobStatusChanged';
import { JobProgress } from './JobProgress';

export class JobState {

    @field(Guid)
    id!: Guid;

    @field(String)
    type!: string;

    @field(String)
    name!: string;

    @field(String)
    details!: string;

    @field(Number)
    status!: JobStatus;

    @field(JobStatusChanged, true)
    statusChanges!: JobStatusChanged[];

    @field(JobProgress)
    progress!: JobProgress;
}
