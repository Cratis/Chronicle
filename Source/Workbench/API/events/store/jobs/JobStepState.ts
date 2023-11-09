/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { JobStepIdentifier } from './JobStepIdentifier';
import { JobStepStatus } from './JobStepStatus';
import { JobStepStatusChanged } from './JobStepStatusChanged';
import { JobStepProgress } from './JobStepProgress';

export class JobStepState {

    @field(JobStepIdentifier)
    id!: JobStepIdentifier;

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
