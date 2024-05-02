// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { Namespace } from './Namespace';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/namespaces');

export class AllNamespaces extends QueryFor<Namespace[]> {
    readonly route: string = '/api/namespaces';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Namespace[] = [];

    constructor() {
        super(Namespace, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<Namespace[]>, PerformQuery] {
        return useQuery<Namespace[], AllNamespaces>(AllNamespaces);
    }
}
