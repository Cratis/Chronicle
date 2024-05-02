// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { ConnectedClient } from '../Cratis/Kernel/Contracts/Clients/ConnectedClient';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/clients');

export class AllConnectedClients extends QueryFor<ConnectedClient[]> {
    readonly route: string = '/api/clients';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ConnectedClient[] = [];

    constructor() {
        super(ConnectedClient, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<ConnectedClient[]>, PerformQuery] {
        return useQuery<ConnectedClient[], AllConnectedClients>(AllConnectedClients);
    }
}
