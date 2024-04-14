/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { FailedPartition } from '../../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Observation/FailedPartition';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/failed-partitions/{observerId}/{observerId}');

export interface AllFailedPartitionsArguments {
    eventStore: string;
    namespace: string;
    observerId?: string;
}
export class AllFailedPartitions extends QueryFor<FailedPartition[], AllFailedPartitionsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/failed-partitions/{observerId}/{observerId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: FailedPartition[] = [];

    constructor() {
        super(FailedPartition, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'observerId',
        ];
    }

    static use(args?: AllFailedPartitionsArguments): [QueryResultWithState<FailedPartition[]>, PerformQuery<AllFailedPartitionsArguments>] {
        return useQuery<FailedPartition[], AllFailedPartitions, AllFailedPartitionsArguments>(AllFailedPartitions, args);
    }
}
