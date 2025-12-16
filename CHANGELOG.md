# Changelog

Todas las mejoras notables de este proyecto serán documentadas en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [0.2.0] - 2025-12-16

### Añadido

- **Módulo de Información del Sistema**: Ahora muestra versión de BIOS, información detallada de red (IP, adaptador) y tiempo de actividad (uptime) del sistema.
- **Optimizador de Sistema**:
  - Limpieza de caché DNS (`flushdns`).
  - Análisis de fragmentación de disco (`defrag /A`).
  - Limpieza de carpeta Prefetch con manejo de permisos.
  - Reinicio de configuración de red (`winsock reset`, `int ip reset`).
  - Reconstrucción de íncide de búsqueda de Windows.
- **Desinstalador de Bloatware**:
  - Detección mejorada escaneando claves de registro de usuario (HKCU) y sistema (HKLM).
  - Nueva heurística para filtrar aplicaciones críticas vs. aplicaciones seguras de eliminar.
- **Interfaz Gráfica**:
  - Nuevos indicadores de carga y mensajes de estado en la barra inferior.
  - Mejoras en la navegación y consistencia visual (Roboto, iconos).
- **Core**:
  - Implementación de métodos asíncronos reales para todas las tareas de mantenimiento.
  - `SystemInfoService` expandido para mayor detalle de hardware.
  - `BloatwareService` refactorizado para mayor seguridad.

### Cambiado

- **MainViewModel**: Refactorización masiva para eliminar código duplicado y centralizar la lógica de ejecución en servicios.
- **App.xaml**: Corrección crítica en la definición de `ResourceDictionary` que causaba cierres inesperados (`XamlParseException`).
- **Licencia**: Confirmación de licencia MIT para el proyecto.

## [0.1.1] - 2025-12-15

### Añadido

- Estructura base del proyecto WPF con MVVM.
- Inyección de dependencias (`Microsoft.Extensions.DependencyInjection`).
- Vistas básicas: Dashboard, Limpieza, Optimización, Servicios.
- Sistema de temas y localización (Español/Inglés).
- Control de ventana personalizado (Chrome).

### Corregido

- Error inicial de compilación por falta de etiquetas de cierre en XAML.
