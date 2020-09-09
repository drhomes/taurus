using Mono.Options;
using NUnit;
using NUnit.Engine;
using NUnitRunner.Models;
using NUnitRunner.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace NUnitRunner
{
    public class NUnitRunner
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var options = new RunnerOptions();

            try
            {
                options = OptionsParser.ParseOptions(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try running with '--help' for more information.");
                Environment.Exit(1);
            }

            var engine = TestEngineActivator.CreateInstance(true);
            var package = new TestPackage(options.TargetAssembly);

            var reportItems = new ConcurrentQueue<ReportItem>();
            var testEventListener = new TestEventListener(engine, package, reportItems, string.Empty);

            var testCount = testEventListener.Runner.CountTestCases(TestFilter.Empty);
            if (testCount == 0)
            {
                throw new ArgumentException("Nothing to run, no tests were loaded");
            }

            var userStepTime = options.RampUp / options.Concurrency;
            var reportWriter = new ReportWriter(reportItems);
            var reportWriterTask = Task.Run(() => reportWriter.StartWriting(options.ReportFile));
            var startTime = DateTime.UtcNow;
            var testTasks = new Task[options.Concurrency];

            // Set up test parameters if supplied
            var testParametersDictionary = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(options.Parameters))
            {
                foreach (string param in options.Parameters.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int eq = param.IndexOf("=");
                    if (eq > 0 && eq < param.Length - 1)
                    {
                        var name = param.Substring(0, eq).Trim();
                        var val = param.Substring(eq + 1).Trim();
                        testParametersDictionary.Add(name, val);
                    }
                }
            }
            testParametersDictionary.Add("IterationId", "0");   // parameter used to track iteration of the test run

            // Create seperate packages for each iteration to supply unique parameters
            var testPackages = new TestPackage[options.Concurrency];
            for (int i = 0; i < options.Concurrency; i++)
            {
                var testPackage = new TestPackage(options.TargetAssembly);
                if (!testPackage.Settings.ContainsKey(FrameworkPackageSettings.TestParametersDictionary))
                {
                    testPackage.AddSetting(FrameworkPackageSettings.TestParametersDictionary, null);
                }
                var testParams = new Dictionary<string, string>();
                foreach (var k in testParametersDictionary.Keys)
                {
                    testParams.Add(k, testParametersDictionary[k]);
                }
                testParams["IterationId"] = (i + 1).ToString();
                testPackage.Settings[FrameworkPackageSettings.TestParametersDictionary] = testParams;
                testPackages[i] = testPackage;
            }

            for (int i = 0; i < options.Concurrency; i++)
            {
                var threadName = "worker_" + (i + 1);
                var testPackage = testPackages[i];
                testTasks[i] = Task.Run(() => Test.RunTest(
                    startTime, options, new TestEventListener(engine, testPackage, reportItems, threadName)));
                Thread.Sleep(userStepTime * 1000);
            }

            await Task.WhenAll(testTasks);
            reportWriter.TestsCompleted = true;
            await reportWriterTask;
        }
    }
}
