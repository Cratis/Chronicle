// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { NullObservableQueryConnection } from './NullObservableQueryConnection';
import { ObservableQuerySubscription, OnNextResult, QueryResult } from '@cratis/arc/queries';
import { container } from 'tsyringe';
import { AllObservers } from 'Api/Observation/AllObservers';
import { ObserverInformation } from 'Api/Observation';
import { AllNamespaces } from 'Api/Namespaces';
import observers from './Observers.json';
import namespaces from './Namespaces.json';

/* eslint-disable @typescript-eslint/no-explicit-any */

function registerFakeQuery<TDataType>(queryType: Constructor, itemConstructor: Constructor, data: any) {
    const query = new queryType() as any;

    const response = {
        data: data,
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        paging: { page: 0, size: data.length, totalItems: data.length, totalPages: 1, pageSize: data.length },
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: ''
    };

    query.subscribe = (callback: OnNextResult<QueryResult<TDataType>>) => {
        const result = new QueryResult<TDataType>(response, itemConstructor, true);
        callback(result);
        return new ObservableQuerySubscription(new NullObservableQueryConnection());
    };
    container.registerInstance(queryType, query);
}


export class FakeData {
    static initialize() {
        registerFakeQuery(AllObservers, ObserverInformation, observers);
        registerFakeQuery(AllNamespaces, String, namespaces);
    }
}

