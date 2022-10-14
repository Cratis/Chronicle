/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';


export class FailedObserverPartition {

    @field(String)
    eventSourceId!: string;

    @field(Number)
    sequenceNumber!: number;

    @field(Date)
    occurred!: Date;

    @field(Date)
    lastAttempt!: Date;

    @field(Number)
    attempts!: number;

    @field(String, true)
    messages!: string[];

    @field(String)
    stackTrace!: string;
}
