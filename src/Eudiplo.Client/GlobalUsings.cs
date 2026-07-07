// Used across most of the Endpoints/ partial-class files (HttpStatusCode for idempotent
// deletes, StringContent/Encoding for request bodies, JsonDocument/JsonElement for
// response parsing) — centralized here instead of repeating the same three usings in
// ~16 of the 20 endpoint files.
global using System.Net;
global using System.Text;
global using System.Text.Json;
