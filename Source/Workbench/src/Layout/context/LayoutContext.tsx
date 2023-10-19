import { createContext, useContext, useState, ReactNode } from 'react';
import { useDarkMode } from "usehooks-ts";

type Theme = 'light' | 'dark';

interface LayoutConfig {
    inputStyle: string;
    colorScheme: string;
    theme: Theme;
    scale: number;
}

interface LayoutContextType {
    layoutConfig: LayoutConfig;
    setLayoutConfig: (config: LayoutConfig) => void;
    theme: Theme;
    toggleTheme: () => void;
}

const LayoutContext = createContext<LayoutContextType | undefined>(undefined);

export function useLayout() {
    const context = useContext(LayoutContext);
    if (!context) {
        Error('useLayout must be used within a LayoutProvider');
    }
    return context;
}

interface LayoutProviderProps {
    children: ReactNode;
}

export function LayoutProvider({ children }: LayoutProviderProps) {
    const {isDarkMode} = useDarkMode();
    const [layoutConfig, setLayoutConfig] = useState<LayoutConfig>({
        inputStyle: 'outlined',
        colorScheme: isDarkMode ? 'dark' : 'light',
        theme: isDarkMode ? 'dark' : 'light',
        scale: 16,
    });

    const toggleTheme = () => {
        setLayoutConfig((prevConfig) => ({
            ...prevConfig,
            theme: prevConfig.theme === 'light' ? 'dark' : 'light',
        }));
    };

    const value: LayoutContextType = {
        layoutConfig,
        setLayoutConfig,
        theme: layoutConfig.theme,
        toggleTheme,
    };

    return <LayoutContext.Provider value={value}>{children}</LayoutContext.Provider>;
}
