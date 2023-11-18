/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { OperationInformation } from './OperationInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/operations');

export interface GetOperationsArguments {
    microserviceId: string;
    tenantId: string;
}
export class GetOperations extends QueryFor<OperationInformation[], GetOperationsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/operations';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: OperationInformation[] = [];

    constructor() {
        super(OperationInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: GetOperationsArguments): [QueryResultWithState<OperationInformation[]>, PerformQuery<GetOperationsArguments>] {
        return useQuery<OperationInformation[], GetOperations, GetOperationsArguments>(GetOperations, args);
    }
}
