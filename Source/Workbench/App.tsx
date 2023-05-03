// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Route, Routes } from 'react-router-dom';
import { Header } from './Header';
import { Configuration } from './configuration/Configuration';
import { Clients } from './clients/Clients';
import { Compliance } from './compliance/Compliance';
import { EventStore } from './eventStore/EventStore';
import { Metrics } from './metrics/Metrics';


export const App = () => {
    return (
        <>
            <Header />
            <Routes>
                <Route path="/">
                    Home
                </Route>
                <Route path="/event-store/*" element={<EventStore />} />
                <Route path="/metrics/*" element={<Metrics />} />
                <Route path="/clients/*" element={<Clients />} />
                <Route path="/compliance/*" element={<Compliance />} />
                <Route path="/configuration/*" element={<Configuration />} />
            </Routes>
        </>
    );
};

