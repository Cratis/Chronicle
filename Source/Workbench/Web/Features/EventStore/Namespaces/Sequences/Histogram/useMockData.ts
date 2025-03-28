// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '@cratis/applications/queries';
import React from 'react';

export const useMockData = (eventLogId: string) => {
    const [data, setData] = React.useState<QueryResult<object[]>>(QueryResult.empty([]));

    React.useEffect(() => {
        const mockData = [];
        const startDate = new Date();
        for (let i = 0; i < 30; i++) {
            const date = new Date(startDate);
            date.setDate(date.getDate() + i);
            mockData.push({
                date: date.toISOString().split('T')[0],
                count: Math.floor(Math.random() * 100)
            });
        }
        const result = {... QueryResult.noSuccess};
        result.isSuccess = true;
        result.data = mockData;
        const queryResult = new QueryResult<object[]>(result, Object, true);
        setData(queryResult);
    }, [eventLogId]);

    return data;
};
