ScheduleTool notes
------------------

This tool uses the Panopto PublicAPI to schedule back-to-back test recordings for the Panopto Remote Recorder (RR).

Build steps:
- Open VS2010 solution
- In Solution Explorer, under Service References, select "Configure Service Reference" for both services
- Update Address field to point to the correct server, click OK.
- Edit app.config, ensure the following are set:
  - Endpoint URLs should reference "PublicAPI", not "PublicAPISSL", eg:
    - https://[servername]/Panopto/PublicAPI/4.2/Auth.svc
- Build project

Usage steps:
- Setup a RR client to connect to a RR server
- Edit ScheduleTool.exe.config and ensure the endpoint addresses point to your server
- Run "ScheduleTool" without any arguments to see the argument list.
- This tool does not support spaces in the folder and RR names
- The back-to-back recordings are scheduled starting from the time the tool is run
