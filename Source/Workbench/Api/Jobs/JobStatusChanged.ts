/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
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
