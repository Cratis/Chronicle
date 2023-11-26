// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef } from 'react';
import { OverlayPanel } from 'primereact/overlaypanel';
import * as icons from "react-icons/fa";
import css from './Profile.module.css';
import { Button } from 'primereact/button';
import { useDarkMode } from 'usehooks-ts';


const ProfileItem = ({ icon, label, onClick }: { icon: any, label: string, onClick: () => void }) => {
    return (
        <li className={css.profileItem}>
            <span onClick={onClick}>
                {icon}
                <span>{label}</span>
            </span>
        </li>
    )
}

export const Profile = () => {
    const { isDarkMode, toggle: toggleDarkMode } = useDarkMode()
    const overlayPanelRef = useRef<OverlayPanel>(null);

    return (
        <div className='flex-1'>
            <div className={'flex justify-end gap-3 '}>

                <Button
                    icon={<icons.FaUser/>}
                    rounded
                    severity="info"
                    onClick={(e) => overlayPanelRef.current?.toggle(e)}
                    aria-label="User" />


                <OverlayPanel ref={overlayPanelRef} className={css.overlayPanel}>
                    <ul className={css.profileItems}>
                        <ProfileItem icon={<icons.FaUser />} label="Profile" onClick={() => { }} />
                        {isDarkMode ?
                            <ProfileItem icon={<icons.FaSun />} label="Light mode" onClick={toggleDarkMode} /> :
                            <ProfileItem icon={<icons.FaMoon />} label="Dark mode" onClick={toggleDarkMode} />}
                    </ul>
                </OverlayPanel>
            </div>
        </div>)
}
