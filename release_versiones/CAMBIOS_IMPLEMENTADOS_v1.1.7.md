# Cambios Técnicos Implementados - Versión 1.1.7

## Optimización de UI y UX para Modos de Rendimiento

Se ha realizado una revisión completa de la experiencia de usuario al interactuar con los modos de rendimiento (Gamer, Dev, Oficina) y el editor de perfiles.

### 1. Sistema de Feedback Visual (Overlays)

- **Objetivo**: Proporcionar retroalimentación inmediata al usuario durante operaciones asíncronas que pueden tardar varios segundos (aplicar perfiles, guardar configuraciones).
- **Implementación**:
  - Se han añadido controles `Grid` superpuestos (overlays) en `DashboardView.xaml` y `ProfileEditorView.xaml`.
  - Estos overlays contienen un fondo semitransparente (`#AA000000`), un indicador de carga (`LoadingSpinner`) y un mensaje de estado.
  - La visibilidad se controla mediante DataBinding a la propiedad `IsBusy` del ViewModel, utilizando un `BooleanToVisibilityConverter`.

### 2. Gestión de Estados en ViewModels

- **MainViewModel.cs**:
  - Actualizado `ExecuteApplyModeAsync` para gestionar el flag `IsBusy`.
  - Se han insertado `Task.Delay(400)` estratégicos para asegurar que las animaciones de carga sean perceptibles y fluidas, evitando parpadeos en operaciones rápidas.
  - Mensajes de estado dinámicos: "Estableciendo Modo X...", "¡Optimización Aplicada!".
  
- **ProfileEditorViewModel.cs**:
  - Añadidas propiedades `IsBusy` y `StatusMessage`.
  - Actualizado `SaveChangesAsync` para bloquear la UI mientras se guarda la configuración en el archivo `settings.json`.
  - Feedback de éxito ("¡Guardado!") con un retardo de 800ms antes de cerrar el overlay.

### 3. Prevención de Race Conditions

- El uso de la propiedad `IsBusy` actúa como un bloqueo lógico (`lock`) en la interfaz. Mientras una operación está en curso, los comandos (`RelayCommand`) verifican este estado antes de ejecutarse, impidiendo que el usuario pueda lanzar múltiples peticiones simultáneas (ej. cliquear "Aplicar Gamer" cinco veces seguidas).

### 4. Actualización de Versión

- **WassControlSys.csproj**: Versión actualizada a `1.1.7`.
- **Documentación**: Actualizados `README.md`, `CHANGELOG.md` y Notas de la versión.
