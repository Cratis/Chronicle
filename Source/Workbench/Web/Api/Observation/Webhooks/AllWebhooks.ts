/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { WebhookDefinition } from './WebhookDefinition';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/observers/webhooks/observe');

class AllWebhooksSortBy {
    private _identifier: SortingActionsForObservableQuery<WebhookDefinition[]>;
    private _eventTypes: SortingActionsForObservableQuery<WebhookDefinition[]>;
    private _target: SortingActionsForObservableQuery<WebhookDefinition[]>;
    private _eventSequenceId: SortingActionsForObservableQuery<WebhookDefinition[]>;
    private _isReplayable: SortingActionsForObservableQuery<WebhookDefinition[]>;
    private _isActive: SortingActionsForObservableQuery<WebhookDefinition[]>;

    constructor(readonly query: AllWebhooks) {
        this._identifier = new SortingActionsForObservableQuery<WebhookDefinition[]>('identifier', query);
        this._eventTypes = new SortingActionsForObservableQuery<WebhookDefinition[]>('eventTypes', query);
        this._target = new SortingActionsForObservableQuery<WebhookDefinition[]>('target', query);
        this._eventSequenceId = new SortingActionsForObservableQuery<WebhookDefinition[]>('eventSequenceId', query);
        this._isReplayable = new SortingActionsForObservableQuery<WebhookDefinition[]>('isReplayable', query);
        this._isActive = new SortingActionsForObservableQuery<WebhookDefinition[]>('isActive', query);
    }

    get identifier(): SortingActionsForObservableQuery<WebhookDefinition[]> {
        return this._identifier;
    }
    get eventTypes(): SortingActionsForObservableQuery<WebhookDefinition[]> {
        return this._eventTypes;
    }
    get target(): SortingActionsForObservableQuery<WebhookDefinition[]> {
        return this._target;
    }
    get eventSequenceId(): SortingActionsForObservableQuery<WebhookDefinition[]> {
        return this._eventSequenceId;
    }
    get isReplayable(): SortingActionsForObservableQuery<WebhookDefinition[]> {
        return this._isReplayable;
    }
    get isActive(): SortingActionsForObservableQuery<WebhookDefinition[]> {
        return this._isActive;
    }
}

class AllWebhooksSortByWithoutQuery {
    private _identifier: SortingActions  = new SortingActions('identifier');
    private _eventTypes: SortingActions  = new SortingActions('eventTypes');
    private _target: SortingActions  = new SortingActions('target');
    private _eventSequenceId: SortingActions  = new SortingActions('eventSequenceId');
    private _isReplayable: SortingActions  = new SortingActions('isReplayable');
    private _isActive: SortingActions  = new SortingActions('isActive');

    get identifier(): SortingActions {
        return this._identifier;
    }
    get eventTypes(): SortingActions {
        return this._eventTypes;
    }
    get target(): SortingActions {
        return this._target;
    }
    get eventSequenceId(): SortingActions {
        return this._eventSequenceId;
    }
    get isReplayable(): SortingActions {
        return this._isReplayable;
    }
    get isActive(): SortingActions {
        return this._isActive;
    }
}

export interface AllWebhooksParameters {
    eventStore: string;
}
export class AllWebhooks extends ObservableQueryFor<WebhookDefinition[], AllWebhooksParameters> {
    readonly route: string = '/api/event-store/{eventStore}/observers/webhooks/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: WebhookDefinition[] = [];
    private readonly _sortBy: AllWebhooksSortBy;
    private static readonly _sortBy: AllWebhooksSortByWithoutQuery = new AllWebhooksSortByWithoutQuery();

    constructor() {
        super(WebhookDefinition, true);
        this._sortBy = new AllWebhooksSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
        ];
    }

    get sortBy(): AllWebhooksSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllWebhooksSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllWebhooksParameters, sorting?: Sorting): [QueryResultWithState<WebhookDefinition[]>, SetSorting] {
        return useObservableQuery<WebhookDefinition[], AllWebhooks, AllWebhooksParameters>(AllWebhooks, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllWebhooksParameters, sorting?: Sorting): [QueryResultWithState<WebhookDefinition[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<WebhookDefinition[], AllWebhooks>(AllWebhooks, new Paging(0, pageSize), args, sorting);
    }
}
