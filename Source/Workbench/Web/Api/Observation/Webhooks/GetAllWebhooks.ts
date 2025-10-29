/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { WebhookDefinition } from './WebhookDefinition';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/observers/webhooks');

class GetAllWebhooksSortBy {
    private _identifier: SortingActionsForQuery<WebhookDefinition[]>;
    private _eventTypes: SortingActionsForQuery<WebhookDefinition[]>;
    private _target: SortingActionsForQuery<WebhookDefinition[]>;
    private _eventSequenceId: SortingActionsForQuery<WebhookDefinition[]>;
    private _isReplayable: SortingActionsForQuery<WebhookDefinition[]>;
    private _isActive: SortingActionsForQuery<WebhookDefinition[]>;

    constructor(readonly query: GetAllWebhooks) {
        this._identifier = new SortingActionsForQuery<WebhookDefinition[]>('identifier', query);
        this._eventTypes = new SortingActionsForQuery<WebhookDefinition[]>('eventTypes', query);
        this._target = new SortingActionsForQuery<WebhookDefinition[]>('target', query);
        this._eventSequenceId = new SortingActionsForQuery<WebhookDefinition[]>('eventSequenceId', query);
        this._isReplayable = new SortingActionsForQuery<WebhookDefinition[]>('isReplayable', query);
        this._isActive = new SortingActionsForQuery<WebhookDefinition[]>('isActive', query);
    }

    get identifier(): SortingActionsForQuery<WebhookDefinition[]> {
        return this._identifier;
    }
    get eventTypes(): SortingActionsForQuery<WebhookDefinition[]> {
        return this._eventTypes;
    }
    get target(): SortingActionsForQuery<WebhookDefinition[]> {
        return this._target;
    }
    get eventSequenceId(): SortingActionsForQuery<WebhookDefinition[]> {
        return this._eventSequenceId;
    }
    get isReplayable(): SortingActionsForQuery<WebhookDefinition[]> {
        return this._isReplayable;
    }
    get isActive(): SortingActionsForQuery<WebhookDefinition[]> {
        return this._isActive;
    }
}

class GetAllWebhooksSortByWithoutQuery {
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

export interface GetAllWebhooksParameters {
    eventStore: string;
}

export class GetAllWebhooks extends QueryFor<WebhookDefinition[], GetAllWebhooksParameters> {
    readonly route: string = '/api/event-store/{eventStore}/observers/webhooks';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: WebhookDefinition[] = [];
    private readonly _sortBy: GetAllWebhooksSortBy;
    private static readonly _sortBy: GetAllWebhooksSortByWithoutQuery = new GetAllWebhooksSortByWithoutQuery();

    constructor() {
        super(WebhookDefinition, true);
        this._sortBy = new GetAllWebhooksSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
        ];
    }

    get sortBy(): GetAllWebhooksSortBy {
        return this._sortBy;
    }

    static get sortBy(): GetAllWebhooksSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: GetAllWebhooksParameters, sorting?: Sorting): [QueryResultWithState<WebhookDefinition[]>, PerformQuery<GetAllWebhooksParameters>, SetSorting] {
        return useQuery<WebhookDefinition[], GetAllWebhooks, GetAllWebhooksParameters>(GetAllWebhooks, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GetAllWebhooksParameters, sorting?: Sorting): [QueryResultWithState<WebhookDefinition[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<WebhookDefinition[], GetAllWebhooks>(GetAllWebhooks, new Paging(0, pageSize), args, sorting);
    }
}
