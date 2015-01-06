/// <copyright file="Program.cs" company="Panopto Inc.">
/// Copyright (c) 2012 All Rights Reserved
/// </copyright>
/// <summary>
/// Sample C# client that uses the Panopto PublicAPI
/// This sample builds a console app that connects to the server via the PublicAPI to schedule
/// back-to-back recordings for the Panopto Remote Recorder
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;

using ScheduleTool.RemoteRecorderManagementService;
using ScheduleTool.SessionManagementService;


namespace ScheduleTool
{
    class Program
    {
        static bool hasBeenInitialized = false;

        // Command line args        
        static string user;
        static string password;
        static string folderName;
        static string rrName;
        static int recordingMins;
        static int numRecordings;

        const int numArguments = 6;

        /// <summary>
        /// Print command line usage
        /// </summary>
        static void Usage()
        {
            Console.WriteLine("ScheduleTool -u [user] -p [password] -f [folder] -r [rr description] -l [recording length mins] -n [number of recordings]");
        }

        /// <summary>
        /// Parse command line arguments
        /// </summary>
        static bool ParseArgs(string[] args)
        {
            bool done = false;

            if (args.Length == (numArguments * 2))
            {
                for (int i = 0; i < args.Length && !done; i += 2)
                {
                    if (args[i].Length != 2 ||
                        args[i][0] != '-' ||
                        i + 1 >= args.Length)
                    {
                        break;
                    }

                    switch (args[i][1])
                    {
                        case 'u':
                            user = args[i + 1];
                            break;

                        case 'p':
                            password = args[i + 1];
                            break;

                        case 'f':
                            folderName = args[i + 1];
                            break;

                        case 'r':
                            rrName = args[i + 1];
                            break;

                        case 'l':
                            recordingMins = Int32.Parse(args[i + 1]);
                            break;

                        case 'n':
                            numRecordings = Int32.Parse(args[i + 1]);
                            break;

                        default:
                            done = true;
                            break;
                    }
                }
            }

            return  user != null &&
                    password != null &&
                    folderName != null &&
                    rrName != null &&
                    recordingMins != 0 &&
                    numRecordings != 0;
        }

        static void Assert(bool condition, string errorMsg)
        {
            if (!condition)
            {
                throw new Exception(errorMsg);
            }
        }

        /// <summary>
        /// Ensures that our custom certificate validation has been applied
        /// </summary>
        public static void EnsureCertificateValidation()
        {        
            if (!hasBeenInitialized)
            {
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(CustomCertificateValidation);
                hasBeenInitialized = true;
            }
        }

        /// <summary>
        /// Ensures that server certificate is authenticated
        /// </summary>
        private static bool CustomCertificateValidation(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {         
            return true;
        }


        /// <summary>
        /// Main application method
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                // Parse args
                if (!ParseArgs(args))
                {
                    Usage();
                    return;
                }

                // Instantiate service clients
                ISessionManagement sessionMgr = new SessionManagementClient();
                IRemoteRecorderManagement rrMgr = new RemoteRecorderManagementClient();

                // Ensure server certificate is validated
                EnsureCertificateValidation();

                // Create auth info. For this sample, we will be passing auth info into all of the PublicAPI web service calls
                // instead of using IAuth.LogonWithPassword() (which is the alternative method).
                SessionManagementService.AuthenticationInfo sessionAuthInfo = new SessionManagementService.AuthenticationInfo()
                                                                              {
                                                                                  UserKey = user,
                                                                                  Password = password
                                                                              };

                RemoteRecorderManagementService.AuthenticationInfo rrAuthInfo = new RemoteRecorderManagementService.AuthenticationInfo()
                                                                                {
                                                                                    UserKey = user,
                                                                                    Password = password
                                                                                };

                int resultPerPage = 10;
                int totalResult = 0;
                int readResult = 0;
                int pageNumber = 0;
                Folder folder = null;
                // Find folder 
                do 
                {
                    ScheduleTool.SessionManagementService.Pagination pagination = new ScheduleTool.SessionManagementService.Pagination { MaxNumberResults = resultPerPage, PageNumber = pageNumber };
                    ListFoldersResponse response = sessionMgr.GetFoldersList(sessionAuthInfo, new ListFoldersRequest { Pagination = pagination }, null);
                    folder = response.Results.FirstOrDefault(a => a.Name == folderName);

                    totalResult = response.TotalNumberResults;
                    readResult += resultPerPage;
                    pageNumber++;

                    if (folder != null)
                    {
                        break;
                    }
                } while (readResult < totalResult);

                Assert(folder != null, "Could not find specified folder - " + folderName);

                // Find RR
                totalResult = 0;
                readResult = 0;
                pageNumber = 0;
                RemoteRecorder recorder = null;
                do
                {
                    ScheduleTool.RemoteRecorderManagementService.Pagination rrPagination = new ScheduleTool.RemoteRecorderManagementService.Pagination { MaxNumberResults = resultPerPage, PageNumber = pageNumber };
                    ListRecordersResponse rrResponse = rrMgr.ListRecorders(rrAuthInfo, rrPagination, RecorderSortField.Name);
                    recorder = rrResponse.PagedResults.FirstOrDefault(a => a.Name == rrName);

                    totalResult = rrResponse.TotalResultCount;
                    readResult += resultPerPage;
                    pageNumber++;

                    if (recorder != null)
                    {
                        break;
                    }
                } while (readResult < totalResult);

                Assert(recorder != null, "Could not find specified recorder - " + rrName);
                List<ScheduleTool.RemoteRecorderManagementService.RecorderSettings> recorderSettings = new List<RecorderSettings>();
                recorderSettings.Add(new RecorderSettings { RecorderId = recorder.Id });                

                // Schedule back-to-back recordings
                DateTime timeBase = DateTime.UtcNow;

                for (int i = 0; i < numRecordings; i++)
                {
                    DateTime startTime = timeBase + TimeSpan.FromMinutes(recordingMins * i);
                    DateTime endTime = timeBase + TimeSpan.FromMinutes(recordingMins * (i + 1));
                    ScheduledRecordingResult result = rrMgr.ScheduleRecording(rrAuthInfo, "TestRecording" + (i+1).ToString(), folder.Id, false, startTime, endTime, recorderSettings.ToArray());
                    Assert(!result.ConflictsExist, "Unable to schedule test recording due to conflicts");
                }

                Console.WriteLine("Recordings have been scheduled");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
