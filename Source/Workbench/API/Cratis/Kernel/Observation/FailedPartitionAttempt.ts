/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';


export class FailedPartitionAttempt {

    @field(Date)
    occurred!: Date;

    @field(Number)
    sequenceNumber!: number;

    @field(String, true)
    messages!: string[];

    @field(String)
    stackTrace!: string;
}
