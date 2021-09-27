// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserRouter as Router, Route } from 'react-router-dom';
import { Navigation } from './Navigation';
import { EventTypes } from './EventTypes';

import { default as styles } from './App.module.scss';
import { EventLog } from './EventLog';

export const App = () => {
    return (
        <Router>
            <div className={styles.appContainer}>
                <div className={styles.navigationBar}>
                    <Navigation />
                </div>
                <div style={{ width: '100%' }}>
                    <Route exact path="/">
                        Home
                    </Route>
                    <Route path="/events/types">
                        <EventTypes />
                    </Route>

                    <Route path="/events/eventlog">
                        <EventLog />
                    </Route>
                </div>
            </div>
        </Router>
    );
};

