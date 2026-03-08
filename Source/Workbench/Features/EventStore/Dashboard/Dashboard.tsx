// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import { EventTypeDistributionWidget } from './widgets/EventTypeDistributionWidget';
import { LagOverviewWidget } from './widgets/LagOverviewWidget';
import { NewEventTypesWidget } from './widgets/NewEventTypesWidget';
import { ObserverHealthWidget } from './widgets/ObserverHealthWidget';
import { RecentActivityWidget } from './widgets/RecentActivityWidget';
import { RecommendationsWidget } from './widgets/RecommendationsWidget';
import { StatusSummary } from './widgets/StatusSummary';
import { SystemHealthWidget } from './widgets/SystemHealthWidget';
import { TimelineWidget } from './widgets/TimelineWidget';

export const Dashboard = () => {
    return (
        <Page title="" noBackground>
            <div className="flex h-full flex-col gap-4 overflow-auto pb-4">
                {/* Top Status Bar */}
                <StatusSummary />

                {/* Row 2: Recent Activity + Recommendations */}
                <div className="grid gap-4 lg:grid-cols-3">
                    <RecentActivityWidget className="lg:col-span-2" />
                    <RecommendationsWidget />
                </div>

                {/* Row 3: Observer Health + Event Type Distribution */}
                <div className="grid gap-4 lg:grid-cols-3">
                    <ObserverHealthWidget className="lg:col-span-2" />
                    <EventTypeDistributionWidget />
                </div>

                {/* Row 4: Lag Overview + System Health */}
                <div className="grid gap-4 lg:grid-cols-3">
                    <LagOverviewWidget className="lg:col-span-2" />
                    <SystemHealthWidget />
                </div>

                {/* Row 5: New Event Types + Timeline */}
                <div className="grid gap-4 lg:grid-cols-3">
                    <NewEventTypesWidget />
                    <TimelineWidget className="lg:col-span-2" />
                </div>
            </div>
        </Page>
    );
};
