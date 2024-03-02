// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DefaultLayout } from "../../Layout/Default/DefaultLayout";
import { Navigate, Route, Routes } from "react-router-dom";
import { Sequences } from "./Namespaces/Sequences/Sequences";
import { IMenuItemGroup } from "../../Layout/Default/Sidebar/MenuItem/MenuItem";
import * as mdIcons from 'react-icons/md';
import * as devIcons from 'react-icons/di';
import { Types } from "./General/Types/Types";
import { Observers } from "./Namespaces/Observers/Observers";
import { Projections } from "./General/Projections/Projections";
import { FailedPartitions } from "./Namespaces/FailedPartitions/FailedPartitions";
import { Recommendations } from "./Namespaces/Recommendations/Recommendations";
import { Jobs } from './Namespaces/Jobs/Jobs';
import { Identities } from './Namespaces/Identities/Identities';
import { Sequences as GeneralSequences } from './General/Sequences/Sequences';
import { Sinks } from './General/Sinks/Sinks';
import strings from 'Strings';

export const EventStore = () => {
    const menuItems: IMenuItemGroup[] = [
        {
            items: [
                { label: strings.mainMenu.recommendations, url: 'namespace/:namespace/recommendations', icon: mdIcons.MdInfo },
                { label: strings.mainMenu.jobs, url: 'namespace/:namespace/jobs', icon: mdIcons.MdGroupWork },
                { label: strings.mainMenu.sequences, url: 'namespace/:namespace/sequences', icon: mdIcons.MdDataArray },
                { label: strings.mainMenu.observers, url: 'namespace/:namespace/observers', icon: mdIcons.MdAirlineStops },
                {
                    label: strings.mainMenu.failedPartitions,
                    url: 'namespace/:namespace/failed-partitions',
                    icon: mdIcons.MdErrorOutline
                },
                { label: strings.mainMenu.identities, url: 'namespace/:namespace/identities', icon: mdIcons.MdPeople },
            ]
        },
        {
            label: strings.mainMenu.general.groupLabel,
            items: [
                { label: strings.mainMenu.general.types, url: 'types', icon: mdIcons.MdDataObject },
                { label: strings.mainMenu.general.projections, url: 'projections', icon: mdIcons.MdMediation },
                { label: strings.mainMenu.general.sequences, url: 'sequences', icon: mdIcons.MdDataArray },
                { label: strings.mainMenu.general.sinks, url: 'sinks', icon: devIcons.DiDatabase }
            ]
        }
    ];
    return (<>
        <Routes>
            <Route path=':eventStoreId'
                   element={<DefaultLayout leftMenuItems={menuItems} leftMenuBasePath={'/event-store/:eventStoreId'}/>}>

                <Route path={'namespace/:namespace'}>
                    <Route path={''} element={<Navigate to={'recommendations'}/>}/>
                    <Route path={'recommendations'} element={<Recommendations/>}/>
                    <Route path={'jobs'} element={<Jobs/>}/>
                    <Route path={'sequences/*'} element={<Sequences/>}/>
                    <Route path={'observers'} element={<Observers/>}/>
                    <Route path={'failed-partitions'} element={<FailedPartitions/>}/>
                    <Route path={'identities'} element={<Identities/>}/>
                </Route>
                <Route path={'types'} element={<Types/>} errorElement={<Projections/>}/>
                <Route path={'projections'} element={<Projections/>}/>
                <Route path={'sequences'} element={<GeneralSequences/>}/>
                <Route path={'sinks'} element={<Sinks/>}/>
            </Route>
        </Routes>
    </>)
}
