/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { GrainId } from './GrainId';
import { JobStepStatus } from './JobStepStatus';
import { JobStepStatusChanged } from './JobStepStatusChanged';
import { JobStepProgress } from './JobStepProgress';

export class JobStepState {

    @field(GrainId)
    grainId!: GrainId;

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
