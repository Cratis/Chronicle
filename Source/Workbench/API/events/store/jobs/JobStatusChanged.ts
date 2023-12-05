/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { JobStatus } from './JobStatus';

export class JobStatusChanged {

    @field(Number)
    status!: JobStatus;

    @field(Date)
    occurred!: Date;

    @field(String, true)
    exceptionMessages!: string[];

    @field(String)
    exceptionStackTrace!: string;
}
