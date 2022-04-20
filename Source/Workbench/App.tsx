// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Route, Routes } from 'react-router-dom';
import { Navigation } from './Navigation';

import { default as styles } from './App.module.scss';
import { Microservices } from './configuration/Microservices';
import { People } from './GDPR/People';
import { EventTypes } from './events/EventTypes';
import { Projections } from './events/Projections';
import { EventLogs } from './events/EventLogs';
import { Observers } from './events/Observers';
import { Tenants } from './configuration/Tenants';


export const App = () => {
    return (
        <div className={styles.appContainer}>
            <div className={styles.navigationBar}>
                <Navigation />
            </div>
            <div style={{ width: '100%' }}>
                <Routes>
                    <Route path="/">
                        Home
                    </Route>
                    <Route path="/configuration/microservices" element={<Microservices/>} />
                    <Route path="/configuration/tenants" element={<Tenants/>} />
                    <Route path="/gdpr/people" element={<People />}/>
                    <Route path="/events/types" element={<EventTypes />}/>
                    <Route path="/events/eventlogs" element={<EventLogs />}/>
                    <Route path="/events/observers" element={<Observers />}/>
                    <Route path="/events/projections" element={<Projections />}/>
                </Routes>
            </div>
        </div>
    );
};

