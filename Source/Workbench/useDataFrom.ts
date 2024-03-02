// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, useRef } from 'react';
import Handlebars from 'handlebars';

export type RefreshData = () => Promise<void>;
export type Map<T = any> = (input: any) => T;
export type DataReady<T = any> = (data: T[]) => void;

export interface RouteInfo {
    template: string;
    arguments: any;
}

const getRouteInfoFrom = (route: string | RouteInfo) => {
    let routeInfo: RouteInfo;

    if (typeof route == typeof '') {
        routeInfo = {
            template: route as string,
            arguments: {}
        };
    } else {
        routeInfo = route as RouteInfo;
    }

    return routeInfo;
};

const areArgumentsEqual = (left: RouteInfo, right: RouteInfo): boolean => {
    const leftKeys = Object.keys(left.arguments);
    const rightKeys = Object.keys(right.arguments);

    if (leftKeys.length != rightKeys.length) return false;

    for (const key of leftKeys) {
        if (left.arguments[key] !== right.arguments[key]) {
            return false;
        }
    }

    return true;
};

export function useDataFrom<T = any>(route: string | RouteInfo, mapFunction?: Map<T>, dataReadyCallback?: DataReady<T>): [T[], RefreshData] {
    const [data, setData] = useState<T[]>([]);
    const routeInfo = useRef<RouteInfo>();
    const template = useRef<Handlebars.TemplateDelegate>();

    const incomingRouteInfo = getRouteInfoFrom(route);

    const getData = async () => {
        if (!routeInfo || !routeInfo.current) return;
        const canFetch = Object.values(routeInfo.current.arguments).some(_ => _) || Object.keys(routeInfo.current.arguments).length == 0;
        if (!canFetch) {
            return;
        }
        const url = template!.current!(routeInfo.current.arguments);
        const response = await fetch(url);
        let result = await response.json();
        result = result.data;
        if (mapFunction) {
            if (Array.isArray(result)) {
                result = result.map(mapFunction);
            } else {
                result = mapFunction(result);
            }
        }

        dataReadyCallback?.(result as T[]);
        setData(result as T[]);
    };

    if (routeInfo.current && !areArgumentsEqual(incomingRouteInfo, routeInfo.current)) {
        routeInfo.current = incomingRouteInfo;
        getData();
    }

    useEffect(() => {
        routeInfo.current = getRouteInfoFrom(route);
        template.current = Handlebars.compile(routeInfo.current.template);
        getData();
    }, []);

    return [data, getData];
}
