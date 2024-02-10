// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, createContext, Dispatch, SetStateAction, ReactNode, useEffect } from 'react';

export interface MenuContextProps {
    paramsFallback: {};
    setParamsFallback: Dispatch<SetStateAction<string>>;
}

export interface MenuProviderProps {
    children: ReactNode;
    params?: {};
}

export const MenuContext = createContext({} as MenuContextProps);

export const MenuProvider = ({ children, params }: MenuProviderProps) => {
    const [paramsFallback, setParamsFallback] = useState(params ?? {});

    useEffect(() => {
        setParamsFallback(params ?? {});
    }, [params]);

    const value = {
        paramsFallback,
        setParamsFallback
    };

    return <MenuContext.Provider value={value}>{children}</MenuContext.Provider>;
};
