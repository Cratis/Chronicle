/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { FailedPartition } from './FailedPartition';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/failed-partitions/{{observerId}}');

export interface AllFailedPartitionsArguments {
    microserviceId: string;
    tenantId: string;
    observerId?: string;
}
export class AllFailedPartitions extends ObservableQueryFor<FailedPartition[], AllFailedPartitionsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/failed-partitions/{{observerId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: FailedPartition[] = [];

    constructor() {
        super(FailedPartition, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
            'observerId',
        ];
    }

    static use(args?: AllFailedPartitionsArguments): [QueryResultWithState<FailedPartition[]>] {
        return useObservableQuery<FailedPartition[], AllFailedPartitions, AllFailedPartitionsArguments>(AllFailedPartitions, args);
    }
}
