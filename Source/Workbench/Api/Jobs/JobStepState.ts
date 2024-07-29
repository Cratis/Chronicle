/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { JobStepStatus } from './JobStepStatus';
import { JobStepStatusChanged } from './JobStepStatusChanged';
import { JobStepProgress } from './JobStepProgress';

export class JobStepState {

    @field(Guid)
    id!: Guid;

    @field(String)
    type!: string;

    @field(String)
    name!: string;

    @field(JobStepStatus)
    status!: JobStepStatus;

    @field(JobStepStatusChanged, true)
    statusChanges!: JobStepStatusChanged[];

    @field(JobStepProgress)
    progress!: JobStepProgress;
}
