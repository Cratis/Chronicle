#

This PR and branch contains a restructuring of integration specs compared to main. Everything sits in the Integration/Client All the specs work on main, but in this context we are setting them up to run across different environments:

- mongodb - inprocess
- mongodb - out-of-process
- postgresql - out-of-process
- mssql - out-of-process
- sqlite - out-of-process

We've made some progress, but things are still failing. There shouldn't be any If(this context) then do.. type of scenarios in the specs at all. They should all go through the Client APIs and be able to spec things through that.
We also do not want extra additional waiting mechanisms, we know when things are ready and not - or at least should know, but might miss exposure to the client for doing so.

Iterate locally, run specs and iterate.

Dig deep on root causes - always prioritize root causes and not putting in workarounds.

Keep in mind that we have multiple databases for segregation, and even namespace databases as well. So keep that in mind, we do not want to merge databases. Look at MongoDB Storage implementation for more details.

Focus on the job, don't change things that is unrelated to the job - don't fix things that is outside of the scope of this job.

Lets get mongodb - out-of-process working first, across the board.
Go through systematically and make everything pass. You can do this namespace by namespace, as we have in the build workflow.

Start with existing errors:
https://github.com/Cratis/Chronicle/actions/runs/26030056327?pr=2269


---

Its unclear to me if we're making progress. We have some serious problems - there are still failures and the fact that things have been running for 1.5 hours and still not done (Projections outofprocess, mongodb) is an indication that something is fundamentally wrong.

For the inprocess version for mongo, we're spending 5-6 minutes. There is absolutely no reason the outofprocess should be that slow.

What is important is that we don't restart the container for each spec. We keep the container, but we reset its state - we are supposed to have an API that should be exposed through GRPC for resetting the state of the Kernel when compiled with DEVELOPMENT and throw an exception when not in DEVELOPMENT. The .NET Client should have this API as well as a Server thing and we should be calling it to clear state when needed.

Look at the current run to learn more, but keep iterating locally, goal is still to get everything to be green:
https://github.com/Cratis/Chronicle/actions/runs/26079185193/job/76677130267?pr=2269
