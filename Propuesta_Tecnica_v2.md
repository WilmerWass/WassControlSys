# Propuesta Técnica: WassControlSys v2.0 (Arquitectura Híbrida)

He analizado tu visión y mi propuesta es manejar **una sola aplicación inteligente** que se transforme según el estado del usuario. Esto evita mantener dos códigos distintos y permite una transición fluida.

---

## 1. Estrategia de "Peso Ligero" (On-Demand)

Para resolver el problema del peso de los modelos de IA (que pueden pesar de 2GB a 5GB):

- **Instalador Base (Core):** Pesará lo mismo que ahora (~20MB). No incluye modelos.
- **Activación IA:** Cuando el usuario activa la IA (por pago o anuncio), la app descarga el modelo automáticamente a `%AppData%/WassControlSys/Models/`.
- **Beneficio:** Los usuarios que solo quieren el "Core" nunca descargan gigas innecesarios.

---

## 2. Sistema de Monetización y Anuncios

Usaremos un sistema de **"Créditos de Tiempo"** o **"Estado Premium"**.

### Ubicación de Anuncios (Spaces):

- **Barra Lateral (Inferior):** Un banner pequeño y elegante debajo del menú de navegación.
- **Secciones Vacías:**
  - **Mantenimiento:** Al final del ScrollViewer.
  - **Dashboard:** En la columna derecha si no hay batería presente.
- **Muro de IA:** Si el usuario entra a "Asesor IA" sin licencia, verá un botón: _"Ver anuncio para desbloquear 24h"_.

**Herramienta sugerida:** `Microsoft.Web.WebView2` (para cargar banners web ligeros o Google AdSense/AdMob).

---

## 3. Implementación de IA (El Cerebro)

Usaremos **LLamaSharp** + **Phi-3 Mini (3.8B - GGUF)**. Es el modelo más equilibrado: muy inteligente, entiende español y corre fluido en PCs con 8GB de RAM.

| Acción de IA          | Herramientas / Datos Utilizados                                          |
| :-------------------- | :----------------------------------------------------------------------- |
| **Upgrade Advisor**   | `SystemInfoService` (CPU/Socket) + Web Scraping (opcional) para precios. |
| **Prediction Engine** | `DiskHealthService` (SMART) + `TemperatureMonitor` (historial térmico).  |
| **Chat Técnico**      | Contexto local del PC + Base de conocimientos de errores de Windows.     |

---

## 4. Hoja de Ruta de Herramientas

- **IA:** `LLamaSharp` (C# library) + Modelos GGUF.
- **Descargas:** `System.Net.Http` para bajar el modelo con barra de progreso.
- **Anuncios:** `WebView2` o un custom `ImageControl` que apunte a un servidor de banners.
- **Persistencia:** `SettingsService` para guardar la fecha de expiración del "Modo IA 24h".

---

## 5. Ejemplo de Arquitectura de Carpetas v2.0

```text
WassControlSys/
├── Core/
│   ├── AIService.cs (Nuevo: Gestiona LLamaSharp)
│   ├── AdService.cs (Nuevo: Gestiona carga de anuncios)
│   └── DownloadService.cs (Nuevo: Descarga modelos)
├── Models/
│   └── AIState.cs (Modo: Locked, Trial, Lifetime)
└── Views/
    └── AdvisorView.xaml (El hub de la IA)
```

¿Qué te parece este esquema técnico? Si estás de acuerdo, el primer paso real sería preparar la **AdvisorView** con el espacio para los anuncios y el botón de activación.
