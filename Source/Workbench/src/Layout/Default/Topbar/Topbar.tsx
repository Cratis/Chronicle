import { useLayoutContext } from '../context/LayoutContext';
import { ThemeSwitch } from './ThemeSwitch';
import { Button } from 'primereact/button';
import css from './Topbar.module.css';
import { FaBars } from 'react-icons/fa6';
import { forwardRef } from 'react';
import { Logo } from "./Logo";
import { Profile } from "./Profile";

export interface AppTopbarRef {
    menubutton?: HTMLButtonElement | null;
    topbarmenu?: HTMLDivElement | null;
    topbarmenubutton?: HTMLButtonElement | null;
}

export const Topbar = forwardRef<AppTopbarRef>(() => {
    const { toggleLeftSidebarOpen } = useLayoutContext();

    return (
        <div className={css.container}>
            <div className={`flex items-center justify-between ${css.leftSide}`}>
                <div className={css.sidebarToggle}>
                    <Button
                        onClick={toggleLeftSidebarOpen}
                        className='p-button-rounded p-button-text p-2'
                    >
                        <FaBars/>
                    </Button>
                </div>
                <div className={'flex-1 flex align-center justify-center'}>
                    <Logo/>
                </div>
            </div>
            <div className="flex-1 flex items-center  justify-between px-5">
                <div>
                    <Profile/>
                </div>
                <div>
                    <ThemeSwitch/>
                </div>
            </div>
        </div>
    );
});
