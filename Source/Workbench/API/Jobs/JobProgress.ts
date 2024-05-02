/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';


export class JobProgress {

    @field(Number)
    totalSteps!: number;

    @field(Number)
    successfulSteps!: number;

    @field(Number)
    failedSteps!: number;

    @field(Boolean)
    isCompleted!: boolean;

    @field(String)
    message!: string;
}
