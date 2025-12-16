using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class DiskHealthService : IDiskHealthService
    {
        public async Task<IEnumerable<DiskHealthInfo>> GetDiskHealthAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<DiskHealthInfo>();
                try
                {
                    using var drives = new System.Management.ManagementObjectSearcher("SELECT DeviceID, Model, SerialNumber FROM Win32_DiskDrive");
                    foreach (var d in drives.Get())
                    {
                        var id = d["DeviceID"]?.ToString() ?? "";
                        var model = d["Model"]?.ToString() ?? "";
                        var serial = d["SerialNumber"]?.ToString() ?? "";
                        var status = GetSmartStatus(id);
                        list.Add(new DiskHealthInfo
                        {
                            DeviceId = id,
                            Model = model,
                            Serial = serial,
                            SmartOk = status.HasValue && status.Value,
                            SmartStatusKnown = status.HasValue
                        });
                    }
                }
                catch { }
                return list;
            });
        }

        private static bool? GetSmartStatus(string deviceId)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(@"root\WMI", "SELECT PredictFailure, InstanceName FROM MSStorageDriver_FailurePredictStatus");
                foreach (var mo in searcher.Get())
                {
                    var instance = mo["InstanceName"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(deviceId) && instance.Contains(deviceId, StringComparison.OrdinalIgnoreCase))
                    {
                        var pf = mo["PredictFailure"];
                        if (pf == null) return null;
                        int v = Convert.ToInt32(pf);
                        return v == 0;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

