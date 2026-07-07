// HttpStatusCode is used in nearly every test file to enqueue fake responses; the
// TestClientFactory/FakeHttpMessageHandler pair is used in every test file that exercises
// EudiploApiClient. Centralized here instead of repeating both in ~22 of 24 files.
global using System.Net;
global using Eudiplo.Client.Tests.TestSupport;
