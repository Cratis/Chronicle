/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { JobStepStatus } from './JobStepStatus';

export class JobStepStatusChanged {

    @field(JobStepStatus)
    status!: JobStepStatus;

    @field(Date)
    occurred!: Date;

    @field(String, true)
    exceptionMessages!: string[];

    @field(String)
    exceptionStackTrace!: string;
}
