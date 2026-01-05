using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WassControlSys.Models
{
    public enum PerformanceMode
    {
        General = 0,
        Gamer = 1,
        Dev = 2,
        Oficina = 3,
        Personalizado = 4
    }

    public class ApplyProfileResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}

namespace WassControlSys.Core
{
    using WassControlSys.Models;

    public class PerformanceProfileService : IPerformanceProfileService
    {
        // GUIDs estándar conocidos de planes de energía de Windows
        private const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";      // Equilibrado
        private const string HighPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";       // Alto rendimiento
        private const string PowerSaverGuid = "a1841308-3541-4fab-bc81-f71556f20b4a";     // Ahorro de energía (referencia)
        private readonly IProcessManagerService? _processManager;
        private readonly IServiceOptimizerService? _serviceOptimizer;
        private readonly ISettingsService _settings;
        private readonly ILogService? _log;

        public PerformanceProfileService(
            ISettingsService settings,
            IProcessManagerService? processManager = null, 
            IServiceOptimizerService? serviceOptimizer = null, 
            ILogService? log = null)
        {
            _settings = settings;
            _processManager = processManager;
            _serviceOptimizer = serviceOptimizer;
            _log = log;
        }

        public async Task<ApplyProfileResult> ApplyProfileAsync(PerformanceMode mode)
        {
            if (mode == PerformanceMode.General)
            {
                return await RestoreOriginalStateAsync();
            }

            var settings = await _settings.LoadAsync();
            string modeKey = mode.ToString();
            
            if (!settings.PerformanceProfiles.TryGetValue(modeKey, out var config))
            {
                config = mode switch {
                    PerformanceMode.Gamer => ProfileConfig.DefaultGamer(),
                    PerformanceMode.Dev => ProfileConfig.DefaultDev(),
                    _ => new ProfileConfig { Mode = mode }
                };
            }

            // Capturar Snapshot si es el primer perfil aplicado
            if (settings.OriginalSystemSnapshot == null)
            {
                _log?.Info("No se encontró snapshot previo. Capturando estado original del sistema...");
                settings.OriginalSystemSnapshot = new SystemSnapshot
                {
                    OriginalPowerPlan = await Task.Run(() => GetCurrentPowerPlanGuid())
                };
                await _settings.SaveAsync(settings);
            }

            var power = await Task.Run(() => TrySetPowerPlan(mode, config.PowerPlanGuid));
            
            try
            {
                // Aplicar prioridad de procesos
                if (config.ReduceBackgroundPriority && _processManager != null)
                {
                    await _processManager.ReduceBackgroundProcessesAsync(ProcessPriorityClass.BelowNormal);
                }

                // Aplicar ajustes de servicios
                if (_serviceOptimizer != null)
                {
                    var allServices = await _serviceOptimizer.GetWindowsServicesAsync();
                    bool settingsChanged = false;

                    foreach (var serviceName in config.ServicesToStop)
                    {
                        var svcInfo = allServices.FirstOrDefault(s => s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
                        if (svcInfo != null)
                        {
                            // Guardar en el snapshot persistente si no existe
                            if (!settings.OriginalSystemSnapshot.Services.ContainsKey(serviceName))
                            {
                                settings.OriginalSystemSnapshot.Services[serviceName] = new ServiceStateSnapshot
                                {
                                    Name = serviceName,
                                    StartType = svcInfo.StartType.ToString(),
                                    WasRunning = svcInfo.IsRunning
                                };
                                settingsChanged = true;
                                _log?.Info($"Persistiendo snapshot para servicio: {serviceName}");
                            }

                            await _serviceOptimizer.SetServiceStartTypeAsync(serviceName, ServiceStartType.Manual);
                            await _serviceOptimizer.StopServiceAsync(serviceName);
                        }
                    }
                    if (settingsChanged) await _settings.SaveAsync(settings);
                }

                // Ajustes de Sistema
                if (config.AutoCleanRam && _processManager != null)
                {
                    _log?.Info("Limpieza automática de RAM activada por perfil.");
                    await _processManager.OptimizeRamAsync();
                }

                if (config.OptimizeVisualEffects)
                {
                    await Task.Run(() => SetVisualEffectsPerformance(true));
                }

                if (config.PauseWindowsUpdate)
                {
                    await Task.Run(() => SetWindowsUpdateStatus(false));
                }

                if (config.DisableNetworkThrottling)
                {
                    await Task.Run(() => SetNetworkThrottling(false));
                }

                if (config.DisableTelemetry)
                {
                    await Task.Run(() => SetTelemetryStatus(false));
                }

                if (config.DisableIndexing)
                {
                    await Task.Run(() => SetIndexingStatus(false));
                }
            }
            catch (Exception ex)
            {
                _log?.Warn($"Acciones adicionales de perfil fallaron: {ex.Message}");
            }

            return power;
        }

        public async Task<ApplyProfileResult> RestoreOriginalStateAsync()
        {
            _log?.Info("Restaurando estado original del sistema...");
            
            try
            {
                var settings = await _settings.LoadAsync();
                if (settings.OriginalSystemSnapshot == null) return new ApplyProfileResult { Success = true };

                // 1. Restaurar Plan de Energía
                if (settings.OriginalSystemSnapshot.OriginalPowerPlan != null)
                {
                    TrySetPowerPlan(PerformanceMode.General, settings.OriginalSystemSnapshot.OriginalPowerPlan);
                }
 
                // 2. Restaurar Servicios
                if (_serviceOptimizer != null && settings.OriginalSystemSnapshot.Services.Count > 0)
                {
                    foreach (var state in settings.OriginalSystemSnapshot.Services.Values)
                    {
                        if (Enum.TryParse<ServiceStartType>(state.StartType, out var startType))
                        {
                            _log?.Info($"Restaurando servicio: {state.Name} a {startType}");
                            await _serviceOptimizer.SetServiceStartTypeAsync(state.Name, startType);
                            if (state.WasRunning) await _serviceOptimizer.StartServiceAsync(state.Name);
                        }
                    }
                }

                // Limpiar snapshot tras restaurar
                settings.OriginalSystemSnapshot = null;
                await _settings.SaveAsync(settings);

                return new ApplyProfileResult { Success = true, Message = "Sistema restaurado al estado original." };
            }
            catch (Exception ex)
            {
                _log?.Error("Error al restaurar el estado original", ex);
                return new ApplyProfileResult { Success = false, Message = $"Error al restaurar: {ex.Message}" };
            }
        }

        private async Task ApplyServiceAdjustmentsAsync(PerformanceMode mode)
        {
            if (_serviceOptimizer == null) return;
            switch (mode)
            {
                case PerformanceMode.Gamer:
                    await _serviceOptimizer.SetServiceStartTypeAsync("SysMain", ServiceStartType.Manual);
                    await _serviceOptimizer.StopServiceAsync("SysMain");
                    await _serviceOptimizer.SetServiceStartTypeAsync("WSearch", ServiceStartType.Manual);
                    await _serviceOptimizer.StopServiceAsync("WSearch");
                    break;
                case PerformanceMode.Oficina:
                    await _serviceOptimizer.SetServiceStartTypeAsync("WSearch", ServiceStartType.Automatic);
                    await _serviceOptimizer.StartServiceAsync("WSearch");
                    await _serviceOptimizer.SetServiceStartTypeAsync("SysMain", ServiceStartType.Automatic);
                    break;
                case PerformanceMode.Dev:
                    await _serviceOptimizer.SetServiceStartTypeAsync("WSearch", ServiceStartType.Manual);
                    break;
                default:
                    break;
            }
        }

        private ApplyProfileResult TrySetPowerPlan(PerformanceMode mode, string? customGuid = null)
        {
            string guid = customGuid ?? GetPowerPlanGuidForMode(mode);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c powercfg -setactive {guid}",
                    UseShellExecute = true,
                    Verb = "runas", // eleva UAC
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process? p = Process.Start(psi);
                if (p == null)
                {
                    return new ApplyProfileResult { Success = false, Message = "No se pudo iniciar powercfg." };
                }
                return new ApplyProfileResult { Success = true, Message = $"Plan de energía aplicado: {mode}." };
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return new ApplyProfileResult { Success = false, Message = "Operación cancelada por el usuario (UAC)." };
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al aplicar el perfil: {ex.Message}" );
                return new ApplyProfileResult { Success = false, Message = $"Error al aplicar el perfil: {ex.Message}" };
            }
        }

        private static string GetCurrentPowerPlanGuid()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c powercfg -getactivescheme",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    // El output es algo como: "GUID del plan de energía: 381b4222-f694-41f0-9685-ff5bb260df2e  (Equilibrado)"
                    var match = System.Text.RegularExpressions.Regex.Match(output, @"([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})");
                    if (match.Success) return match.Groups[1].Value;
                }
            }
            catch { }
            return BalancedGuid;
        }

        private static string GetPowerPlanGuidForMode(PerformanceMode mode)
        {
            return mode switch
            {
                PerformanceMode.Gamer => HighPerfGuid,
                PerformanceMode.Dev => BalancedGuid, // se puede ajustar a Alto rendimiento si se desea
                PerformanceMode.Oficina => BalancedGuid,
                _ => BalancedGuid,
            };
        }
        private void SetVisualEffectsPerformance(bool optimize)
        {
            try
            {
                // Reg Key: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects
                // VisualFXSetting: 1 = Let Windows choose, 2 = Best performance, 3 = Best appearance
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                if (key != null) key.SetValue("VisualFXSetting", optimize ? 2 : 1, Microsoft.Win32.RegistryValueKind.DWord);
                _log?.Info($"Efectos visuales ajustados para {(optimize ? "Rendimiento" : "Apariencia")}");
            }
            catch (Exception ex) { _log?.Warn($"Error ajustando efectos visuales: {ex.Message}"); }
        }

        private void SetWindowsUpdateStatus(bool enabled)
        {
            try
            {
                // Solo un ejemplo simple, en realidad requiere detener el servicio 'wuauserv'
                if (!enabled) _serviceOptimizer?.StopServiceAsync("wuauserv");
                _log?.Info($"Estado de Windows Update ajustado: {(enabled ? "Habilitado" : "Pausado por perfil")}");
            }
            catch { }
        }

        private void SetNetworkThrottling(bool enabled)
        {
            try
            {
                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile
                // NetworkThrottlingIndex: 0xffffffff desactivado, 10 default
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true);
                if (key != null) key.SetValue("NetworkThrottlingIndex", enabled ? 10 : 0xFFFFFFFF, Microsoft.Win32.RegistryValueKind.DWord);
                _log?.Info($"Network Throttling {(enabled ? "Restaurado" : "Desactivado para Gaming")}");
            }
            catch { }
        }

        private void SetTelemetryStatus(bool enabled)
        {
            try
            {
                // Solo detenemos los servicios principales si se solicita desactivar
                if (!enabled)
                {
                    _serviceOptimizer?.StopServiceAsync("DiagTrack");
                    _serviceOptimizer?.StopServiceAsync("dmwappushservice");
                }
                _log?.Info($"Telemetría {(enabled ? "Habilitada" : "Desactivada por perfil")}");
            }
            catch { }
        }

        private void SetIndexingStatus(bool enabled)
        {
            try
            {
                if (!enabled) _serviceOptimizer?.StopServiceAsync("WSearch");
                _log?.Info($"Indexación {(enabled ? "Habilitada" : "Desactivada por perfil")}");
            }
            catch { }
        }
    }
}
