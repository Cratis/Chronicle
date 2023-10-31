import { createContext, useState, ReactNode, useContext } from 'react';

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
    const [layoutConfig, setLayoutConfig] = useState<ILayoutConfig>(
        defaultLayoutContext.layoutConfig
    );

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
