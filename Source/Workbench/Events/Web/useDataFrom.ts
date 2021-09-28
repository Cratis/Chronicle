// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';

export type RefreshData = () => Promise<void>;
export type Map<T = any> = (input: any) => T;

export function useDataFrom<T>(route: string, mapFunction?: Map<T>): [T[], RefreshData] {
    const [data, setData] = useState<T[]>([]);
    const getData = async () => {
        const response = await fetch(route);
        let result = await response.json();
        if (mapFunction) {
            if (Array.isArray(result)) {
                result = result.map(mapFunction);
            } else {
                result = mapFunction(result);
            }
        }

        setData(result as T[]);
    };

    useEffect(() => {
        getData();
    }, [])

    return [data, getData];
};
