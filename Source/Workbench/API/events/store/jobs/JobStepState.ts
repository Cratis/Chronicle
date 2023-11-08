/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { JobStepStatus } from './JobStepStatus';
import { JobStepStatusChanged } from './JobStepStatusChanged';
import { JobStepProgress } from './JobStepProgress';

export class JobStepState {

    @field(String)
    id!: string;

    @field(String)
    type!: string;

    @field(String)
    name!: string;

    @field(Number)
    status!: JobStepStatus;

    @field(JobStepStatusChanged, true)
    statusChanges!: JobStepStatusChanged[];

    @field(JobStepProgress)
    progress!: JobStepProgress;

    @field(Object)
    request!: any;
}
