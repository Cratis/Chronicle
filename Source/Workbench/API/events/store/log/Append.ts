/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';

export class Append extends Command {
    readonly route: string = '/api/events/store/log/{eventSourceId}/{eventTypeId}/{eventGeneration}';

    isSpecified!: any;
}
