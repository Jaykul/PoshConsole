using System;
using System.Collections.Generic;

namespace Huddled.PoshConsole
{
    public interface IConsoleControlCallback
    {
        void ExecuteCommand(string commandLine);
        List<String> GetTabCompletions(string commandLine);
        string GetHistory(int id);
    }
}
