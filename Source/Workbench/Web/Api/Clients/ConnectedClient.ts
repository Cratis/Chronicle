/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class ConnectedClient {

    @field(String)
    connectionId!: string;

    @field(String)
    version!: string;

    @field(SerializableDateTimeOffset)
    lastSeen!: SerializableDateTimeOffset;

    @field(Boolean)
    isRunningWithDebugger!: boolean;
}
