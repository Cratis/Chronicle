import { createContext, useContext, useState, ReactNode } from 'react';

type Theme = 'light' | 'dark';

interface LayoutConfig {
    ripple: boolean;
    inputStyle: string;
    colorScheme: string;
    theme: Theme;
    scale: number;
}

// Define a type for the context
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
    const [layoutConfig, setLayoutConfig] = useState<LayoutConfig>({
        ripple: false,
        inputStyle: 'outlined',
        colorScheme: 'light',
        theme: 'light',
        scale: 16,
    });

    const color = layoutConfig.theme === 'light' ? '#333' : '#FFF';
    const backgroundColor = layoutConfig.theme === 'light' ? '#FFF' : '#333';

    document.body.style.color = color;
    document.body.style.backgroundColor = backgroundColor;

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
