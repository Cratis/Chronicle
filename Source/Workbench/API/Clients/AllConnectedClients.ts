/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { ConnectedClient } from '../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Clients/ConnectedClient';
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
