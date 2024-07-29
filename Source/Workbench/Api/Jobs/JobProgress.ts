/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';

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
