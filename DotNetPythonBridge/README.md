# DotNetPythonBridge

**Reliable Python integration for .NET (Conda, WSL, and service management)**

DotNetPythonBridge is a .NET runtime layer designed to make it *easy and reliable* to launch/monitor long-running Python services, run Python processes and manage Conda/Mamba environments (including WSL) — without brittle shell scripts or manual setup.

## 🧠 Key Benefits

- **Cross-platform support** (Windows & WSL)
- **Conda/Mamba environment discovery & management**
- **Service lifecycle management** (start, health check, stop)
- **Port reservation & retry logic**
- **Safe shell and WSL command escaping**

This library helps you embed Python workflows within your .NET apps while avoiding common pitfalls.

## 📦 Install

```bash
dotnet add package DotNetPythonBridge

# Or via NuGet Package Manager

Install-Package DotNetPythonBridge
```

## 🚀 Quick Example

```csharp
// Start a Python service (auto port)
var service = await PythonService.Start(@"path\to\script.py");

// Use service info
Console.WriteLine($"Started on port: {service.Port}, PID: {service.Pid}");

// Stop when done
service.Stop();
```

## 📌 Licensing

This package is free for non-commercial use under the Polyform Noncommercial License 1.0.0.
For commercial use, a separate commercial license is required. Visit the project repo for details:
👉 https://github.com/achhibbar/DotNetPythonBridge

## Contact
For questions or issues, please open an issue on GitHub or contact us at:
👉 **dotnetpythonbridge@gmail.com**