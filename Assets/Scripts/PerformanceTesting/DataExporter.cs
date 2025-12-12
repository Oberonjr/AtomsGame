using System.IO;
using System.Text;
using UnityEngine;

public static class DataExporter
{
    public static void ExportForSpreadsheet(string filepath)
    {
        // Export in tab-separated format for easy spreadsheet import
        StringBuilder tsv = new StringBuilder();

        // Headers
        tsv.AppendLine("Mode\tAvg FPS\tMin FPS\t1% Low\tFrame Time (ms)\tPeak Memory (MB)\tGC Gen0\tGC Gen1\tTotal Attacks\tDuration (s)");

        // Data rows will be populated from profiler

        File.WriteAllText(filepath, tsv.ToString());
    }
}