import { Persona } from './Persona';
import classes from './Sidebar.module.css';
import { TopSection } from './TopSection';

export const SidebarComponent = () => {
    return (
        <div className={classes.sidebar}>
            <div className={classes.container}>
                <TopSection />
            </div>
            <div>
                <Persona />
            </div>
        </div>
    );
};
