using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32; // Para acceso al Registro
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class StartupService : IStartupService
    {
        private readonly ILogService _log;

        public StartupService(ILogService log)
        {
            _log = log;
        }

        public async Task<IEnumerable<StartupItem>> GetStartupItemsAsync()
        {
            return await Task.Run(() =>
            {
                var items = new List<StartupItem>();
                items.AddRange(GetRegistryStartupItems());
                items.AddRange(GetStartupFolderItems());
                return items;
            });
        }

        public async Task<bool> EnableStartupItemAsync(StartupItem item)
        {
            return await Task.Run(() =>
            {
                _log.Info($"Attempting to enable startup item: {item.Name} ({item.Type})");
                switch (item.Type)
                {
                    case StartupItemType.RegistryRun:
                        return SetRegistryStartupItemState(item, true);
                    case StartupItemType.StartupFolder:
                        return SetStartupFolderItemState(item, true);
                    //case StartupItemType.TaskScheduler:
                    // Pendiente de implementar
                    //    _log.Warn($"Task Scheduler item enabling not yet supported: {item.Name}");
                    //    return false;
                    default:
                        _log.Warn($"Unsupported startup item type for enabling: {item.Type}");
                        return false;
                }
            });
        }

        public async Task<bool> DisableStartupItemAsync(StartupItem item)
        {
            return await Task.Run(() =>
            {
                _log.Info($"Attempting to disable startup item: {item.Name} ({item.Type})");
                switch (item.Type)
                {
                    case StartupItemType.RegistryRun:
                        return SetRegistryStartupItemState(item, false);
                    case StartupItemType.StartupFolder:
                        return SetStartupFolderItemState(item, false);
                    //case StartupItemType.TaskScheduler:
                    // Pendiente de implementar
                    //    _log.Warn($"Task Scheduler item disabling not yet supported: {item.Name}");
                    //    return false;
                    default:
                        _log.Warn($"Unsupported startup item type for disabling: {item.Type}");
                        return false;
                }
            });
        }

        private IEnumerable<StartupItem> GetRegistryStartupItems()
        {
            var items = new List<StartupItem>();
            string[] runKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
            };

            foreach (var keyPath in runKeys)
            {
                // HKLM (Máquina Local) requiere administrador, HKCU (Usuario Actual) no
                // Leyendo de HKLM
                try
                {
                    using (var baseKey = Registry.LocalMachine.OpenSubKey(keyPath))
                    {
                        if (baseKey != null)
                        {
                            foreach (string valueName in baseKey.GetValueNames())
                            {
                                var value = baseKey.GetValue(valueName);
                                if (value is string path)
                                {
                                    items.Add(new StartupItem
                                    {
                                        Name = valueName,
                                        Path = path,
                                        IsEnabled = true, // Si está en Run, se considera habilitado
                                        Type = StartupItemType.RegistryRun
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Could not read HKLM registry key {keyPath}: {ex.Message}");
                }

                // Leyendo de HKCU
                try
                {
                    using (var baseKey = Registry.CurrentUser.OpenSubKey(keyPath))
                    {
                        if (baseKey != null)
                        {
                            foreach (string valueName in baseKey.GetValueNames())
                            {
                                var value = baseKey.GetValue(valueName);
                                if (value is string path)
                                {
                                    items.Add(new StartupItem
                                    {
                                        Name = valueName,
                                        Path = path,
                                        IsEnabled = true, // Si está en Run, se considera habilitado
                                        Type = StartupItemType.RegistryRun
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Could not read HKCU registry key {keyPath}: {ex.Message}");
                }
            }
            return items;
        }

        private bool SetRegistryStartupItemState(StartupItem item, bool enable)
        {
            // This is tricky. To "disable" a registry item without deleting it,
            // we'd typically move it to a "disabled" key or rename it.
            // Para simplificar en esta versión inicial, asumiremos que habilitar significa asegurar que está en la clave Run,
            // y deshabilitar significa eliminarlo. Esto es destructivo, por lo que se necesitaría una advertencia en la UI.
            _log.Warn($"SetRegistryStartupItemState for {item.Name} ({item.Type}): Currently not fully implemented/destructive.");
            _log.Warn("Enabling/disabling registry startup items currently involves adding/removing entries, which is a destructive action.");
            
            // Este método requeriría privilegios administrativos para modificar las claves HKLM.
            // Por ahora, devuelve falso ya que no está completamente implementado de forma segura.
            return false;
        }

        private IEnumerable<StartupItem> GetStartupFolderItems()
        {
            var items = new List<StartupItem>();
            string[] startupPaths = {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), // Inicio por usuario
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) // Inicio para todos los usuarios
            };

            foreach (var path in startupPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                        {
                            // Filtrar ejecutables, accesos directos, etc.
                            string extension = Path.GetExtension(file).ToLowerInvariant();
                            if (new[] { ".exe", ".lnk", ".bat", ".vbs", ".cmd" }.Contains(extension))
                            {
                                items.Add(new StartupItem
                                {
                                    Name = Path.GetFileNameWithoutExtension(file),
                                    Path = file,
                                    IsEnabled = true, // Si está en la carpeta y es un tipo válido, está habilitado
                                    Type = StartupItemType.StartupFolder
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Could not enumerate startup folder {path}: {ex.Message}");
                    }
                }
            }
            return items;
        }

        private bool SetStartupFolderItemState(StartupItem item, bool enable)
        {
            // Para simplificar, deshabilitar significaría moverlo fuera de la carpeta,
            // habilitar significaría moverlo de vuelta. Esto también requiere un manejo cuidadoso
            // y potencialmente privilegios administrativos si está en CommonStartup.
            _log.Warn($"SetStartupFolderItemState for {item.Name} ({item.Type}): Currently not fully implemented/destructive.");
            _log.Warn("Enabling/disabling startup folder items currently involves moving files, which is a destructive action.");
            return false;
        }
    }
}
