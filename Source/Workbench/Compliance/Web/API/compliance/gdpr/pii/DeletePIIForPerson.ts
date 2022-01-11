/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/frontend/commands';

export class DeletePIIForPerson extends Command {
    readonly route: string = '/api/compliance/gdpr/pii/delete';

    personId!: string;
}
