﻿namespace UnityPlayerLogAnalyzer
{
    public class LogLine
    {
        public enum LogType
        {
            Log,
            Warning,
            Error
        }

        public LogType type;
        public int sequence;
        public string message;
        public string callstack;

        public int repeat = 1;
        public int startFromSourceLine;
        public int endAtSourceLine;

        public bool SameWith( LogLine rhs )
        {
            return type == rhs.type && message == rhs.message && callstack == rhs.callstack;
        }
    }
}
