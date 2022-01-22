﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Data;

namespace Lotlab
{
    public enum LogLevel
    {
        TRACE,
        DEBUG,
        INFO,
        WARNING,
        ERROR,
    }

    /// <summary>
    /// 简易日志记录器
    /// </summary>
    public class SimpleLogger : IDisposable
    {
        readonly object logLock = new object();
        List<LogItem> logs = new List<LogItem>();
        FileStream logFile = null;
        StreamWriter logFileWriter = null;

        public ObservableCollection<LogItem> ObserveLogs { get; } = new ObservableCollection<LogItem>();


        /// <summary>
        /// 创建一个日志记录器
        /// </summary>
        /// <param name="file">日志文件</param>
        /// <param name="filter">默认过滤等级</param>
        public SimpleLogger(string file, LogLevel filter = LogLevel.INFO)
        {
            filterLevel = filter;

            if (!string.IsNullOrEmpty(file))
            {
                logFile = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                logFileWriter = new StreamWriter(logFile);
            }

            BindingOperations.EnableCollectionSynchronization(ObserveLogs, logLock);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            logFileWriter?.Flush();
            logFileWriter?.Close();
            logFileWriter = null;

            logFile?.Close();
            logFile = null;
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level"></param>
        /// <param name="content"></param>
        public void Log(LogLevel level, string content)
        {
            lock (logLock)
            {
                var item = new LogItem(level, content);
                logs.Add(item);
                if (level >= filterLevel)
                {
                    ObserveLogs.Add(item);
                }
                if (logFileWriter != null)
                {
                    logFileWriter.Write(item.ToString());
                    logFileWriter.Write("\n");
                    logFileWriter.Flush();
                }
            }
        }

        /// <summary>
        /// 当前过滤等级
        /// </summary>
        LogLevel filterLevel = LogLevel.INFO;

        /// <summary>
        /// 设置过滤等级
        /// </summary>
        /// <param name="level"></param>
        public void SetFilter(LogLevel level)
        {
            if (filterLevel != level)
            {
                filterLevel = level;

                lock (logLock)
                {
                    ObserveLogs.Clear();
                    foreach (var item in logs)
                    {
                        if (item.Level >= filterLevel)
                            ObserveLogs.Add(item);
                    }
                }
            }
        }

        public void LogTrace(string content)
        {
            Log(LogLevel.TRACE, content);
        }
        public void LogDebug(string content)
        {
            Log(LogLevel.DEBUG, content);
        }
        public void LogInfo(string content)
        {
            Log(LogLevel.INFO, content);
        }
        public void LogWarning(string content)
        {
            Log(LogLevel.WARNING, content);
        }
        public void LogError(string content)
        {
            Log(LogLevel.ERROR, content);
        }
        public void LogError(Exception e)
        {
            LogError(e.ToString());
        }

    }

    public class LogItem
    {
        public DateTime Time { get; }

        public LogLevel Level { get; }

        public string Content { get; }

        public LogItem(LogLevel level, string content)
        {
            Level = level;
            Content = content;
            Time = DateTime.Now;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Time.ToString("O"));
            sb.Append(" - [");
            sb.Append(Level.ToString());
            sb.Append("] ");
            sb.Append(Content);
            return sb.ToString();
        }
    }

}
