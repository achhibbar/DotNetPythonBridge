using DotNetPythonBridge;
using DotNetPythonBridge.Utils;
using Microsoft.Extensions.Logging;
using static DotNetPythonBridge.Utils.WSL_Helper;

namespace DotNetPythonBridge.SampleApp
{
    // Add this at the top of your Form1 class
    public static class SampleConfig
    {
        // Update these values for your environment
        // The name of an existing conda environment to test with
        public static string CondaEnvName = "SimpleITK_OpenCV_env";
        // The path to a test Python service script e.g., @"C:\Path\To\TestService.py"
        public static string TestServiceScriptPath = "TestService.py";
        // The path to a test Python script e.g., @"C:\Path\To\TestScript.py"
        public static string TestScriptPath = "TestScript2.py";
        // The arguments to pass to the test script (if any)
        public static string[] TestScriptArguments = new string[] { "First_Name", "Surname" };
        // The path to a test conda environment YAML file e.g., @"C:\Path\To\testCondaEnvCreate.yml"
        public static string CondaEnvYamlPath = "testCondaEnvCreate.yml";
        // If the name of the new conda environment is not specified in the YAML, this name can be used
        public static string NewCondaEnvName = "DotNetPythonBridgeTest-env";
        // The default WSL distro to use when initializing WSL-related functionality manually
        public static string DefaultWSLDistro = "Ubuntu";
        // The default conda paths for manual initialization
        public static string DefaultCondaPath = @"C:\Users\Ash\miniconda3\Scripts\conda.exe";
        // The default WSL conda path for manual initialization
        // Can either be the Linux path e.g., "/home/username/miniconda3/bin/conda"
        // or the Windows path to the WSL filesystem e.g., @"\\wsl$\Ubuntu\home\username\miniconda3\bin\conda.exe"
        public static string DefaultWSLCondaPath = @"/home/achhibbar/miniconda3/bin/conda";
    }

    public partial class Form1 : Form
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly DotNetPythonBridgeOptions options = new DotNetPythonBridgeOptions();
        private readonly PythonServiceOptions serviceOptions = new PythonServiceOptions();
        private bool isCondaInitialized = false;
        // Field to cache WSL distros
        private WSL_Helper.WSL_Distros? distros = null;

        // Fields to hold the services for Windows and WS
        // They're best defined as global fields so they can be disposed automatically if the Form is closed without calling service.Stop()
        private PythonServiceHandle? winPyService;
        private PythonServiceHandle? wslPyService;

        public Form1()
        {
            InitializeComponent();

            // Example of setting up logging

            // Keep the factory alive as a field
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddDebug() // Add Debug output
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    })
                    .SetMinimumLevel(LogLevel.Debug);
            });

            // Create a logger for the app
            var appLogger = _loggerFactory.CreateLogger<Form1>();
            appLogger.LogInformation("App starting...");

            // Pass a logger to DotNetPythonBridge
            Log.SetLogger(_loggerFactory.CreateLogger("DotNetPythonBridge"));
        }

        private async void btnListEnvs_Click(object sender, EventArgs e)
        {
            //var envs = await CondaManager.ListEnvironments();

            ////add the envs to the richTextBox
            //rtbPythonBridge.Text = string.Join(Environment.NewLine, envs) + Environment.NewLine;

            //rtbPythonBridge.Text += Environment.NewLine;

            //rtbPythonBridge.Text += CondaManager.GetCondaOrMambaPath() + Environment.NewLine;

            //rtbPythonBridge.Text += Environment.NewLine;

            ////get a specific env
            //Task<PythonEnvironment> env = CondaManager.GetEnvironment(SampleConfig.CondaEnvName);
            //rtbPythonBridge.Text += env + Environment.NewLine;

            //rtbPythonBridge.Text += Environment.NewLine;



            //var pyResult = await PythonRunner.RunCode(await env, "import sys; print(sys.executable)");
            //rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;

            //// Try another code snippet with an output
            //pyResult = await PythonRunner.RunCode(await env, "for i in range(5): print(f'Line {i}')");
            //rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;

            //rtbPythonBridge.Text += Environment.NewLine;


            //// Start a Python service (make sure you have a suitable script)
            //env = CondaManager.GetEnvironment("ultralytics-newest-env");

            //options.DefaultPort = 8080;
            //using var service = await PythonService.Start(await env, SampleConfig.TestServiceScriptPath, options);

            //// Health check
            //using var client = new HttpClient();
            //var resp = await client.GetStringAsync($"http://127.0.0.1:{service.Port}/health");
            //Console.WriteLine(resp);
        }

        private void btnTestPorts_Click(object sender, EventArgs e)
        {
            //get a free port
            rtbPythonBridge.Text += "Testing Ports..." + Environment.NewLine;
            int freePort = PortHelper.GetFreePort();
            rtbPythonBridge.Text += "Free Port: " + freePort.ToString();

            rtbPythonBridge.Text += Environment.NewLine;

            //check if a port is free
            bool isFree = PortHelper.checkIfPortIsFree(freePort);
            rtbPythonBridge.Text += $"Is Port {freePort} Free: " + isFree.ToString();
            rtbPythonBridge.Text += Environment.NewLine + Environment.NewLine;
        }

        private async void btnTestWSL_Helper_Click(object sender, EventArgs e)
        {
            if (distros == null)
            {
                //get wsl distros
                distros = await WSL_Helper.GetWSLDistros();
            }
            else
            {
                //refresh the distros
                distros = await WSL_Helper.GetWSLDistros(refresh: true);
                rtbPythonBridge.Text += "Refreshed WSL Distros." + Environment.NewLine;
            }

            rtbPythonBridge.Text += "WSL Distros:" + Environment.NewLine;
            foreach (var distro in distros.Distros)
            {
                rtbPythonBridge.Text += $"Distro: {distro.Name}, IsDefault: {distro.IsDefault}";
                rtbPythonBridge.Text += Environment.NewLine;
            }

            //get the default distro
            var defaultDistro = distros.GetDefaultDistro();
            if (defaultDistro != null)
            {
                rtbPythonBridge.Text += $"Default Distro: {defaultDistro.Name}";
                rtbPythonBridge.Text += Environment.NewLine;
            }
            rtbPythonBridge.Text += Environment.NewLine;
        }

        private async void btnTestConda_Click(object sender, EventArgs e)
        {
            if (!checkBoxWSL.Checked) // run in windows
            {

                //get conda path
                rtbPythonBridge.Text += "Getting Conda/Mamba Info..." + Environment.NewLine;
                string condaPath = await CondaManager.GetCondaOrMambaPath();
                rtbPythonBridge.Text += "Conda/Mamba Path: " + condaPath;
                rtbPythonBridge.Text += Environment.NewLine + Environment.NewLine;

                //get conda envs in windows
                rtbPythonBridge.Text += "Getting Conda/Mamba Environments..." + Environment.NewLine;
                var envs = await CondaManager.ListEnvironments();
                // add the envs to the richTextBox, each env on a new line
                foreach (var env in envs)
                {
                    rtbPythonBridge.Text += env + Environment.NewLine;
                }
                rtbPythonBridge.Text += Environment.NewLine;

                //create a new conda env using a yaml file
                rtbPythonBridge.Text += "Creating Conda/Mamba Environment from YAML..." + Environment.NewLine;
                await CondaManager.CreateEnvironment(SampleConfig.CondaEnvYamlPath, SampleConfig.NewCondaEnvName);
                rtbPythonBridge.Text += "Environment created from YAML." + Environment.NewLine;
                //list the envs again to see the new env
                envs = await CondaManager.ListEnvironments(refresh: true);
                foreach (var env in envs)
                {
                    rtbPythonBridge.Text += env + Environment.NewLine;
                }
                rtbPythonBridge.Text += Environment.NewLine;

                //delete the env
                rtbPythonBridge.Text += "Deleting Conda/Mamba Environment..." + Environment.NewLine;
                await CondaManager.DeleteEnvironment(SampleConfig.NewCondaEnvName);
                rtbPythonBridge.Text += "Environment deleted." + Environment.NewLine;
                //list the envs again to see the env is deleted
                envs = await CondaManager.ListEnvironments(refresh: true);
                foreach (var env in envs)
                {
                    rtbPythonBridge.Text += env + Environment.NewLine;
                }
                rtbPythonBridge.Text += Environment.NewLine;
            }
            else // run in wsl
            {

                //get wsl distros
                rtbPythonBridge.Text += "Getting Conda/Mamba Info in WSL..." + Environment.NewLine;
                distros = await WSL_Helper.GetWSLDistros(refresh: true);
                //get the default distro
                var defaultDistro = distros.GetDefaultDistro();
                //get the conda path in wsl default distro
                if (defaultDistro != null)
                {
                    string condaPathInWSL = await CondaManager.GetCondaOrMambaPathWSL(defaultDistro);
                    rtbPythonBridge.Text += "Conda/Mamba Path in WSL: " + condaPathInWSL;
                    rtbPythonBridge.Text += Environment.NewLine;
                }
                rtbPythonBridge.Text += Environment.NewLine;



                //get conda envs in wsl
                rtbPythonBridge.Text += "Getting Conda/Mamba Environments in WSL..." + Environment.NewLine;
                // wsl -d Ubuntu bash -lic "conda info --json"
                if (defaultDistro != null)
                {
                    var envsInWSL = await CondaManager.ListEnvironmentsWSL(defaultDistro);
                    foreach (var env in envsInWSL)
                    {
                        rtbPythonBridge.Text += env + Environment.NewLine;
                    }
                    rtbPythonBridge.Text += Environment.NewLine;
                }
                rtbPythonBridge.Text += Environment.NewLine;



                //create a new conda env using conda create in wsl
                rtbPythonBridge.Text += "Creating Conda/Mamba Environment in WSL..." + Environment.NewLine;

                if (distros.GetDefaultDistro() != null)
                {

                    await CondaManager.CreateEnvironmentWSL(SampleConfig.CondaEnvYamlPath, SampleConfig.NewCondaEnvName, distros.GetDefaultDistro());
                    rtbPythonBridge.Text += "Environment created in WSL." + Environment.NewLine;
                    //list the envs again to see the new env
                    var envs = CondaManager.ListEnvironmentsWSL(distros.GetDefaultDistro(), refresh: true); //refresh the env list to see the new env
                    foreach (var env in await envs)
                    {
                        rtbPythonBridge.Text += env + Environment.NewLine;
                    }
                    rtbPythonBridge.Text += Environment.NewLine;

                    //delete the env in wsl
                    rtbPythonBridge.Text += "Deleting Conda/Mamba Environment in WSL..." + Environment.NewLine;
                    await CondaManager.DeleteEnvironmentWSL(SampleConfig.NewCondaEnvName, distros.GetDefaultDistro());
                    rtbPythonBridge.Text += "Environment deleted in WSL." + Environment.NewLine;
                    //list the envs again to see the env is deleted
                    envs = CondaManager.ListEnvironmentsWSL(distros.GetDefaultDistro(), refresh: true); // refresh the env list to see the env is deleted
                    foreach (var env in await envs)
                    {
                        rtbPythonBridge.Text += env + Environment.NewLine;
                    }
                    rtbPythonBridge.Text += Environment.NewLine;
                }
                else
                {
                    rtbPythonBridge.Text += "No default WSL distro found. Cannot create environment in WSL." + Environment.NewLine;
                }

                //// set some options and get the default wsl distro using the options class
                //DotNetPythonBridgeOptions options = new DotNetPythonBridgeOptions
                //{
                //    DefaultCondaPath = @"F:\Ash docs\BTBP\c#\Ash projects\DotNetPythonBridgeUI\miniconda3",
                //    DefaultWSLCondaPath = "/home/ash/miniconda3",
                //    DefaultWSLDistro = "Ubuntu"
                //};
                //WSL_Helper.WSL_Distro distro = await options.GetWSL_DistroAsync();
            }
        }

        private async void btnPythonRunner_Click(object sender, EventArgs e)
        {
            bool stopped = false;

            if (!checkBoxWSL.Checked) // run in windows
            {
                if (winPyService != null) // dispose existing service first
                {
                    await winPyService.DisposeAsync();
                    winPyService = null;
                }

                //get a specific env
                var env = await CondaManager.GetEnvironment(SampleConfig.CondaEnvName);
                //get the python executable path
                string pythonExe = await PythonRunner.GetPythonExecutable(env);
                rtbPythonBridge.Text += $"Conda Env Python Executable: {env.Name} at {pythonExe}" + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;

                // start a python service in windows using the env
                rtbPythonBridge.Text += "Starting Python Service in Windows..." + Environment.NewLine;
                string serviceFilePath = SampleConfig.TestServiceScriptPath;
                winPyService = await PythonService.Start(serviceFilePath, env);
                rtbPythonBridge.Text += $"Python Service started in Windows on port {winPyService.Service.Port}" + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;

                //run a python script in windows using the env
                string scriptPath = SampleConfig.TestScriptPath;
                var pyResult = await PythonRunner.RunScript(scriptPath, env, SampleConfig.TestScriptArguments);
                rtbPythonBridge.Text += "Running Python Script in Windows:" + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;

                //inline python code for a simple math operation that returns a result
                string inlineCode = "result = 0\nfor i in range(5):\n    result += i\nprint(f'Sum of first 5 numbers is: {result}')";

                //run inline python code in windows using the env
                pyResult = await PythonRunner.RunCode(inlineCode, env);
                rtbPythonBridge.Text += "Running Inline Python Code in Windows:" + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;

                // check the health of the python service
                bool healthy = await winPyService.Service.WaitForHealthCheck(serviceOptions);
                rtbPythonBridge.Text += "Python Service Health Check: " + healthy.ToString() + Environment.NewLine;

                //stop the windows service
                if (winPyService != null)
                {
                    await winPyService.DisposeAsync();
                    winPyService = null;
                    stopped = true;
                }

                rtbPythonBridge.Text += "Python Service in Windows stopped." + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;
            }
            else // run in wsl
            {
                if (wslPyService != null) // dispose existing service first
                {
                    await wslPyService.DisposeAsync();
                    wslPyService = null;
                }

                // get a specific env in wsl
                WSL_Distros distros = await WSL_Helper.GetWSLDistros();
                var defaultDistro = distros.GetDefaultDistro();

                if (defaultDistro != null)
                {
                    var env = await CondaManager.GetEnvironmentWSL(SampleConfig.CondaEnvName, defaultDistro);
                    //get the python executable path
                    var pythonExe = await PythonRunner.GetPythonExecutableWSL(env, defaultDistro);
                    rtbPythonBridge.Text += $"Conda Env Python Executable in WSL: {env.Name} at {pythonExe}" + Environment.NewLine;
                    rtbPythonBridge.Text += Environment.NewLine;
                }
                else
                {
                    rtbPythonBridge.Text += "No default WSL distro found. Cannot get environment in WSL." + Environment.NewLine;
                }
                rtbPythonBridge.Text += Environment.NewLine;

                //run a python script in wsl using the env
                if (defaultDistro != null)
                {
                    var env = await CondaManager.GetEnvironmentWSL(SampleConfig.CondaEnvName, defaultDistro);
                    string scriptPath = SampleConfig.TestScriptPath;
                    var pyResult = await PythonRunner.RunScriptWSL(scriptPath, env, defaultDistro, SampleConfig.TestScriptArguments);
                    rtbPythonBridge.Text += "Running Python Script in WSL:" + Environment.NewLine;
                    rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                    rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                    rtbPythonBridge.Text += Environment.NewLine;
                }
                else
                {
                    rtbPythonBridge.Text += "No default WSL distro found. Cannot run script in WSL." + Environment.NewLine;
                }

                //run inline python code in wsl using the env
                if (defaultDistro != null)
                {
                    var env = await CondaManager.GetEnvironmentWSL(SampleConfig.CondaEnvName, defaultDistro);
                    string inlineCode = "result = 0\nfor i in range(5):\n    result += i\nprint(f'Sum of first 5 numbers is: {result}')";
                    var pyResult = await PythonRunner.RunCodeWSL(inlineCode, env, defaultDistro);
                    rtbPythonBridge.Text += "Running Inline Python Code in WSL:" + Environment.NewLine;
                    rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                    rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                    rtbPythonBridge.Text += Environment.NewLine;
                }
                else
                {
                    rtbPythonBridge.Text += "No default WSL distro found. Cannot run code in WSL." + Environment.NewLine;
                }

                if (defaultDistro != null)
                {
                    var env = await CondaManager.GetEnvironmentWSL(SampleConfig.CondaEnvName, defaultDistro);
                    string serviceFilePath = SampleConfig.TestServiceScriptPath;
                    rtbPythonBridge.Text += "Starting Python Service in WSL..." + Environment.NewLine;
                    wslPyService = await PythonService.StartWSL(serviceFilePath, env, wsl: defaultDistro);
                    rtbPythonBridge.Text += $"Python Service started in WSL on port {wslPyService.Service.Port}" + Environment.NewLine;
                    rtbPythonBridge.Text += Environment.NewLine;

                    // check the health of the python service
                    var healthy = await wslPyService.Service.WaitForHealthCheck(serviceOptions);
                    rtbPythonBridge.Text += "Python Service Health Check in WSL: " + healthy.ToString() + Environment.NewLine;

                    //stop the wsl service
                    if (wslPyService != null)
                    {
                        await wslPyService.DisposeAsync();
                        wslPyService = null;
                        stopped = true;
                    }

                    rtbPythonBridge.Text += "Python Service in WSL stopped." + Environment.NewLine;
                    rtbPythonBridge.Text += Environment.NewLine;
                }
                else
                {
                    rtbPythonBridge.Text += "No default WSL distro found. Cannot start service in WSL." + Environment.NewLine;
                }
            }


        }

        private async void btnLazyInit_Click(object sender, EventArgs e)
        {
            if (!isCondaInitialized)
            {
                await CondaManager.Initialize();
                isCondaInitialized = true;
                rtbPythonBridge.Text += "CondaManager Initialized Lazily." + Environment.NewLine + Environment.NewLine;
            }
            else
            {
                await CondaManager.Initialize(reinitialize: true); // allow reinitialization
                rtbPythonBridge.Text += "CondaManager Re-Initialized Lazily." + Environment.NewLine + Environment.NewLine;
            }




            rtbPythonBridge.Text += "Conda/Mamba Path: " + CondaManager.CondaPath + Environment.NewLine + Environment.NewLine;

            // get the conda envs by iterating the result of the async method
            rtbPythonBridge.Text += "Conda/Mamba Environments:" + Environment.NewLine;
            if (CondaManager.PythonEnvironments == null)
            {
                rtbPythonBridge.Text += "No Conda Environments found." + Environment.NewLine;
                return;
            }
            foreach (var env in CondaManager.PythonEnvironments.Environments)
            {
                rtbPythonBridge.Text += env + Environment.NewLine;
            }
            rtbPythonBridge.Text += Environment.NewLine;

            //rtbPythonBridge.Text += "WSL Distro: " + CondaManager.WSL.Name + Environment.NewLine + Environment.NewLine;

            rtbPythonBridge.Text += "WSL Conda/Mamba Path: " + CondaManager.WSL_CondaPath + Environment.NewLine;
            // get the conda envs in wsl by iterating through PythonEnvironmentsInWSL
            rtbPythonBridge.Text += "Conda/Mamba Environments in WSL:" + Environment.NewLine;
            if (CondaManager.PythonEnvironmentsWSL == null)
            {
                rtbPythonBridge.Text += "No Conda Environments found in WSL." + Environment.NewLine;
                return;
            }
            foreach (var env in CondaManager.PythonEnvironmentsWSL.Environments)
            {
                rtbPythonBridge.Text += env + Environment.NewLine;
            }
            rtbPythonBridge.Text += Environment.NewLine;
        }

        private async void buttonManual_Init_Click(object sender, EventArgs e)
        {
            // set some options and get the default wsl distro using the options class using fluent syntax
            DotNetPythonBridgeOptions dotNetPythonBridgeOptions = new DotNetPythonBridgeOptions()
                .WithCondaPath(SampleConfig.DefaultCondaPath)
                .WithWSLDistro(SampleConfig.DefaultWSLDistro)
                .WithWSLCondaPath(SampleConfig.DefaultWSLCondaPath); // windows path to wsl conda

            await CondaManager.Initialize(dotNetPythonBridgeOptions, reinitialize: true);

            rtbPythonBridge.Text += "Conda/Mamba Path: " + CondaManager.CondaPath + Environment.NewLine + Environment.NewLine;
            rtbPythonBridge.Text += "Conda/Mamba Environments:" + Environment.NewLine;
            if (CondaManager.PythonEnvironments == null)
            {
                rtbPythonBridge.Text += "No Conda Environments found." + Environment.NewLine;
                return;
            }
            foreach (var env in CondaManager.PythonEnvironments.Environments)
            {
                rtbPythonBridge.Text += env + Environment.NewLine;
            }
            rtbPythonBridge.Text += Environment.NewLine;
            rtbPythonBridge.Text += "WSL Distro: " + CondaManager.WSL.Name + Environment.NewLine + Environment.NewLine;
            rtbPythonBridge.Text += "WSL Conda/Mamba Path: " + CondaManager.WSL_CondaPath + Environment.NewLine;
            rtbPythonBridge.Text += "Conda/Mamba Environments in WSL:" + Environment.NewLine;
            if (CondaManager.PythonEnvironmentsWSL == null)
            {
                rtbPythonBridge.Text += "No Conda Environments found in WSL." + Environment.NewLine;
                return;
            }
            foreach (var env in CondaManager.PythonEnvironmentsWSL.Environments)
            {
                rtbPythonBridge.Text += env + Environment.NewLine;
            }
            rtbPythonBridge.Text += Environment.NewLine;
        }

        private async void btnLazyServiceStartStop_Click(object sender, EventArgs e)
        {
            if (isCondaInitialized)
            {
                CondaManager.Reset(); // reset the CondaManager to uninitialized state
            }

            CancellationTokenSource cts = new CancellationTokenSource(100000); // cancel after 100 seconds
            CancellationToken token = cts.Token;
            TimeSpan timeout = TimeSpan.FromSeconds(300); // set a timeout of 300 seconds
            bool stopped = false;

            if (!checkBoxWSL.Checked) // run in windows
            {
                if (winPyService != null) // dispose existing service first
                {
                    await winPyService.DisposeAsync();
                    winPyService = null;
                }

                // Start and stop a python service using lazy initialization
                // start a python service in windows using the env
                rtbPythonBridge.Text += "Starting Python Service in Windows..." + Environment.NewLine;
                string serviceFilePath = SampleConfig.TestServiceScriptPath;
                var env = await CondaManager.GetEnvironment(SampleConfig.CondaEnvName);
                // no env provided, will use lazy init by using the base conda env
                winPyService = await PythonService.Start(serviceFilePath, env, cancellationToken: token, timeout: timeout);
                rtbPythonBridge.Text += $"Python Service started in Windows on port {winPyService.Service.Port}" + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;

                // check the health of the python service
                bool healthy = await winPyService.Service.WaitForHealthCheck(serviceOptions);
                rtbPythonBridge.Text += "Python Service Health Check: " + healthy.ToString() + Environment.NewLine;

                //stop the windows service
                if (winPyService != null)
                {
                    await winPyService.DisposeAsync();
                    winPyService = null;
                    stopped = true;
                }
                
                // report stopping based on bool result
                rtbPythonBridge.Text += stopped ? "Python Service in Windows stopped successfully." + Environment.NewLine : "Failed to stop Python Service in Windows." + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;
            }
            else // run in wsl
            {
                if (wslPyService != null) // dispose existing service first
                {
                    await wslPyService.DisposeAsync();
                    wslPyService = null;
                }

                string serviceFilePath = SampleConfig.TestServiceScriptPath;
                rtbPythonBridge.Text += "Starting Python Service in WSL..." + Environment.NewLine;
                var env = await CondaManager.GetEnvironmentWSL(SampleConfig.CondaEnvName);
                // no env or distro provided, will use lazy init by using the base conda env in default wsl distro
                wslPyService = await PythonService.StartWSL(serviceFilePath, env, cancellationToken: token, timeout: timeout); 
                rtbPythonBridge.Text += $"Python Service started in WSL on port {wslPyService.Service.Port}" + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;

                // check the health of the python service
                var healthy = await wslPyService.Service.WaitForHealthCheck(serviceOptions);
                rtbPythonBridge.Text += "Python Service Health Check in WSL: " + healthy.ToString() + Environment.NewLine;

                //stop the wsl service
                if (wslPyService != null)
                {
                    await wslPyService.DisposeAsync();
                    wslPyService = null;
                    stopped = true;
                }
                rtbPythonBridge.Text += "Python Service in WSL stopped." + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;
            }

            isCondaInitialized = true; // set back to initialized state
        }

        private async void btnLazyInlineScriptRun_Click(object sender, EventArgs e)
        {
            if (isCondaInitialized)
            {
                CondaManager.Reset(); // reset the CondaManager to uninitialized state
            }

            // create a shared cancellation token source that can be used for all runs and has a timeout of 10 seconds
            CancellationTokenSource cts = new CancellationTokenSource(10000); // cancel after 10 seconds
            CancellationToken token = cts.Token;
            TimeSpan timeout = TimeSpan.FromSeconds(30); // set a timeout of 30 seconds

            if (!checkBoxWSL.Checked) // run in windows
            {
                //lazy run a python script in windows
                string scriptPath = SampleConfig.TestScriptPath;
                // run script with cancellation token and timeout
                var pyResult = await PythonRunner.RunScript(scriptPath, arguments: SampleConfig.TestScriptArguments, cancellationToken: token, timeout: timeout); // no env provided, will use lazy init by using the base conda env
                rtbPythonBridge.Text += "Running Python Script in Windows:" + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;

                //run inline python code in windows
                //inline python code for a simple math operation that returns a result
                string inlineCode = "result = 0\nfor i in range(5):\n    result += i\nprint(f'Sum of first 5 numbers is: {result}')";
                pyResult = await PythonRunner.RunCode(code: inlineCode, cancellationToken: token, timeout: timeout);  // no env provided, will use lazy init by using the base conda env
                rtbPythonBridge.Text += "Running Inline Python Code in Windows:" + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;
            }
            else // run in wsl
            {
                //run a python script in wsl using the env
                string scriptPath = SampleConfig.TestScriptPath;
                // no env or distro provided, will use lazy init by using the base conda env in default wsl distro
                var pyResult = await PythonRunner.RunScriptWSL(scriptPath, arguments: SampleConfig.TestScriptArguments, cancellationToken: token, timeout: timeout);
                rtbPythonBridge.Text += "Running Python Script in WSL:" + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;


                // run inline python code in wsl using the env
                string inlineCode = "result = 0\nfor i in range(5):\n    result += i\nprint(f'Sum of first 5 numbers is: {result}')";
                pyResult = await PythonRunner.RunCodeWSL(inlineCode, cancellationToken: token, timeout: timeout); // no env or distro provided, will use lazy init by using the base conda env in default wsl distro
                rtbPythonBridge.Text += "Running Inline Python Code in WSL:" + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Output + Environment.NewLine;
                rtbPythonBridge.Text += pyResult.Error + Environment.NewLine;
                rtbPythonBridge.Text += Environment.NewLine;
            }

            isCondaInitialized = true; // set back to initialized state
        }
    }
}
