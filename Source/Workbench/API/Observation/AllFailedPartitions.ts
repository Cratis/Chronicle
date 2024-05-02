// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { FailedPartition } from '../Cratis/Kernel/Observation/FailedPartition';
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
