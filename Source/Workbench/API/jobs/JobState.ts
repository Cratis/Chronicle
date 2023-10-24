/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { JobStatus } from './JobStatus';
import { JobStatusChanged } from './JobStatusChanged';
import { JobProgress } from './JobProgress';

export class JobState {

    @field(String)
    id!: string;

    @field(String)
    name!: string;

    @field(String)
    type!: string;

    @field(Number)
    status!: JobStatus;

    @field(JobStatusChanged, true)
    statusChanges!: JobStatusChanged[];

    @field(JobProgress)
    progress!: JobProgress;

    @field(Boolean)
    remove!: boolean;

    @field(Object)
    request!: any;
}
