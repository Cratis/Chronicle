/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { SuggestionInformation } from './SuggestionInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/suggestions/observe');

export interface AllSuggestionsArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllSuggestions extends ObservableQueryFor<SuggestionInformation[], AllSuggestionsArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/suggestions/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: SuggestionInformation[] = [];

    constructor() {
        super(SuggestionInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: AllSuggestionsArguments): [QueryResultWithState<SuggestionInformation[]>] {
        return useObservableQuery<SuggestionInformation[], AllSuggestions, AllSuggestionsArguments>(AllSuggestions, args);
    }
}
