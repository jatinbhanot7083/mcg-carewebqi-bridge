**Subject:** MCG CareWebQI Bridge — handover to engineering

---

Team,

Handing over the MCG CareWebQI integration. It's a complete, working .NET 10 solution that bridges any host application (EvokeConnect today, others tomorrow) to MCG CareWebQI 12.0 — signed POST, episode round-trip, SOAP ACK, dock/popup/focus UI mechanics, and certification-script readiness all built in.

**Repository:** [https://github.com/jatinbhanot7083/mcg-carewebqi-bridge](https://github.com/jatinbhanot7083/mcg-carewebqi-bridge)

Private repo. I'll add each of you as a collaborator — please share your GitHub handle if you haven't already.

---

## What's in the box

- .NET 10 / C# bridge service with YARP reverse proxy, EF Core 10, CoreWCF
- SQL Server schema + 12 passing unit tests
- Mock MCG server for local development (replaced by real MCG via one config change when keys arrive)
- Full documentation set in both Markdown and Word format (`docs/` folder + `README`)

## Read these in order — they're designed to be followed sequentially

1. `README.docx` *(5 min — orient)*
2. `docs/HANDOVER-RUN-SAMPLE.docx` *(10 min — get it running locally)*
3. `docs/HANDOVER-ANGULAR-INTEGRATION.docx` *(60 min — the main one, has full TypeScript code for the EvokeConnect side)*
4. `docs/INTEGRATION.docx` and `docs/CONFIG.docx` *(reference — keep open while coding)*
5. `docs/PRODUCTION-DEPLOYMENT.docx` *(when we're ready to ship)*
6. The four `CERT-*.docx` files *(when MCG keys arrive and we're approaching certification)*

## First-day goal

Get the solution cloned, built, and the demo page running locally at `http://localhost:7090/demo`. Click through the dock-and-popup MCG flow against the mock. You'll see real signed POSTs, real CwqiMessage XML, and real SQL Server writes — confirming it's a working integration, not a wireframe.

## Architecture short version

- Bridge stays .NET (no Angular migration needed)
- EvokeConnect adds ~130 lines of TypeScript (a service + one component) to call the bridge
- In production, the bridge proxies real MCG under its own domain — same dock/popup/focus features work

## Questions

First check the docs — the answer to "what fields can I pass?" / "how does dock mode work?" / "what does MCG cert require?" is in there. Anything missing or unclear, ping me.

Thanks,

[Your name]
