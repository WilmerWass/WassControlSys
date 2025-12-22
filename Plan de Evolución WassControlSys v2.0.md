# Plan de Evolución WassControlSys v2.0

## Visión General
El proyecto se dividirá en dos capas funcionales dentro del mismo ejecutable. La app detectará automáticamente si el usuario tiene activado el "AI-Module".

---

## 🏗️ Ruta de Planificación: Las Dos Versiones

### 1. WassControlSys "Core" (Gratis y Código Abierto)
Es el motor de diagnóstico que ya tenemos. Ligero, rápido y mantenido por la comunidad. La arquitectura actual basada en servicios (`Core/`), modelos (`Models/`) y vistas (`Views/`) es la base perfecta para esta versión.

**Funciones incluidas:**
*   Lectura completa de hardware (`SystemInfoService`, `HardwareView`).
*   Monitor de procesos y consumo de recursos (`ProcessManagerService`, `RendimientoView`).
*   Limpieza de archivos y mantenimiento del sistema (`SystemMaintenanceService`, `MantenimientoView`).
*   Gestión de aplicaciones, actualizaciones y bloatware (`WingetService`, `BloatwareService`, `AplicacionesView`).
*   Optimización de servicios y arranque del sistema (`ServiceOptimizerService`, `StartupService`).
*   Análisis de seguridad y privacidad (`SecurityService`, `PrivacyService`).

**Filosofía:** "Transparencia total, privacidad y control para el usuario".

### 2. WassControlSys "AI-Edition" (Premium / Contribución)
Será el cerebro de la aplicación. Utilizará un modelo de IA local (como Llama 3 o Phi-3) para razonar sobre los datos recolectados por la capa "Core" y ofrecer asistencia proactiva.

**Funciones Exclusivas Propuestas:**
*   **Asistente de Upgrades:** Basado en los datos de `SystemInfoService`, recomendará componentes exactos de hardware compatibles (CPU, RAM, SSD) para mejorar el rendimiento.
*   **Predictor de Fallos:** Analizará los datos de `DiskHealthService` y `TemperatureMonitorService` para advertir si un componente podría fallar a corto plazo.
*   **Chat Técnico Offline:** Un chatbot para resolver dudas sobre errores de Windows o del propio PC, utilizando el conocimiento del sistema que ya tiene la aplicación.
*   **Optimizador Inteligente:** Usará los datos de `ProcessManagerService` y `MonitoringService` para sugerir qué servicios desactivar o qué perfil de rendimiento aplicar según el patrón de uso del usuario (Gaming, Oficina, Edición).

---

## 💰 Estrategia de Acceso (Monetización Sugerida)
Para activar el "AI-Module", se podría implementar una sección de "Activar IA" con varios modelos de acceso:

1.  **Contribución GitHub (Sponsors):** Los patrocinadores del proyecto en GitHub reciben una licencia de por vida.
2.  **Pago Único (Lifetime Deal):** Un pago accesible (ej. $5 - $10 USD) para desbloquear permanentemente las funciones de IA.
3.  **Modelo "Ads-to-Unlock" (Opcional):** Permitir a los usuarios ver un anuncio de video para desbloquear las funciones de IA por 24 horas.

---

## 🛠️ Roadmap Técnico Adaptado

### Fase 1: Integración de la Interfaz de Usuario (UI)
**Objetivo:** Hacer visible el nuevo módulo "Asesor IA" en la aplicación, preparando la estructura para las funcionalidades futuras.

**Acciones:**
1.  **Validar la Arquitectura Actual:** La estructura del proyecto con carpetas `Core`, `ViewModels` y `Views` es modular y está lista para la expansión. No se necesita una reestructuración.
2.  **Crear la Nueva Sección en el Modelo:**
    *   Añadir la sección `Advisor` al enumerador `AppSection` en el archivo `Models/AppSection.cs`.
3.  **Crear la Vista Platzhalter:**
    *   Crear los archivos `Views/AdvisorView.xaml` y `Views/AdvisorView.xaml.cs`.
    *   En el XAML, diseñar una interfaz que presente las futuras funcionalidades de IA (Asistente de Upgrades, Predictor de Fallos, etc.) con un indicador de "Próximamente" o un ícono de candado (🔒).
4.  **Integrar en la Navegación Principal:**
    *   En `MainWindow.xaml`, añadir el nuevo `<views:AdvisorView>` junto a las otras vistas y un `<Button>` en el menú de navegación con el texto "Asesor IA" que apunte a la sección `Advisor`.
    *   En `ViewModels/MainViewModel.cs`, añadir el `case AppSection.Advisor:` en el método `ExecuteNavigate` para gestionar la navegación a la nueva vista.

**Resultado Esperado:** La aplicación tendrá un nuevo apartado "Asesor IA" visible en el menú, pero aún no funcional. Esto establece el "escaparate" para el desarrollo futuro.

### Fase 2: Integración de IA Local
**Objetivo:** Dotar a la aplicación de capacidad de razonamiento local.

**Acciones:**
1.  **Implementar LLamaSharp:** Integrar la librería en el proyecto de C#.
2.  **Sistema de Descarga del Modelo:** Crear una rutina que descargue automáticamente el modelo de IA (en formato GGUF) la primera vez que se acceda a una función IA. Esto mantiene el instalador inicial ligero.
3.  **Crear el Primer "System Prompt":** Programar el prompt inicial que instruirá a la IA sobre cómo interpretar los datos del sistema. Por ejemplo, un prompt para el "Optimizador Inteligente" que reciba datos de uso de CPU/RAM y una lista de servicios.

### Fase 3: Lanzamiento y Feedback
**Objetivo:** Publicar la primera versión con IA y recoger opiniones.

**Acciones:**
1.  **Lanzar Beta en GitHub:** Publicar una versión preliminar para que la comunidad pueda probarla.
2.  **Invitar a Colaboradores:** Ofrecer licencias gratuitas a los primeros usuarios que prueben las funciones de IA y reporten errores o sugerencias.
3.  **Iterar:** Mejorar las funcionalidades basadas en el feedback recibido.

---

## 📝 Ejemplo de Texto para tu GitHub (Sección README)

### 🌟 Elige tu versión

*   **WassControlSys Standard (Gratis y de Código Abierto)**
    *   Diagnóstico completo de hardware y monitoreo en tiempo real.
    *   Limpieza del sistema, gestión de apps y optimizador de servicios.
    *   100% impulsado por la comunidad y enfocado en la privacidad.

*   **WassControlSys AI-Edition (Premium)**
    *   **Actualizaciones Inteligentes de Hardware:** Recibe consejos de la IA sobre qué componentes comprar para mejorar tu PC.
    *   **Predicción de Fallos:** Deja que la IA analice la salud del sistema para prevenir problemas antes de que ocurran.
    *   **Asistente Técnico:** Un chat sin conexión para ayudarte a arreglar errores de Windows usando los datos de tu propio sistema.
    *   Desbloquéalo apoyándonos en **GitHub Sponsors** o con una **donación única**.