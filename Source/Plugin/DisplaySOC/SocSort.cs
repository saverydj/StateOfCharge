using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisplaySOC
{
    public static class SocSort
    {
        public const string Pass = "✓";
        public const string Fail = "X";

        private const string MaxChargeName = "Max SOC";
        private const string InitalSOCName = "Initial SOC";
        private const string FinalSOCName = "Final SOC";
        private const string DeltaName = "∆SOC";

        public static string CycleNumber = "Cycle Number";
        public static string MaxCharge = "Max SOC";
        public static string InitalSOC = "Initial SOC";
        public static string FinalSOC = "Final SOC";
        public static string Delta = "∆SOC";
        public static string Depletion = "% of Total Depletion";
        public static string Recommendation = "< 2%";

        public static bool[] SortOrder = { true, false, false, false, false, false, false };
        public static string[] ColumnNames = { CycleNumber, MaxCharge, InitalSOC, FinalSOC, Delta, Depletion, Recommendation };

        public static void OrderBy(int j)
        {
            if (j >= SortOrder.Length) return;
            for(int i = 0; i < SortOrder.Length; i++)
            {
                if (i == j) SortOrder[i] = !SortOrder[i];
                else SortOrder[i] = false;
            }
        }

        public static void AppendUnitsToColumnNames(string units)
        {
            if (String.IsNullOrEmpty(units)) return;
            MaxCharge = MaxChargeName + " (" + units + ")";
            InitalSOC = InitalSOCName + " (" + units + ")";
            FinalSOC = FinalSOCName + " (" + units + ")";
            Delta = DeltaName + " (" + units + ")";
            ColumnNames = new string[]{ CycleNumber, MaxCharge, InitalSOC, FinalSOC, Delta, Depletion, Recommendation };
        }
    }
}
