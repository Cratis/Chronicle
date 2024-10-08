/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { JobStepProgress } from './JobStepProgress';
import { JobStepStatus } from './JobStepStatus';
import { JobStepStatusChanged } from './JobStepStatusChanged';

export class JobStepState {

    @field(Guid)
    id!: Guid;

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
}
