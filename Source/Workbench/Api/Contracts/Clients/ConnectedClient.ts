/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';

export class ConnectedClient {

    @field(String)
    connectionId!: string;

    @field(String)
    version!: string;

    @field(Date)
    lastSeen!: Date;

    @field(Boolean)
    isRunningWithDebugger!: boolean;
}
