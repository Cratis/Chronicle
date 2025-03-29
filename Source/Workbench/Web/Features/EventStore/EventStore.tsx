// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DefaultLayout } from "../../Layout/Default/DefaultLayout";
import { Navigate, Route, Routes } from "react-router-dom";
import { SequencesFuture } from "./Namespaces/Sequences/SequencesFuture";
import { IMenuItemGroup } from "../../Layout/Default/Sidebar/MenuItem/MenuItem";
import * as mdIcons from 'react-icons/md';
// import * as devIcons from 'react-icons/di';
// import * as gameIcons from 'react-icons/gi';
import { Types } from "./General/Types/Types";
import { Observers } from "./Namespaces/Observers/Observers";
import { Projections } from "./General/Projections/Projections";
import { FailedPartitions } from "./Namespaces/FailedPartitions/FailedPartitions";
import { Recommendations } from "./Namespaces/Recommendations/Recommendations";
import { Jobs } from './Namespaces/Jobs/Jobs';
import { Identities } from './Namespaces/Identities/Identities';
import { Sequences as GeneralSequences } from './General/Sequences/Sequences';
import { Sinks } from './General/Sinks/Sinks';
import { Reducers } from './General/Reducers/Reducers';
import { Reactors } from './General/Reactors/Reactors';
import strings from 'Strings';
import { Namespaces } from './General/Namespaces/Namespaces';
import { Sequences } from './Namespaces/Sequences/Sequences';
import { useRelativePath } from '../../Utils/useRelativePath';

export const EventStore = () => {
    const menuItems: IMenuItemGroup[] = [
        {
            items: [
                { label: strings.mainMenu.recommendations, url: ':namespace/recommendations', icon: mdIcons.MdInfo },
                { label: strings.mainMenu.jobs, url: ':namespace/jobs', icon: mdIcons.MdGroupWork },
                { label: strings.mainMenu.sequences, url: ':namespace/sequences', icon: mdIcons.MdDataArray },
                { label: strings.mainMenu.observers, url: ':namespace/observers', icon: mdIcons.MdAirlineStops },
                { label: strings.mainMenu.failedPartitions, url: ':namespace/failed-partitions', icon: mdIcons.MdErrorOutline },
                { label: strings.mainMenu.identities, url: ':namespace/identities', icon: mdIcons.MdPeople },
            ]
        },
        {
            label: strings.mainMenu.general.groupLabel,
            items: [
                { label: strings.mainMenu.general.types, url: 'types', icon: mdIcons.MdDataObject },
                { label: strings.mainMenu.general.namespaces, url: 'namespaces', icon: mdIcons.MdApps },
                // { label: strings.mainMenu.general.sequences, url: 'sequences', icon: mdIcons.MdDataArray },
                // { label: strings.mainMenu.general.projections, url: 'projections', icon: mdIcons.MdMediation },
                // { label: strings.mainMenu.general.reducers, url: 'reducers', icon: gameIcons.GiTransform },
                // { label: strings.mainMenu.general.reactors, url: 'reactors', icon: gameIcons.GiReactor },
                // { label: strings.mainMenu.general.sinks, url: 'sinks', icon: devIcons.DiDatabase }
            ]
        }
    ];

    const basePath = useRelativePath('event-store');

    return (<>
        <Routes>
            <Route path=':eventStore'
                element={<DefaultLayout menu={menuItems} basePath={`${basePath}/:eventStore`} />}>

                <Route path={'types'} element={<Types />} />
                <Route path={'namespaces'} element={<Namespaces />} />
                <Route path={'sequences'} element={<GeneralSequences />} />
                <Route path={'projections'} element={<Projections />} />
                <Route path={'reducers'} element={<Reducers />} />
                <Route path={'reactors'} element={<Reactors />} />
                <Route path={'sinks'} element={<Sinks />} />

                <Route path={':namespace'}>
                    <Route path={''} element={<Navigate to={'recommendations'} />} />
                    <Route path={'recommendations'} element={<Recommendations />} />
                    <Route path={'jobs'} element={<Jobs />} />
                    <Route path={'sequences'} element={<Sequences />} />
                    <Route path={'sequences-future'} element={<SequencesFuture />} />
                    <Route path={'observers'} element={<Observers />} />
                    <Route path={'failed-partitions'} element={<FailedPartitions />} />
                    <Route path={'identities'} element={<Identities />} />
                </Route>
            </Route>
        </Routes>
    </>);
};
