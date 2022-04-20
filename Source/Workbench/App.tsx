// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Route, Routes } from 'react-router-dom';
import { Navigation } from './Navigation';

import { default as styles } from './App.module.scss';
import { Microservices } from './configuration/Microservices';
import { People } from './GDPR/People';
import { EventTypes } from './events/store/EventTypes';
import { Projections } from './events/store/Projections';
import { EventSequences } from './events/store/EventSequences';
import { Observers } from './events/store/Observers';
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
                    <Route path="/events/store/types" element={<EventTypes />}/>
                    <Route path="/events/store/sequence" element={<EventSequences />}/>
                    <Route path="/events/store/observers" element={<Observers />}/>
                    <Route path="/events/store/projections" element={<Projections />}/>
                </Routes>
            </div>
        </div>
    );
};

