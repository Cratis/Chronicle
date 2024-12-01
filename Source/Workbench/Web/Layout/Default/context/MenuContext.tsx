// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, createContext, Dispatch, SetStateAction, ReactNode, useEffect } from 'react';

export interface MenuContextProps {
    paramsFallback: object;
    setParamsFallback: Dispatch<SetStateAction<object>>;
}

export interface MenuProviderProps {
    children: ReactNode;
    params?: object;
}

export const MenuContext = createContext({} as MenuContextProps);

export const MenuProvider = ({ children, params }: MenuProviderProps) => {
    const [paramsFallback, setParamsFallback] = useState(params ?? {});

    useEffect(() => {
        setParamsFallback(params ?? {});
    }, [params]);

    const value: MenuContextProps = {
        paramsFallback,
        setParamsFallback
    };

    return <MenuContext.Provider value={value}>{children}</MenuContext.Provider>;
};
