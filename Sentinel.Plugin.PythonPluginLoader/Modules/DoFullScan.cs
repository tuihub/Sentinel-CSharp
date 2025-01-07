﻿using Microsoft.Extensions.Logging;
using Python.Runtime;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using Sentinel.Plugin.Results;
using System.Text.Json;

namespace Sentinel.Plugin.PythonPluginLoader
{
    public partial class PythonPluginLoader : IPlugin
    {
        public Task<ScanChangeResult> DoFullScanAsync(IQueryable<AppBinary> appBinaries, CancellationToken ct = default)
        {
            using (Py.GIL())
            {
                var pyScriptPath = (Config as Config)!.PythonScriptPath;
                var pyClassName = (Config as Config)!.PythonClassName;
                using var scope = Py.CreateScope();
                scope.Import(scope.Exec(Resource.PluginBase));
                var code = File.ReadAllText(pyScriptPath);
                scope.Import(scope.Exec(code));
                _logger?.LogDebug($"Creating python class {pyClassName}.");
                using var pyClass = scope.Get(pyClassName).Invoke((Config as Config)!.ToPython());
                _logger?.LogInformation($"Invoking do_full_scan method from python script.");
                using PyObject pyReturn = pyClass.InvokeMethod("do_full_scan", JsonSerializer.Serialize(appBinaries, s_jso).ToPython());
                _logger?.LogInformation($"Converting python return to ScanChangeResult object.");
                if (pyReturn.AsManagedObject(typeof(string)) is not string resultJson)
                {
                    throw new Exception("Python script must return a string object.");
                }
                var result = JsonSerializer.Deserialize<ScanChangeResult>(resultJson, s_jso)
                    ?? throw new Exception("Python script must return a valid ScanChangeResult object.");
                return Task.FromResult(result);
            }
        }
    }
}
