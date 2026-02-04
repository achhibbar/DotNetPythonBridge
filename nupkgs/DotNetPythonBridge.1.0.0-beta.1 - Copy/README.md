# DotNetPythonBridge

DotNetPythonBridge is a .NET runtime layer for executing and managing Python processes, environments, and services across Windows and WSL.

If you’ve ever struggled with:
- Hard-coded Python or Conda paths
- Different behavior between Windows and WSL
- Shell quoting and escaping issues
- Orphaned Python processes or stuck ports
- Python services that work locally but fail in deployment

…this library exists to remove that friction.

DotNetPythonBridge provides a clean .NET API for:
- Discovering and running Python, Conda, or Mamba (native or via WSL)
- Managing and activating environments safely
- Starting, monitoring, and stopping long-running Python services
- Reserving ports and performing basic health checks

It’s built for developers who want to **embed Python into .NET applications** without turning process management into a maintenance burden.

---

## Table of contents
- [Key features](#key-features)
- [Who is this for?](#who-is-this-for)
- [Install / build](#install--build)
- [Quick examples](#quick-examples)
  - [Run a Python script](#run-a-python-script)
  - [Start a long-running Python service](#start-a-long-running-python-service)
  - [List Conda environments](#list-conda-environments)
- [Options and configuration](#options-and-configuration)
- [WSL, Conda and Mamba](#wsl-conda-and-mamba)
- [Port reservation and races — important note](#port-reservation-and-races---important-note)
- [Logging and diagnostics](#logging-and-diagnostics)
- [Tests](#tests)
- [Contributing](#contributing)
- [License](#license)

---

## Key features
- Run arbitrary Python processes (script files or inline code) with robust process management and cancellation.
- Discover and manage Conda/Mamba environments on Windows and WSL.
- Start and monitor long-running Python services with optional health checks and retry behavior.
- Helpers for safe bash/WSL argument escaping to avoid shell-injection pitfalls.
- Utilities for reserving TCP ports (with retry mitigation for races).
- Logging via Microsoft.Extensions.Logging (Serilog used in the repository).

---

## Who is this for?

- .NET developers embedding Python for ML, CV, or data processing
- Applications that need to start and manage Python services
- Teams using Conda/Mamba across Windows and WSL
- Anyone tired of debugging shell scripts in production

If you just need to run a one-off Python script, this may be overkill.
If Python is part of your application architecture, this library is designed for you.

---

## Install / build

This repository is primarily C# (library) with a few Python helpers. There is no published NuGet package in the README — to use the library:

1. Clone the repo:
   ```bash
   git clone https://github.com/achhibbar/DotNetPythonBridge.git
   ```

2. Build:
   ```bash
   cd DotNetPythonBridge
   dotnet build
   ```

3. Reference the project from your .NET app (either via project reference or by packaging a NuGet).

---

## Quick examples

Note: the examples below assume you reference the library project or compiled assembly and have a logger configured similar to the repository.

Start a long-running Python service. If no Conda environment is specified, the base environment is used:
```csharp
using DotNetPythonBridge;

// Start the service (uses auto-assigned port by default)
var service = await PythonService.Start(@"path\to\my_service.py");

// Get the assigned port and PID from the service
Console.WriteLine($"Service started (PID: {service.Pid}) on port {service.Port}");

// When finished with the service, stop it (this will dispose resources)
await service.Stop();
```

Start a long-running Python service in WSL with the default distro, a specific Conda environment, and auto-assigned port:
```csharp
using DotNetPythonBridge;

// Get a Conda environment by name from the default WSL distro
var condaEnv = await CondaManager.GetEnvironmentWSL("my_env");

// Start the service (uses auto-assigned port by default)
var service = await PythonService.StartWSL(@"path\to\my_service.py", condaEnv);

// Get the assigned port and PID from the service
Console.WriteLine($"Service started (PID: {service.Pid}) on port {service.Port}");

// When finished with the service, stop it (this will dispose resources)
await service.Stop();
```

Run a Python script and capture output:
```csharp
using DotNetPythonBridge;

// Get a Conda environment by name
var condaEnv = await CondaManager.GetEnvironment("my_env");

// Arguments for the script
string[] arguments = new string[] { "--arg1", "value" };

// Run a script with arguments using the specified Conda environment
var result = await PythonRunner.RunScript("path/to/my_script.py", condaEnv, arguments);

// Check for error using result.Error, and if no error, print output
if (string.IsNullOrEmpty(result.Error))
{
	Console.WriteLine($"Output: {result.Output}");
}
```

List Conda environments:
```csharp
using DotNetPythonBridge;

// Ensure CondaManager initialized (optional)
await CondaManager.Initialize(new DotNetPythonBridgeOptions());

// Get list of environments
var envs = await CondaManager.ListEnvironments();
foreach(var env in envs)
    Console.WriteLine($"{env.Name} -> {env.Path}");
```

Create environment from YAML:
```csharp
await CondaManager.CreateEnvironment("env.yml"); // calls conda env create -f "env.yml"
```

---

## Options and configuration

Primary options objects:
- `DotNetPythonBridgeOptions` — global / initialization options (WSL defaults, timeouts).
- `PythonServiceOptions` — controls service start behavior:
  - `DefaultPort` (int): 0 = auto (ephemeral), or a specific port.
  - `DefaultServiceArgs` (string): extra args passed to the script (escaped).
  - `HealthCheckEnabled` (bool): perform health check after start.
  - `ServiceRetryCount` (int): number of start attempts if health check fails.
  - Timeouts: `HealthCheckTimeoutSeconds`, `ForceKillTimeoutMilliseconds`, `StopTimeoutMilliseconds`.

You can call `CondaManager.Initialize(...)` with a `DotNetPythonBridgeOptions` instance to set defaults such as `DefaultWSLDistro`, `DefaultCondaPath`, and timeouts.

---

## WSL, Conda and Mamba

- The library attempts to locate an executable to run conda-like commands. It searches in this order:
  - `conda` (or `conda.exe` on Windows)
  - `mamba` (or `mamba.exe` on Windows)
- That means if both conda and mamba are present and discoverable on PATH, the library will prefer conda.
- If you want to force the library to use a specific executable, pass the path in `DotNetPythonBridgeOptions` (e.g., set `DefaultCondaPath`) during `CondaManager.Initialize(...)`.

WSL notes:
- WSL paths are handled using helper methods that convert Windows paths to WSL-style paths and vice versa where needed.
- Some WSL commands may require Unicode encoding; the library uses configurable encodings for process standard output when necessary.

---

## Port reservation and races — important note

Because arbitrary Python services usually bind their own socket, there's an inherent race if the library "reserves" a free port and then releases it before the child process binds it. This repo implements the following pragmatic approach:

- The library can reserve a free port (via `PortHelper.ReservePort`) to find a free port and momentarily bind it.
- The reservation is released immediately before starting the child process so the child can bind the port. That creates a short race window.
- To mitigate failures caused by that race, `PythonService.Start(...)` implements retry logic:
  - On start failure (health-check fails or the child exits quickly with bind errors), the library will retry up to `PythonServiceOptions.ServiceRetryCount` times, reserving a new free port each attempt.
- If you control the Python service, the recommended (race-free) approaches are:
  - Support `--port 0` (let OS choose a free port) and print/report the chosen port to stdout or a file so the .NET side can read it.
  - Use socket inheritance / activation: parent creates the listening socket and passes the open handle to the child. This requires cooperation from the Python service and platform-specific handling.

In short: if you cannot modify the Python service, the library's retry approach is the practical mitigation. If you can modify the service, prefer port-0 reporting or socket inheritance for atomic handoff.

---

## Logging and diagnostics

- The repo uses `Log.Logger` (Serilog) for logging. Configure your application logger as appropriate.
- Useful diagnostic points:
  - Conda/WSL discovery logs (`CondaManager`)
  - Port reservation and service-start logs (`PortHelper`, `PythonService`)
  - Process execution output captured by `ProcessHelper` (returned as `PythonResult`)

Be mindful of logging sensitive data — arguments are escaped, but avoid logging secrets in service args.

---

## Tests

- Unit tests reside in `DotNetPythonBridge.Tests`. They cover `ProcessHelper` behavior (cancellation, encodings) and other helpers.
- To run tests:
  ```bash
  dotnet test
  ```

Suggested additional tests:
- Simulate a pre-bound port and verify `PythonService` retry behavior.
- WSL discovery tests (may require WSL on host).
- FilenameHelper path conversion edge cases and Windows UNC paths.

---

## Troubleshooting

- If commands fail to find conda/mamba:
  - Ensure the desired executable is on PATH or pass the exact path via `DotNetPythonBridgeOptions.DefaultCondaPath`.
- Intermittent WSL failures:
  - Confirm WSL is available (`wsl --list --verbose`) and that the distro is not in a sleeping state. Consider increasing relevant WSL/command timeouts in options.
- Service fails to bind port:
  - This can be due to a race condition. Increase `ServiceRetryCount` or modify the Python service to support port `0` and report the chosen port.

---

## License

This project is **source-available** and licensed under the  
**Polyform Noncommercial License 1.0.0**.

### What this means

✅ **Free to use for:**
- Personal projects
- Research
- Evaluation
- Educational use
- Open-source or non-commercial projects

❌ **Not free for:**
- Commercial use
- Internal business tools
- SaaS products
- Paid services
- Any revenue-generating activity

### Commercial use

If you wish to use this software for **commercial purposes**, you must obtain a
**commercial license**.

Commercial licenses are available here:
> 🔗 **[Link coming soon — Lemon Squeezy]**

Until the commercial license page is live, please contact:
> **dotnetpythonbridge@gmail.com**

### Why this model?

This licensing model allows the project to:
- Remain openly available and transparent
- Be free for learning and experimentation
- Sustain continued development through commercial funding

For full license terms, see:
- [`LICENSE.md`](./LICENSE.md)

