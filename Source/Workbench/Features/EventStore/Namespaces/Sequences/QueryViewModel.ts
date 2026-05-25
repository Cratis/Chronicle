// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { observable } from 'mobx';
import { QueryDefinition } from './QueryDefinition';

export class QueryViewModel {
    private _query!: QueryDefinition;
    private _hasChanges: boolean = false;
    private _eventSourceId: string = '';
    private _eventTypes: string[] = observable.array([]);
    private _startTime: Date | null = null;
    private _endTime: Date | null = null;

    get query(): QueryDefinition {
        return this._query;
    }

    set query(query: QueryDefinition) {
        this._query = query;
        this._eventSourceId = query.eventSourceId ?? '';
        this._eventTypes = observable.array(query.eventTypes ?? []);
        this._startTime = query.startTime ?? null;
        this._endTime = query.endTime ?? null;
        this._hasChanges = false;
    }

    get hasChanges(): boolean {
        return this._hasChanges;
    }

    get eventSourceId(): string {
        return this._eventSourceId;
    }

    set eventSourceId(value: string) {
        this._eventSourceId = value;
        this._hasChanges = true;
    }

    get eventTypes(): string[] {
        return this._eventTypes;
    }

    set eventTypes(value: string[]) {
        this._eventTypes = observable.array(value);
        this._hasChanges = true;
    }

    get startTime(): Date | null {
        return this._startTime;
    }

    set startTime(value: Date | null) {
        this._startTime = value;
        this._hasChanges = true;
    }

    get endTime(): Date | null {
        return this._endTime;
    }

    set endTime(value: Date | null) {
        this._endTime = value;
        this._hasChanges = true;
    }

    save() {
        this._hasChanges = false;
    }
}
