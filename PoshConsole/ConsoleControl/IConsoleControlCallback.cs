using System;
using System.Collections.Generic;

namespace PoshConsole.Controls
{
    public interface IConsoleControlCallback
    {
        void ExecuteCommand(string commandLine);
        List<String> GetTabCompletions(string commandLine);
        string GetHistory(int id);
    }
}
