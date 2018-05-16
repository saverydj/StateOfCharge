using Stars.Applications.Analysis;
using STARS.Applications.Interfaces.EntityManager;
using STARS.Applications.VETS.Interfaces.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using STARS.Applications.VETS.Interfaces.Logging;
using System.Reflection;
using STARS.Applications.Interfaces.EntityProperties.CustomFields;

namespace STARS.Applications.VETS.Plugins.SOC
{
    [Export("SOC", typeof(Program)), PartCreationPolicy(CreationPolicy.Shared)]
    public class Program
    {
        public static IEntityQuery EntityQuery { get; private set; }
        public static OnlineResources OnlineResources { get; private set; }

        [ImportingConstructor]
        public Program(IEntityQuery entityQuery, OnlineResources onlineResources, ISystemLogManager systemLogManager)
        {
            EntityQuery = entityQuery;
            OnlineResources = onlineResources;
            SystemLogService.Logger = systemLogManager;
        }

        public static void GetSocDataInParallel(string testID, Test runningTest = null)
        {
            Thread getSocDataThread = new Thread(x => GetSocData(testID, runningTest)) { IsBackground = true };
            getSocDataThread.Start();
        }

        public static void GetSocData(string testID, Test runningTest = null)
        {
            string testRecordID = GetTestRecordID(testID);

            if (!CheckForCycleRepeat(runningTest)) return;        
            if (!GetNominalBatteryCF(runningTest, out string nominalBattery)) return;         
            if (!GetDataMatrices(testRecordID, out IDataMatrices dataMatrices)) return;
            string[] columnNames = new string[] { Config.SocColumnName, Config.CycleNumberColumnName, Config.SampleNumberColumnName, Config.TestStateColumnName, Config.TestTimeColumnName };
            if (!GetDataMatrixByColumnNames(dataMatrices, columnNames, testRecordID, out IDataMatrix dataMatrix)) return;
            if (!GetColumnData(dataMatrix, Config.SocColumnName, testRecordID, out object[] socColumn)) return;
            if (!GetColumnData(dataMatrix, Config.CycleNumberColumnName, testRecordID, out object[] cycleNumberColumn)) return;
            if (!GetColumnData(dataMatrix, Config.SampleNumberColumnName, testRecordID, out object[] sampleNumberColumn)) return;
            if (!GetColumnData(dataMatrix, Config.TestStateColumnName, testRecordID, out object[] testStateColumn)) return;
            if (!GetColumnData(dataMatrix, Config.TestTimeColumnName, testRecordID, out object[] testTimeColumn)) return;
            if (!GetSOCData(TypeCast.ToDouble(nominalBattery), socColumn, cycleNumberColumn, sampleNumberColumn, testStateColumn, testTimeColumn, dataMatrix, testRecordID, out List<string> socData)) return;

            DisplaySOC(ConstructCmdArgs(testID, "Ah", socData), Config.DisplayExePath);
        }

        private static bool CheckForCycleRepeat(Test runningTest)
        {
            if (runningTest == null)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No currently running test."));
                return false;
            }

            List<PropertyInfo> properties = runningTest.GetType().GetProperties().ToList();
            if (!properties.Any(x => x.Name == "CycleRepeat"))
            {
                //SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No prompt to repeat test procedure option was found on test resource {0}.", runningTest.Name));
                return false;
            }

            if (properties.FirstOrDefault(x => x.Name == "CycleRepeat").GetValue(runningTest, null).ToString() != "OperatorControlled")
            {
                //SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("Prompt to repeat test procedure option set to false on test resource {0}.", runningTest.Name));
                return false;
            }

            return true;
        }

        private static bool GetNominalBatteryCF(Test runningTest, out string nominalBattery)
        {
            Vehicle runningVehicle = EntityQuery.FirstOrDefault<Vehicle>(x => x.Name == runningTest.VehicleName);
            nominalBattery = String.Empty;
            List<CustomFieldValue> cusotmfields = runningVehicle.CustomFieldValues.ToList();
            if (!cusotmfields.Any(x => x.CustomFieldID == "NominalBatteryCapacity"))
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("Custom field {0} could not be found on the vehicle resource {1}.", "NominalBatteryCapacity", runningVehicle.Name));
                return false;
            }
            nominalBattery = cusotmfields.FirstOrDefault(x => x.CustomFieldID == "NominalBatteryCapacity").Value.Trim(' ');
            return true;
        }

        private static string GetNewestTRName()
        {
            var tr = EntityQuery.Where<TestResult>().ToList().OrderByDescending(x => x.Created).FirstOrDefault();
            return tr.Name;
        }

        private static string GetTestRecordID(string testID)
        {
            OnlineResources.AddEntry("Workstation", "SR_WorkstationStatus_ActiveProject");
            string workstation = OnlineResources.GetValueAsString("Workstation");
            return @"\\Root_UserData_Projects_" + workstation + @"_Results_" + testID;
        }

        private static bool GetDataMatrices(string testRecordID, out IDataMatrices dataMatrices)
        {
            dataMatrices =  (new TestRecordFactory()).GetTestRecord(testRecordID).Matrices;
            if (dataMatrices == null)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No data matrices could be found for the test record '{0}'", testRecordID));
                return false;
            }
            return true;
        }

        private static bool GetDataMatrix(IDataMatrices dataMatrices, string dataMatrixName, string notDataMatrixName, string testRecordID, out IDataMatrix dataMatrix)
        {
            dataMatrix = null;
            string[] notNames = notDataMatrixName.Split(',');
            for (int i = 0; i < dataMatrices.Count; i++)
            {
                bool canBeThisMatrix = true;
                foreach(string notName in notNames)
                {
                    if (dataMatrices[i].Name.Contains(notName))
                    {
                        canBeThisMatrix = false;
                        break;
                    }
                }
                if (canBeThisMatrix && dataMatrices[i].Name.Contains(dataMatrixName))
                {
                    dataMatrix = dataMatrices[i];
                    break;
                }
            }

            if (dataMatrix == null)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No matrix '{0}' could be found for the test record '{1}'", dataMatrixName, testRecordID));
                return false;
            }
            if (dataMatrix.RowCount == 0)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("Matrix '{0}' contains no row information for the test record '{1}'", dataMatrix.Name, testRecordID));
                return false;
            }
            if (dataMatrix.Columns == null)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No columns could be found in the data matrix '{0}' for the test record '{1}'", dataMatrix.Name, testRecordID));
                return false;
            }
            return true;
        }

        private static bool GetDataMatrixByColumnNames(IDataMatrices dataMatrices, string[] columnNames, string testRecordID, out IDataMatrix dataMatrix)
        {
            dataMatrix = null;
            if (columnNames.Length == 0)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn("No column names provided to search for data matrix.");
                return false;
            }
            
            bool[] hasColumns = new bool[columnNames.Length];
            for (int i = 0; i < dataMatrices.Count; i++)
            {
                for (int j = 0; j < dataMatrices[i].Columns.Count; j++)
                {
                    for(int k = 0; k < columnNames.Length; k++)
                    {
                        if (dataMatrices[i].Columns[j].Name.Contains(columnNames[k])) hasColumns[k] = true;
                    }
                }
                if (hasColumns.All(x => x))
                {
                    dataMatrix = dataMatrices[i];
                    break;
                }
                else hasColumns = new bool[columnNames.Length];
            }

            if (dataMatrix == null)
            {
                string columnNamesAsString = String.Empty;
                for (int i = 0; i < columnNames.Length; i++)
                {
                    if (columnNames.Length == 1) columnNamesAsString += columnNames[i];
                    else if (i == columnNames.Length - 1) columnNamesAsString += "and " + columnNames[i];
                    else columnNamesAsString += columnNames[i] + ", ";
                }
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No matrix containing column names: '{0}' could be found for the test record '{1}'", columnNamesAsString, testRecordID));
                return false;
            }
            if (dataMatrix.RowCount == 0)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("Matrix '{0}' contains no row information for the test record '{1}'", dataMatrix.Name, testRecordID));
                return false;
            }
            if (dataMatrix.Columns == null)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No columns could be found in the data matrix '{0}' for the test record '{1}'", dataMatrix.Name, testRecordID));
                return false;
            }
            return true;
        }

        private static bool GetColumnData(IDataMatrix dataMatrix, string columnName, string testRecordID, out object[] column)
        {
            column = null;
            for (int i = 0; i < dataMatrix.Columns.Count; i++)
            {
                if (dataMatrix.Columns[i].Name.Contains(columnName))
                {
                    column = dataMatrix.Columns[i].Data;
                }
            }
            if (column == null)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("Column '{0}' could be found in the data matrix '{1}' for the test record '{2}'", columnName, dataMatrix.Name, testRecordID));
                return false;
            }
            return true;
        }

        private static bool GetSOCData(double nominalCharge, object[] socColumn, object[] cycleNumberColumn, object[] sampleNumberColumn, object[] testStateColumn, object[] testTimeColumn, IDataMatrix dataMatrix, string testRecordID, out List<string> socData)
        {
            socData = new List<string>();
            double initalSOC = 0;
            double finalSOC = 0;
            double initialRow = 0;
            double finalRow = 0;
            int gotSamples = 0;
            int cycleNumber = 1;
            bool init = true;

            socData.Add((nominalCharge).ToString());
            socData.Add((nominalCharge).ToString());

            for (int i = 0; i < socColumn.Length; i++)
            {
                if (cycleNumber < TypeCast.ToInt(cycleNumberColumn[i].ToString()))
                {
                    //Add integrated soc info for previous cycle
                    finalSOC = (initialRow - finalRow) + initalSOC;
                    socData.Add((initalSOC).ToString());
                    socData.Add((finalSOC).ToString());
                    gotSamples = 0;
                    cycleNumber = TypeCast.ToInt(cycleNumberColumn[i].ToString());
                }
                if (TypeCast.ToInt(sampleNumberColumn[i].ToString()) > 0 && testStateColumn[i].ToString() == "2" || testStateColumn[i].ToString() == "4" || testStateColumn[i].ToString() == "6")
                {
                    if (gotSamples == 0)
                    {
                        initialRow = TypeCast.ToDouble(socColumn[i].ToString()) * (1.0 / 3600.0);

                        if (init)
                        {
                            initalSOC = nominalCharge;
                            init = false;
                        }
                        else initalSOC = (finalRow - initialRow) + finalSOC;

                        gotSamples = 1;
                    }
                    else
                    {
                        finalRow = TypeCast.ToDouble(socColumn[i].ToString()) * (1.0 / 3600.0);
                        gotSamples = 2;
                    }
                }
            }
            if (gotSamples == 2)
            {
                finalSOC = (initialRow - finalRow) + initalSOC;
                socData.Add((initalSOC).ToString());
                socData.Add((finalSOC).ToString());
            }

            if (socData.Count < 4)
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("No state of charge data could be parsed from the data matrix '{0}' for the test record '{1}'.", dataMatrix.Name, testRecordID));
                return false;
            }

            return true;
        }

        private static string ConstructCmdArgs(string testID, string units, List<string> socData)
        {
            string cmdArgs = "\"" + testID + "\"" + " " + units + " " ;
            foreach (string socDatum in socData) cmdArgs += " " + socDatum;
            return cmdArgs;
        }

        private static void DisplaySOC(string cmdArgs, string exePath)
        {
            if (!File.Exists(exePath))
            {
                SystemLogService.DisplayErrorInVETSLogNoReturn(String.Format("Executable '{0}' does not exist.", exePath));
                return;
            }

            ProcessStartInfo start = new ProcessStartInfo();
            start.Arguments = cmdArgs;
            start.FileName = exePath;
            int exitCode;

            using (Process proc = Process.Start(start))
            {
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
        }
    }
}
