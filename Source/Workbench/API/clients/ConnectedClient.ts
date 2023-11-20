/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';


export class ConnectedClient {

    @field(String)
    connectionId!: string;

    @field(String)
    clientUri!: string;

    @field(String)
    version!: string;

    @field(Date)
    lastSeen!: Date;

    @field(Boolean)
    isRunningWithDebugger!: boolean;

    @field(Boolean)
    isMultiTenanted!: boolean;
}
