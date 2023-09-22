/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';


export class FailedPartitionAttempt {

    @field(Date)
    occurred!: Date;

    @field(Number)
    eventSequenceNumber!: number;

    @field(String, true)
    messages!: string[];

    @field(String)
    stackTrace!: string;
}
