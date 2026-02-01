# DotNetPythonBridge

DotNetPythonBridge is a small .NET library that makes it easier to run Python executables, manage Conda/Mamba environments (including WSL), and start/monitor long-running Python services from .NET applications. It provides helpers for process execution, escaping shell/WSL commands, port reservation and basic service health checks.

This README describes:
- What the library does
- Quick start and API examples
- Configuration and options
- WSL / Conda / Mamba behavior
- Port reservation and service-start semantics (important note about races)
- Troubleshooting, tests, and contribution pointers

---

## Table of contents
- [Key features](#key-features)
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

Run a Python script and capture output:
```csharp
using DotNetPythonBridge;
using DotNetPythonBridge.Utils;

// Run a script with arguments
var result = await ProcessHelper.RunProcess("python", "script.py --arg1 value");
if (result.ExitCode == 0)
{
    Console.WriteLine(result.Output);
}
else
{
    Console.Error.WriteLine(result.Error);
}
```

Start a long-running Python service (with retries + health check):
```csharp
using DotNetPythonBridge;

// Start will attempt up to PythonServiceOptions.ServiceRetryCount retries if the health-check fails
var options = new PythonServiceOptions()
    .WithPort(0) // 0 = auto; library will pick a free port (with reservation/retry)
    .WithServiceArgs("--host 127.0.0.1")
    .WithServiceRetryCount(3)
    .EnableHealthCheck(true);

var service = await PythonService.Start("path/to/my_service.py", env: null, options: options);

// Use the service
Console.WriteLine($"Service started (PID: {service.Pid}) on port {service.Port}");

// When finished
await service.Stop();
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

Run a command inside WSL:
```csharp
// The library contains WSL helper utilities. Example usage differs depending on target.
// See WSL_Helper methods in the repo for specifics.
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

## Licensing

DotNetPythonBridge is **open-source and free for non-commercial use**.

### ✅ Free Use
- Personal projects
- Research and experimentation
- Academic and educational use
- Open-source, non-commercial projects

### 💼 Commercial Use
A **paid commercial license is required** if you use DotNetPythonBridge:
- In a for-profit company
- In internal tools supporting revenue
- In SaaS, hosted services, or paid APIs
- As part of a product or service offered for sale
- For consulting or client deliverables

If you're unsure whether your use is commercial, **assume that it is**.

📧 Contact **[your email]** for commercial licensing.
