using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32; // For Registry access
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class BloatwareService : IBloatwareService
    {
        private readonly ILogService _log;
        private readonly IDialogService _dialogService; // Para advertencias/confirmaciones

        public BloatwareService(ILogService log, IDialogService dialogService)
        {
            _log = log;
            _dialogService = dialogService;
        }

        public async Task<IEnumerable<BloatwareApp>> GetBloatwareAppsAsync()
        {
            return await Task.Run(() =>
            {
                var bloatwareList = new List<BloatwareApp>();

                // Rutas de registro tanto de Máquina Local (HKLM) como de Usuario Actual (HKCU)
                var rootKeys = new[] { Registry.LocalMachine, Registry.CurrentUser };
                var uninstallPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var rootKey in rootKeys)
                {
                    foreach (var uninstallPath in uninstallPaths)
                    {
                        using (RegistryKey baseKey = rootKey.OpenSubKey(uninstallPath))
                        {
                            if (baseKey == null) continue;

                            foreach (string subKeyName in baseKey.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = baseKey.OpenSubKey(subKeyName))
                                {
                                    if (subKey == null) continue;

                                    var appName = subKey.GetValue("DisplayName")?.ToString();
                                    var publisher = subKey.GetValue("Publisher")?.ToString();
                                    var installLocation = subKey.GetValue("InstallLocation")?.ToString();
                                    var uninstallString = subKey.GetValue("UninstallString")?.ToString();

                                    if (!string.IsNullOrEmpty(appName) && !string.IsNullOrEmpty(uninstallString))
                                    {
                                        // RELAJAR FILTRADO: Si la heurística es muy estricta, no saldrá nada
                                        // Por ahora, incluiremos MÁS apps para probar, pero marcaremos si es sospechoso
                                        
                                        // bool isBloat = IsPotentialBloatware(appName, publisher);
                                        // Para que el usuario vea ALGO, vamos a listar todo lo que no sea esencial del sistema
                                        // y dejaremos que el usuario decida (con cuidado).
                                        
                                        // Filtrado básico de seguridad (no listar drivers ni updates de seguridad críticos)
                                        if (!IsCriticalSystemComponent(appName, publisher)) 
                                        {
                                            bloatwareList.Add(new BloatwareApp
                                            {
                                                Name = appName,
                                                Publisher = publisher,
                                                InstallLocation = installLocation,
                                                UninstallCommand = uninstallString,
                                                IsSystemApp = IsWindowsSystemApp(publisher)
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Eliminar duplicados por nombre
                return bloatwareList.GroupBy(x => x.Name).Select(g => g.First()).OrderBy(a => a.Name);
            });
        }

        public async Task<bool> UninstallBloatwareAppAsync(BloatwareApp app)
        {
            if (app == null || string.IsNullOrEmpty(app.UninstallCommand))
            {
                _log.Warn("Attempted to uninstall a null or invalid bloatware app.");
                return false;
            }

            // Siempre pedir confirmación antes de desinstalar
            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea desinstalar '{app.Name}'? Esta acción no se puede deshacer.", "Confirmar Desinstalación");
            if (!confirm) return false;

            try
            {
                _log.Info($"Attempting to uninstall bloatware app: {app.Name} with command: {app.UninstallCommand}");
                
                // Los comandos de desinstalación pueden variar mucho (msiexec, setup.exe, EXEs personalizados)
                // Este es un intento básico para manejar casos comunes.
                // Podría ser necesario un análisis y ejecución más robustos.
                ProcessStartInfo psi;
                if (app.UninstallCommand.StartsWith("MsiExec.exe", StringComparison.OrdinalIgnoreCase))
                {
                    psi = new ProcessStartInfo("msiexec.exe", app.UninstallCommand.Replace("MsiExec.exe", "").Trim())
                    {
                        UseShellExecute = true,
                        Verb = "runas" // Solicitar elevación de UAC
                    };
                }
                else
                {
                    // Intentar parsear el comando y los argumentos
                    string command = app.UninstallCommand;
                    string arguments = "";

                    if (command.Contains(" "))
                    {
                        arguments = command.Substring(command.IndexOf(" ") + 1);
                        command = command.Substring(0, command.IndexOf(" "));
                    }

                    psi = new ProcessStartInfo(command, arguments)
                    {
                        UseShellExecute = true,
                        Verb = "runas" // Solicitar elevación de UAC
                    };
                }

                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        _log.Error($"Failed to start uninstall process for {app.Name}");
                        await _dialogService.ShowMessage($"No se pudo iniciar el proceso de desinstalación para '{app.Name}'.", "Error de Desinstalación");
                        return false;
                    }
                    await Task.Run(() => process.WaitForExit()); // Esperar a que el proceso de desinstalación se complete
                    _log.Info($"Uninstall process for {app.Name} exited with code {process.ExitCode}");
                    
                    if (process.ExitCode == 0) // Típicamente 0 significa éxito
                    {
                         await _dialogService.ShowMessage($"'{app.Name}' desinstalado correctamente. Es posible que necesite reiniciar el sistema.", "Desinstalación Completa");
                        return true;
                    }
                    else
                    {
                         await _dialogService.ShowMessage($"La desinstalación de '{app.Name}' falló con código {process.ExitCode}.", "Error de Desinstalación");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error uninstalling bloatware app {app.Name}", ex);
                await _dialogService.ShowMessage($"Error al desinstalar '{app.Name}': {ex.Message}", "Error de Desinstalación");
                return false;
            }
        }

        private bool IsCriticalSystemComponent(string appName, string publisher)
        {
            if (string.IsNullOrEmpty(appName)) return true; // Skip unknown

            // Exclusiones críticas (Drivers, .NET, C++, Windows Updates)
            if (appName.Contains("Microsoft Visual C++", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains(".NET", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Driver", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("AMD", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Realtek", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Update for Windows", StringComparison.OrdinalIgnoreCase)) return true;
            
            return false; 
        }

        private bool IsWindowsSystemApp(string publisher)
        {
            if (string.IsNullOrEmpty(publisher)) return false;
            return publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) || publisher.Contains("Windows", StringComparison.OrdinalIgnoreCase);
        }
    }
}
