using System.Collections.Generic;

namespace WassControlSys.Models
{
    public class ProfileConfig
    {
        public PerformanceMode Mode { get; set; }
        
        // Servicios
        public List<string> ServicesToStop { get; set; } = new List<string>();
        public bool DisableTelemetry { get; set; } = false;
        public bool DisableIndexing { get; set; } = false;
        public bool PauseWindowsUpdate { get; set; } = false;
        
        // Procesos y RAM
        public bool ReduceBackgroundPriority { get; set; } = false;
        public bool AutoCleanRam { get; set; } = false;
        public List<string> ProcessesToKill { get; set; } = new List<string>();
        
        // Sistema
        public bool OptimizeVisualEffects { get; set; } = false; // Ajustar para rendimiento
        public bool DisableNetworkThrottling { get; set; } = false; // Para gaming (Multimedia Class Scheduler)
        
        // Energía
        public string PowerPlanGuid { get; set; } = "381b4222-f694-41f0-9685-ff5bb260df2e"; // Equilibrado

        // Auto-Boost (Nombres de ejecutables que activan este perfil)
        public List<string> AutoBoostProcesses { get; set; } = new List<string>();

        public static ProfileConfig DefaultGamer() => new ProfileConfig
        {
            Mode = PerformanceMode.Gamer,
            ServicesToStop = new List<string> { "SysMain", "WSearch", "TabletInputService", "PrintNotify" },
            ReduceBackgroundPriority = true,
            DisableTelemetry = true,
            DisableIndexing = true,
            PauseWindowsUpdate = true,
            AutoCleanRam = true,
            OptimizeVisualEffects = true,
            DisableNetworkThrottling = true,
            PowerPlanGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" // Alto rendimiento
        };

        public static ProfileConfig DefaultDev() => new ProfileConfig
        {
            Mode = PerformanceMode.Dev,
            ServicesToStop = new List<string> { "WSearch", "Spooler" },
            ReduceBackgroundPriority = false,
            DisableTelemetry = true,
            DisableIndexing = false,
            AutoCleanRam = false,
            OptimizeVisualEffects = false,
            PowerPlanGuid = "381b4222-f694-41f0-9685-ff5bb260df2e" // Equilibrado
        };

        public static ProfileConfig DefaultOficina() => new ProfileConfig
        {
            Mode = PerformanceMode.Oficina,
            ServicesToStop = new List<string>(),
            ReduceBackgroundPriority = false,
            DisableTelemetry = false,
            DisableIndexing = false,
            PauseWindowsUpdate = false,
            AutoCleanRam = false,
            OptimizeVisualEffects = false,
            PowerPlanGuid = "381b4222-f694-41f0-9685-ff5bb260df2e"
        };
    }
}
