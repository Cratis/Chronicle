/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';


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
