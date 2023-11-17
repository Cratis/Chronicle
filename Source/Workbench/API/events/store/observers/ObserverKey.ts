/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';


export class ObserverKey {

    @field(String)
    microserviceId!: string;

    @field(String)
    tenantId!: string;

    @field(String)
    eventSequenceId!: string;

    @field(String)
    sourceMicroserviceId?: string;

    @field(String)
    sourceTenantId?: string;
}
