using System.Reflection;
using System.Diagnostics;
using System;
using System.Text;
using System.Threading;

int timeout;
try
{
    timeout = int.Parse(args[0]) * 1000;
}
catch
{
    timeout = 60 * 1000;
}
var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string[] testsFile = new string[1];
try
{
    testsFile = File.ReadAllLines(Path.Combine(currentFolder, "tests.txt"));
}
catch
{
    Console.WriteLine("Tests file not found or contained invalid data, exiting.");
    Environment.Exit(1);
}
string fileToRun = testsFile[0];
// Delete any existing text
File.WriteAllText(Path.Combine(currentFolder, "results.txt"), "");
var counter = 0;
while (true)
{
    if (counter == testsFile.Length)
    {
        break;
    }
    using (Process process = new Process())
    {
        process.StartInfo.FileName = "python";
        process.StartInfo.Arguments = fileToRun;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;

        using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
        {
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    File.AppendAllText(Path.Combine(currentFolder, "results.txt"), e.Data + "\n");
                }
            };
            process.Start();
            Thread.Sleep(500);
            while (true)
            {
                if (testsFile[counter] == "endofcase")
                {
                    counter = counter + 1;
                    break;
                }
                else
                {
                    process.StandardInput.WriteLine(testsFile[counter]);
                    counter = counter + 1;
                }
            }
            process.BeginOutputReadLine();
            if (process.WaitForExit(timeout) &&
            outputWaitHandle.WaitOne(timeout))
            {
                // Process completed. Check process.ExitCode here.
            }
            else
            {
                Console.WriteLine("Test timed out");
            }
        }
    }
}