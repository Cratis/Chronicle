/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';


export class FailedPartition {

    @field(String)
    eventSourceId!: string;

    @field(Number)
    tail!: number;

    @field(String, true)
    exceptionMessages!: String[];

    @field(String)
    exceptionStackTrace!: string;

    @field(Date)
    occurred?: Date;

    @field(String)
    partition!: string;

    @field(Number)
    head?: number;

    @field(Number)
    recoveredTo?: number;

    @field(Boolean)
    isRecovered!: boolean;
}
