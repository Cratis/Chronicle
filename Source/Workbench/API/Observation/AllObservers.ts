// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from 'Infrastructure/queries';
import { ObserverInformation } from '../Cratis/Kernel/Contracts/Observation/ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/observers/observe/observe');

export interface AllObserversArguments {
    eventStore: string;
    namespace: string;
}
export class AllObservers extends ObservableQueryFor<ObserverInformation[], AllObserversArguments> {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/observers/observe/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ObserverInformation[] = [];

    constructor() {
        super(ObserverInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    static use(args?: AllObserversArguments): [QueryResultWithState<ObserverInformation[]>] {
        return useObservableQuery<ObserverInformation[], AllObservers, AllObserversArguments>(AllObservers, args);
    }
}
