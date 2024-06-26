﻿using System.Reflection;
using System.Diagnostics;

int timeout;
if (args.Length > 0 && args[0] == "version")
{
    Console.WriteLine("v1.0");
    Environment.Exit(0);
}
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
    if (args.Length > 0)
    {
        testsFile = File.ReadAllLines(args[0]);
    }
    else
    {
        testsFile = File.ReadAllLines(Path.Combine(currentFolder, "tests.txt"));
    }
    }
catch
{
    Console.WriteLine("Tests file not found or contained invalid data, exiting.");
    Environment.Exit(1);
}
string fileToRun = testsFile[0];
// Delete any existing text
if (args.Length > 1)
{
    File.WriteAllText(args[1], "");
}
else
{
    File.WriteAllText(Path.Combine(currentFolder, "results.txt"), "");
}
var counter = 1;
while (true)
{
    int inputUsed = 0;
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
        List<string> output = new List<string>();
        List<string> input = new List<string>();
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
                    if (inputUsed < input.Count)
                    {
                        var newStrings = e.Data.Split(':');
                        output.Add(newStrings[0] + ": " + input[inputUsed]);
                        output.Add(newStrings[1]);
                        inputUsed++;
                    }
                    else
                    {
                        output.Add(e.Data);
                    }
                }
            };
            process.Start();
            Thread.Sleep(200);
            while (true)
            {
                if (testsFile[counter] == "endofcase")
                {
                    counter = counter + 1;
                    break;
                }
                else
                {
                    input.Add(testsFile[counter]);
                    process.StandardInput.WriteLine(testsFile[counter]);
                    counter = counter + 1;
                }
            }
            process.BeginOutputReadLine();
            if (process.WaitForExit(timeout) &&
            outputWaitHandle.WaitOne(timeout))
            {
                // Process completed. Check process.ExitCode here.
                var finalOutput = @"""""""" + "\n";
                for (int a = 0; a < output.Count; a++)
                {
                    finalOutput = finalOutput + output[a] + "\n";
                }
                finalOutput = finalOutput + @"""""""" + "\n";
                if (args.Length > 1)
                {
                    File.AppendAllText(args[1], finalOutput);
                }
                else
                {
                    File.AppendAllText(Path.Combine(currentFolder, "results.txt"), finalOutput);
                }
            }
            else
            {
                Console.WriteLine("Test timed out");
            }
        }
    }
}