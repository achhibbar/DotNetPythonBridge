# DotNetPythonBridge — Installation & Quick Start

This document explains how to install DotNetPythonBridge from the downloadable ZIP (included with commercial purchases) or from a NuGet feed, and troubleshooting tips. It covers both Windows (PowerShell) and cross‑platform (bash/WSL) commands.

Contents of the downloadable ZIP
- DotNetPythonBridge.1.0.0-beta.2.nupkg
- DotNetPythonBridge.1.0.0-beta.2.snupkg
- README_INSTALL.md
- COMMERCIAL_LICENSE.md
- SampleApp (folder with a .NET app that demonstrates usage)

## Prerequisites
- .NET SDK 8.0: Ensure you have the .NET SDK version 8.0 installed on your machine. This SDK is required to build and run .NET applications using the DotNetPythonBridge.
- A project that targets a compatible framework (see NuGet package description)
- Visual Studio, Rider, or VS Code for development
- If using WSL: an installed Linux distribution with Python available
- Conda or Mamba for Python environment management

## Installation options (choose one)

A — Install from local ZIP (recommended for Lemon Squeezy downloadable)
Extract the ZIP to a folder (example: `C:\DotNetPythonBridgeDownloads` or `/home/user/DotNetPythonBridgeDownloads`).

PowerShell (Windows)
```powershell
# create a local folder and extract ZIP (if needed)
Expand-Archive -Path .\DotNetPythonBridge-1.0.0-beta.2.zip -DestinationPath C:\DotNetPythonBridgeDownloads

# add a local nuget source
dotnet nuget add source "C:\DotNetPythonBridgeDownloads" -n DotNetPythonBridgeLocal

# add package to your project
dotnet add path\to\YourProject.csproj package DotNetPythonBridge --version 1.0.0-beta.2 --source DotNetPythonBridgeLocal

# restore and build
dotnet restore
dotnet build
```

Bash / macOS / Linux / WSL
```bash
# extract
unzip DotNetPythonBridge-1.0.0-beta.2.zip -d ~/DotNetPythonBridgeDownloads

# add a local nuget source
dotnet nuget add source "~/DotNetPythonBridgeDownloads" -n DotNetPythonBridgeLocal

# add package to your project
dotnet add path/to/YourProject.csproj package DotNetPythonBridge --version 1.0.0-beta.2 --source DotNetPythonBridgeLocal

# restore & build
dotnet restore
dotnet build
```

B — Install from NuGet.org
```bash
# from repo root or project folder
dotnet add path/to/YourProject.csproj package DotNetPythonBridge --version 1.0.0-beta.2
dotnet restore
dotnet build
```

## Example quick start (run the Sample App)
1. Open the sample folder (DotNetPythonBridge.SampleApp).
2. Build & run:
```bash
dotnet build DotNetPythonBridge.SampleApp
dotnet run --project DotNetPythonBridge.SampleApp
```
3. The sample app allows you test the bridge and see example code, including how to start python service, call python code, and handle results (natively or WSL).

## Support & contact
- For purchase/download issues from Lemon Squeezy: use Lemon Squeezy support and include your order ID.
- For technical issues with the package or sample app: open a GitHub Issue at https://github.com/achhibbar/DotNetPythonBridge/issues or use Discussions for general questions.
- For license, pricing or enterprise feed requests: email dotnetpythonbridge@gmail.com

## Contact & contribution
- Repo: https://github.com/achhibbar/DotNetPythonBridge
- NuGet package: https://www.nuget.org/packages/DotNetPythonBridge/
- Please file issues or feature requests on GitHub so we can track them.

Thank you for trying DotNetPythonBridge — if anything in this install guide is unclear, open a GitHub Discussion or send the order ID to support and we’ll help you get running quickly.