using System;
using System.Collections.Generic;
using System.Text;

namespace UnityPlayerLogToYaml
{
    class MainProcessor
    {

        public bool m_IgnoreMessageLog = false;
        public int m_GroupingStyle = 1;
        private List<LogLine> m_LogLines;

        public void Convert(string path)
        {
            //m_IgnoreMessageLog = optNoLog.Checked;
            //m_GroupingStyle = optGrouping.SelectedIndex;

            // Use sb because it is faster and less garbage
            StringBuilder sb = new StringBuilder(System.IO.File.ReadAllText(path));

            // Sanitize it, in this order
            // \r\n to \n
            // \r to \n
            sb.Replace("\r\n", "\n");
            sb.Replace("\r", "\n");

            // Extract logging part
            // Strategy is to revolve around "UnityEngine.Debug:Log" on start of the line, simple and stupid, no regex
            m_LogLines = new List<LogLine>();
            string[] allLines = sb.ToString().Split('\n');
            sb.Clear();
            _ExtractLogLines(m_LogLines, allLines);

            if (m_LogLines.Count == 0)
            {
                Console.WriteLine($"Program cannot detect pattern such as\n{CaptureMethod1.LogKeyword}\nLog file might be from non-dev mode or script debugging turned off.\nNow giveup or go to github to modify this program yourself!\n\nTeehee!", "Teehee!");
                return;
            }

            _WriteToOutputLog(sb, allLines, m_LogLines);

            string outputPath = path + ".yaml";
            System.IO.File.WriteAllText(outputPath, sb.ToString());
            Console.WriteLine($"Output to {outputPath}\nLog line found {m_LogLines.Count}");
        }

        private void _ExtractLogLines(List<LogLine> logs, string[] allLines)
        {
            int sequence = 1;
            for (int i = 0; i < allLines.Length; i++)
            {
                // Pattern 1
                if (CaptureMethod1.Detect(allLines[i]))
                {
                    LogLine ll = new LogLine();
                    ll.sequence = sequence;
                    sequence++;

                    CaptureMethod1.CaptureLogLineAround(i, allLines, ll);
                    _AddByGrouping(ll, logs, m_GroupingStyle);

                    i = ll.endAtSourceLine + 1;
                }
                else if (CaptureMethod2.Detect(allLines[i]))
                {
                    LogLine ll = new LogLine();
                    ll.sequence = sequence;
                    sequence++;

                    CaptureMethod2.CaptureLogLineAround(i, allLines, ll);
                    _AddByGrouping(ll, logs, m_GroupingStyle);

                    i = ll.endAtSourceLine + 1;
                }
            }

        }

        private static void _AddByGrouping(LogLine ll, List<LogLine> logs, int groupingStyle)
        {
            // Depends on grouping option, we could ignore this if it is identical source from last log
            if (groupingStyle == 0)
            {
                // Always add
            }
            else if (groupingStyle == 1)
            {
                // Only same as last message
                LogLine last = null;
                if (logs.Count > 0)
                    last = logs[logs.Count - 1];

                // Compare with last one
                if (last != null)
                {
                    if (last.SameWith(ll))
                    {
                        last.repeat++;
                        return; // Skip to next
                    }
                }
            }
            else if (groupingStyle == 2) // Group any
            {
                // Search all past logs
                for (int i = 0; i < logs.Count; i++)
                {
                    if (logs[i].SameWith(ll))
                    {
                        logs[i].repeat++;
                        return;
                    }
                }
            }

            logs.Add(ll);
        }

        /* Outputtting //////////////////////////////////////////////*/

        private void _WriteToOutputLog(StringBuilder sb, string[] allLines, List<LogLine> logLines)
        {
            sb.AppendLine();
            sb.AppendLine($"# Exported with Wappen's UnityPlayerLogAnalyzer =====================");
            sb.AppendLine($"# Export options");
            sb.AppendLine($"ExcludeLog: {m_IgnoreMessageLog}");
            sb.AppendLine($"GroupingStyle: {m_GroupingStyle}");
            sb.AppendLine($"# ===================================================================");
            sb.AppendLine();

            // Count all types
            int log, warning, error;
            log = warning = error = 0;
            foreach (LogLine ll in logLines)
            {
                switch (ll.type)
                {
                    case LogLine.LogType.Error: error++; break;
                    case LogLine.LogType.Warning: warning++; break;
                    default: log++; break;
                }
            }

            sb.AppendLine($"# Counters");
            sb.AppendLine($"Error: {error}");
            sb.AppendLine($"Warning: {warning}");
            string ignoreMsg = m_IgnoreMessageLog ? " # Ignored" : "";
            sb.AppendLine($"Log: {log}{ignoreMsg}");
            sb.AppendLine();

            sb.AppendLine("# Log lines start here! ----------------------------------------------");
            sb.AppendLine("Open in Visual Studio Code, press CTRL-A, to select all. Then press CTRL+K, CTRL+0 to collapse all.");
            sb.AppendLine();

            _PrintClientMachineInfo(sb, allLines);

            foreach (LogLine ll in logLines)
                _WriteToOutputLogSingle(sb, ll);
        }

        private void _PrintClientMachineInfo(StringBuilder sb, string[] allLines)
        {
            // Output yaml here
            int firstLogStartsAtLine = allLines.Length;
            if (m_LogLines.Count > 0)
                firstLogStartsAtLine = m_LogLines[0].startFromSourceLine;

            // Print first part because it captures client machine info and other etc.
            sb.AppendLine(LineUtil.CaptureTextFromToLine(0, firstLogStartsAtLine - 1, allLines));
        }

        private void _WriteToOutputLogSingle(StringBuilder sb, LogLine ll)
        {
            if (ll.type == LogLine.LogType.Log && m_IgnoreMessageLog)
                return;

            // Header
            sb.Append($"{ll.type} {ll.sequence}: ");
            string msg = ll.message.Replace("\n", "\n  "); // If there is newline in message, indent it
            if (ll.type == LogLine.LogType.Error)
            {
                sb.Append("#"); // Start with yaml multiline lateral to hightlight it to another color
            }
            else if (ll.type == LogLine.LogType.Warning)
            {
                sb.Append("#");
            }
            else if (ll.type == LogLine.LogType.Log)
            {
                sb.Append("#");
            }

            if (ll.repeat > 1)
                sb.Append($"(x{ll.repeat}) "); // Show collapsed occurrence

            sb.AppendLine(msg);

            // Callstack
            const string StartListIndent = "  - ";
            string c = ll.callstack.Replace("\n", "\n" + StartListIndent); // Insert indent for each line
            //sb.AppendLine( "  Callstack:" );
            sb.AppendLine(StartListIndent + c);

            //sb.AppendLine( ); // One last blank line for entry to separated when expanded
        }
    }
}
