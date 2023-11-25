// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useState, ReactNode, useContext, useEffect } from 'react';
import { useLocalStorage } from "usehooks-ts";

export interface ILayoutConfig {
    leftSidebarOpen: boolean;
    leftSidebarHidden: boolean;
}

interface ILayoutContext {
    layoutConfig: ILayoutConfig;
    setLayoutConfig: (config: ILayoutConfig) => void;
    toggleLeftSidebarOpen: () => void;
    openLeftSidebar: () => void;
    closeLeftSidebar: () => void;
    hideLeftSidebar: () => void;
    showLeftSidebar: () => void;
}

const defaultLayoutContext: ILayoutContext = {
    layoutConfig: {
        leftSidebarOpen: true,
        leftSidebarHidden: false,
    },
    setLayoutConfig: () => null,
    toggleLeftSidebarOpen: () => null,
    openLeftSidebar: () => null,
    closeLeftSidebar: () => null,
    hideLeftSidebar: () => null,
    showLeftSidebar: () => null,
};
export const LayoutContext = createContext<ILayoutContext>(defaultLayoutContext);

export const useLayoutContext = () => useContext(LayoutContext);
export const LayoutProvider = (props: { children: ReactNode }) => {
    const [storedLayoutConfig, setStoredLayoutConfig] = useLocalStorage('layoutConfig', JSON.stringify(defaultLayoutContext.layoutConfig));

    const [layoutConfig, setLayoutConfig] = useState<ILayoutConfig>(
        storedLayoutConfig
            ? JSON.parse(storedLayoutConfig)
            : defaultLayoutContext.layoutConfig
    );
    useEffect(() => {
        setStoredLayoutConfig(JSON.stringify(layoutConfig));
    }, [layoutConfig]);


    const toggleLeftSidebarOpen = () => {
        setLayoutConfig({
            ...layoutConfig,
            leftSidebarOpen: !layoutConfig.leftSidebarOpen,
        });
    };
    const openLeftSidebar = () => {
        setLayoutConfig({
            ...layoutConfig,
            leftSidebarOpen: true,
        });
    };
    const closeLeftSidebar = () => {
        setLayoutConfig({
            ...layoutConfig,
            leftSidebarOpen: false,
        });
    };
    const hideLeftSidebar = () => {
        setLayoutConfig({
            ...layoutConfig,
            leftSidebarHidden: true,
        });
    };
    const showLeftSidebar = () => {
        setLayoutConfig({
            ...layoutConfig,
            leftSidebarHidden: false,
        });
    };

    return (
        <LayoutContext.Provider
            value={{
                layoutConfig,
                setLayoutConfig,
                toggleLeftSidebarOpen,
                openLeftSidebar,
                closeLeftSidebar,
                hideLeftSidebar,
                showLeftSidebar,
            }}
        >
            {props.children}
        </LayoutContext.Provider>
    );
};
