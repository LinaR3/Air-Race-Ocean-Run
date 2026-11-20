using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace AirRace.EditorTools
{
    /// <summary>Runs the project's Play Mode tests from the Air Race menu and logs a concise report.</summary>
    public static class AirRacePlayModeTestRunner
    {
        private const string ResultPath = "/tmp/AirRace_PlayMode_QA.txt";
        private static readonly StringBuilder Report = new StringBuilder();

        [MenuItem("Air Race/Run Play Mode QA Tests")]
        public static void RunPlayModeTests()
        {
            Report.Clear();
            File.WriteAllText(ResultPath, "Running Play Mode tests...\n");
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultsLogger());
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
                assemblyNames = new[] { "AirRace.PlayModeTests" }
            }));
        }

        private sealed class ResultsLogger : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) => Report.AppendLine("Started: " + testsToRun.FullName);
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                    Report.AppendLine($"FAIL {result.FullName}: {result.Message}\n{result.StackTrace}");
                else if (result.TestStatus == TestStatus.Passed)
                    Report.AppendLine("PASS " + result.FullName);
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                Report.AppendLine($"SUMMARY passed={result.PassCount}, failed={result.FailCount}, skipped={result.SkipCount}, status={result.TestStatus}");
                File.WriteAllText(ResultPath, Report.ToString());
                Debug.Log("AIR_RACE_QA_FINISHED: " + Report.ToString());
            }
        }
    }
}
