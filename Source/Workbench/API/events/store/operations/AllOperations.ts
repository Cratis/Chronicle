/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { OperationInformation } from './OperationInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/operations/observe');

export interface AllOperationsArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllOperations extends ObservableQueryFor<OperationInformation[], AllOperationsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/operations/observe';
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

    static use(args?: AllOperationsArguments): [QueryResultWithState<OperationInformation[]>] {
        return useObservableQuery<OperationInformation[], AllOperations, AllOperationsArguments>(AllOperations, args);
    }
}
