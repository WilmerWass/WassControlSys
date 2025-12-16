using System;
using System.Management;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public class SecurityStatus
    {
        public string AntivirusName { get; set; } = "Desconocido";
        public bool IsAntivirusEnabled { get; set; } = false;
        public bool IsFirewallEnabled { get; set; } = false;
        public bool IsUacEnabled { get; set; } = false;
        
        public string OverallStatus => (IsAntivirusEnabled && IsFirewallEnabled && IsUacEnabled) ? "Seguro" : "Riesgo";
    }

    public class SecurityService : ISecurityService
    {
        public async Task<SecurityStatus> GetSecurityStatusAsync()
        {
            return await Task.Run(() =>
            {
                var status = new SecurityStatus
                {
                    IsAntivirusEnabled = CheckAntivirus(out string avName),
                    AntivirusName = avName,
                    IsFirewallEnabled = CheckFirewall(),
                    IsUacEnabled = CheckUac()
                };
                return status;
            });
        }

        private bool CheckAntivirus(out string name)
        {
            name = "No detectado";
            bool enabled = false;
            try
            {
                // Consulta WMI a SecurityCenter2
                using var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct");
                foreach (var result in searcher.Get())
                {
                    name = result["displayName"]?.ToString() ?? "Desconocido";
                    
                    // productState es una máscara de bits. 
                    // Normalmente, si el bit 12 (0x1000) está activado, está ENCENDIDO. 
                    // Simplemente verificar si existe es un buen primer paso, pero intentemos analizar el estado.
                    // Nota: Esto es una simplificación.
                    string hexState = "";
                    if (result["productState"] != null)
                    {
                        int state = Convert.ToInt32(result["productState"]);
                        // Verificación estándar de "Activado" (heurística)
                        // 0x1000 = Activado, 0x0000 = Desactivado. 
                        // Pero diferentes proveedores usan diferentes banderas. 
                                            // Asumiremos que si se reporta CUALQUIER AV aquí, es probable que sea el activo.
                                            // Podría ser necesaria una verificación más robusta para proveedores específicos.                        enabled = true; 
                        hexState = state.ToString("X");
                    }
                    
                    // Simplemente tomar el primero encontrado
                    break;
                }
            }
            catch 
            {
                // Problema de respaldo o de permisos
            }
            return enabled;
        }

        private bool CheckFirewall()
        {
            // Verificación simple a través del Registro o WMI. 
            // Usar WMI "root\StandardCimv2" -> MSFT_NetFirewallProfile es más limpio pero requiere versiones más recientes de Windows.
            // Nos ceñiremos al Registro para una compatibilidad o suposiciones más amplias.
            // La verificación de perfiles de Dominio, Público, Estándar en el registro es compleja.
            // Una heurística más simple: Verificar si el servicio 'MpsSvc' (Firewall de Windows) está en ejecución.
            try
            {
                 using var sc = new System.ServiceProcess.ServiceController("MpsSvc");
                 return sc.Status == System.ServiceProcess.ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckUac()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false);
                if (key != null)
                {
                    var val = key.GetValue("EnableLUA");
                    if (val is int i) return i == 1;
                }
            }
            catch { }
            return false;
        }
    }
}
