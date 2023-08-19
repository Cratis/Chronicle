/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { RecoverFailedPartitionState } from './RecoverFailedPartitionState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/failed-partitions');

export interface AllFailedPartitionsArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllFailedPartitions extends ObservableQueryFor<RecoverFailedPartitionState[], AllFailedPartitionsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/failed-partitions';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: RecoverFailedPartitionState[] = [];

    constructor() {
        super(RecoverFailedPartitionState, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: AllFailedPartitionsArguments): [QueryResultWithState<RecoverFailedPartitionState[]>] {
        return useObservableQuery<RecoverFailedPartitionState[], AllFailedPartitions, AllFailedPartitionsArguments>(AllFailedPartitions, args);
    }
}
