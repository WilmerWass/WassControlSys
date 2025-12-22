using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class WingetService : IWingetService
    {
        private readonly ILogService _log;

        public WingetService(ILogService log)
        {
            _log = log;
        }

        public async Task<IEnumerable<WingetApp>> GetUpdatableAppsAsync()
        {
            return await Task.Run(() =>
            {
                var apps = new List<WingetApp>();
                try
                {
                    var psi = new ProcessStartInfo("winget", "upgrade")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process == null) return apps;
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        string? headerLine = lines.FirstOrDefault(l => l.Contains("Id") && (l.Contains("Version") || l.Contains("Versión")));
                        if (headerLine == null) return apps;

                        int idIndex = headerLine.IndexOf("Id");
                        int versionIndex = headerLine.IndexOf("Version");
                        if (versionIndex == -1) versionIndex = headerLine.IndexOf("Versión");

                        int availableIndex = headerLine.IndexOf("Available");
                        if (availableIndex == -1) availableIndex = headerLine.IndexOf("Disponible");
                        
                        int sourceIndex = headerLine.IndexOf("Source");
                        if (sourceIndex == -1) sourceIndex = headerLine.IndexOf("Origen");

                        if (idIndex == -1 || versionIndex == -1 || availableIndex == -1 || sourceIndex == -1) return apps;
                        
                        bool headerPassed = false;
                        foreach (var line in lines)
                        {
                            if (!headerPassed)
                            {
                                if (line.Trim().StartsWith("---")) headerPassed = true;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(line)) continue;

                            string name = line.Substring(0, idIndex).Trim();
                            string id = line.Substring(idIndex, versionIndex - idIndex).Trim();
                            string version = line.Substring(versionIndex, availableIndex - versionIndex).Trim();
                            string availableVersion = line.Substring(availableIndex, sourceIndex - availableIndex).Trim();
                            string source = line.Substring(sourceIndex).Trim();

                            apps.Add(new WingetApp
                            {
                                Name = name,
                                Id = id,
                                CurrentVersion = version,
                                AvailableVersion = availableVersion,
                                Source = source
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Winget no disponible o error: {ex.Message}");
                }
                return apps;
            });
        }

        public async Task<bool> UpdateAppAsync(string appId, IProgress<(int, string)> progress)
        {
            try
            {
                progress?.Report((0, "Iniciando actualización..."));
                var psi = new ProcessStartInfo("winget", $"upgrade --id {appId} --accept-source-agreements --accept-package-agreements")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    Verb = "runas"
                };

                using (var process = new Process { StartInfo = psi })
                {
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                        {
                            _log.Info($"[Winget] {args.Data}");
                            if (args.Data.Contains("Downloading"))
                            {
                                progress?.Report((0, "Descargando..."));
                            }
                            else
                            {
                                var match = Regex.Match(args.Data, @"(\d{1,3})%");
                                if (match.Success && int.TryParse(match.Groups[1].Value, out int percentage))
                                {
                                    progress?.Report((percentage, $"Descargando... {percentage}%"));
                                }
                            }

                            if (args.Data.ToLower().Contains("installing"))
                            {
                                progress?.Report((100, "Instalando..."));
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null) _log.Error($"[Winget Error] {args.Data}");
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        progress?.Report((100, "Actualización completada."));
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error actualizando app {appId}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateAllAppsAsync()
        {
            return await Task.Run(() =>
             {
                try
                {
                    var psi = new ProcessStartInfo("winget", "upgrade --all --silent")
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = false
                    };
                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                        return process?.ExitCode == 0;
                    }
                }
                catch { return false; }
             });
        }
    }
}
