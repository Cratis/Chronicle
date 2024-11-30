// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef } from 'react';
import { OverlayPanel } from 'primereact/overlaypanel';
import * as icons from "react-icons/fa";
import css from './Profile.module.css';
import { Button } from 'primereact/button';
import { useDarkMode } from 'usehooks-ts';
import strings from 'Strings';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const ProfileItem = ({ icon, label, onClick }: { icon: any, label: string, onClick?: () => void }) => {
    return (
        <li className={css.profileItem} onClick={onClick}>
            <span className='mr-4'>{icon}</span>
            <span>{label}</span>
        </li>
    );
};

export const Profile = () => {
    const { isDarkMode, toggle: toggleDarkMode } = useDarkMode();
    const overlayPanelRef = useRef<OverlayPanel>(null);

    return (
        <div className='flex-1'>
            <div className={'flex justify-end gap-3 '}>

                <Button
                    icon={<icons.FaUser />}
                    rounded
                    severity="info"
                    className="p-2"
                    onClick={(e) => overlayPanelRef.current?.toggle(e)}
                    aria-label="User" />


                <OverlayPanel ref={overlayPanelRef} className={css.overlayPanel}>
                    <ul className={css.profileItems}>
                        <ProfileItem icon={<icons.FaUser />} label={strings.layout.topBar.profile.myAccount} />
                        {isDarkMode ?
                            <ProfileItem icon={<icons.FaSun />} label={strings.layout.topBar.profile.lightMode} onClick={toggleDarkMode} /> :
                            <ProfileItem icon={<icons.FaMoon />} label={strings.layout.topBar.profile.darkMode} onClick={toggleDarkMode} />}
                    </ul>
                </OverlayPanel>
            </div>
        </div>);
};
