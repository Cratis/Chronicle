import { TopSection } from './TopSection';

interface ISidebarProps {
    children: React.ReactNode;
}

export const Sidebar = ({ children}: ISidebarProps) => {
    return (
        <div>
            <TopSection/>
            {children}
        </div>
    );
};
