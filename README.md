# Performance Watcher - Monitor de Procesos en Tiempo Real

## Descripción

Performance Watcher es una aplicación de escritorio desarrollada en .NET 8.0 con WinForms que permite monitorear el rendimiento de procesos en tiempo real. La aplicación proporciona métricas detalladas de CPU, memoria y ancho de banda de red para procesos seleccionados, con visualización gráfica en tiempo real.

## Características

### 🔍 Búsqueda de Procesos
- Búsqueda de procesos por nombre
- Visualización de PIDs disponibles
- Agregación de múltiples procesos con el mismo nombre

### 📊 Métricas Monitoreadas
- **CPU**: Uso de CPU por proceso (%)
- **Memoria**: Consumo de memoria RAM (MB)
- **Red - Descarga**: Ancho de banda de descarga (KB/s)
- **Red - Subida**: Ancho de banda de subida (KB/s)

### ⚡ Rendimiento
- Refresco constante de 1 segundo (1 Hz)
- Algoritmos optimizados para mínimo impacto en CPU
- Cálculo eficiente usando APIs nativas de Windows
- Caché de datos históricos para gráficos

### 🎨 Interfaz Moderna
- Diseño moderno y limpio
- Interfaz responsive que se adapta al tamaño de ventana
- Esquema de colores profesional
- Controles intuitivos y user-friendly

### 📈 Gráficos en Tiempo Real
- Visualización de datos en tiempo real
- Ventana deslizante de tiempo (sliding window)
- Configuración flexible:
  - **Tipo de métrica**: CPU, Memoria, Red (Descarga/Subida)
  - **Modo de vista**: Por proceso individual o agregado
  - **Rango temporal**: Configurable de 10 a 3600 segundos
  - **Escala Y**: Auto-escala o valor máximo personalizado
- Leyenda con colores únicos por proceso
- Ejes claramente etiquetados

## Requisitos del Sistema

- Windows 10 o superior
- .NET 8.0 Runtime (o SDK para desarrollo)
- Mínimo 100 MB de RAM
- Permisos de usuario para leer información de procesos

## Instalación

### Opción 1: Compilar desde código fuente

1. Instalar .NET 8.0 SDK desde [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

2. Clonar el repositorio:
```bash
git clone <repository-url>
cd PerformanceWatcher
```

3. Compilar el proyecto:
```bash
dotnet build -c Release
```

4. Ejecutar la aplicación:
```bash
dotnet run
```

### Opción 2: Publicar ejecutable independiente

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El ejecutable estará en `bin/Release/net8.0-windows/win-x64/publish/`

## Uso

### 1. Buscar Procesos
- Ingresa el nombre del proceso en el campo de búsqueda
- Presiona "Search" o Enter
- Los procesos encontrados aparecerán en la lista con sus PIDs

### 2. Agregar a Monitor
- Selecciona uno o más procesos de la lista de búsqueda
- Haz clic en "Add to Monitor"
- Los procesos se agregarán a la lista de monitoreados

### 3. Visualizar Métricas
- Las métricas actuales se muestran en la tabla en la parte superior
- El gráfico muestra la evolución temporal de las métricas

### 4. Configurar Gráfico

**Tipo de Métrica**:
- CPU %: Uso de procesador
- Memory (MB): Consumo de memoria
- Network Down (KB/s): Velocidad de descarga
- Network Up (KB/s): Velocidad de subida

**Modo de Vista**:
- Per Process: Muestra cada proceso con su propia línea
- Aggregated: Suma todos los procesos en una sola línea

**Rango Temporal (Max Time)**:
- Define cuántos segundos mostrar en el eje X
- Cuando se alcanza el límite, la ventana se desliza automáticamente
- Ejemplo: Si el máximo es 500s:
  - Al segundo 500: muestra 0-500
  - Al segundo 501: muestra 1-501
  - Al segundo 502: muestra 2-502

**Escala Y**:
- Auto Scale Y: Ajusta automáticamente al valor máximo
- Max Y Value: Define un máximo fijo para el eje Y

### 5. Remover Procesos
- Selecciona un proceso de la lista de monitoreados
- Haz clic en "Remove Selected"

## Arquitectura del Código

### Componentes Principales

```
PerformanceWatcher/
├── Program.cs                 # Punto de entrada de la aplicación
├── MainForm.cs               # Lógica principal de la UI
├── MainForm.Designer.cs      # Diseño de la interfaz
├── ProcessMonitor.cs         # Monitoreo de CPU y memoria
├── NetworkMonitor.cs         # Monitoreo de red
├── MetricsCollector.cs       # Agregación y gestión de métricas
└── PerformanceWatcher.csproj # Configuración del proyecto
```

### Clases Principales

**ProcessMonitor**:
- Búsqueda de procesos por nombre
- Cálculo eficiente de uso de CPU
- Monitoreo de memoria RAM
- Gestión de múltiples PIDs

**NetworkMonitor**:
- Monitoreo de ancho de banda por proceso
- Cálculo de velocidades de subida/bajada
- Gestión de estadísticas de red

**MetricsCollector**:
- Coordinación de monitores
- Agregación de datos por nombre de proceso
- Almacenamiento de histórico (hasta 1 hora)
- Gestión de procesos activos

### Optimizaciones de Rendimiento

1. **Cálculo de CPU**: Uso de `Process.TotalProcessorTime` en lugar de PerformanceCounters para reducir overhead
2. **Caché de datos**: Histórico limitado a 3600 entradas (1 hora)
3. **Limpieza automática**: Eliminación de procesos terminados
4. **Threading**: Uso de locks mínimos para evitar bloqueos
5. **UI Updates**: Actualización incremental solo de datos cambiados

## Limitaciones Conocidas

1. **Monitoreo de Red por Proceso**:
   - Windows no proporciona APIs sencillas para ancho de banda por proceso
   - La implementación actual usa aproximaciones
   - Para mediciones precisas, considerar herramientas de nivel kernel como WinPcap o ETW

2. **Permisos**:
   - Algunos procesos del sistema requieren privilegios elevados
   - Ejecutar como administrador para acceso completo

3. **Rendimiento**:
   - Monitorear muchos procesos (>20) puede aumentar el uso de CPU
   - El histórico se limita a 1 hora para evitar consumo excesivo de memoria

## Solución de Problemas

### La aplicación no encuentra procesos
- Verifica que el nombre del proceso sea correcto
- Algunos procesos requieren permisos de administrador
- Intenta ejecutar la aplicación como administrador

### Métricas de red muestran 0
- El monitoreo de red por proceso es limitado en Windows
- Verifica que el proceso esté generando tráfico de red activamente
- Considera usar herramientas especializadas para mediciones precisas

### Alto uso de CPU de la aplicación
- Reduce el número de procesos monitoreados
- Aumenta el intervalo de refresco (requiere modificar código)
- Cierra aplicaciones en segundo plano

## Desarrollo Futuro

### Mejoras Propuestas
- [ ] Integración con ETW para monitoreo de red preciso
- [ ] Exportación de métricas a CSV/JSON
- [ ] Alertas configurables por umbrales
- [ ] Temas claro/oscuro
- [ ] Guardado/carga de configuraciones
- [ ] Soporte para monitoreo remoto
- [ ] Historial persistente en base de datos

## Contribuciones

Las contribuciones son bienvenidas. Por favor:
1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## Licencia

Este proyecto es de código abierto y está disponible bajo la licencia MIT.

## Autor

Desarrollado para monitoreo de procesos en tiempo real con .NET y WinForms.

## Agradecimientos

- .NET Team por las excelentes APIs de monitoreo
- Comunidad de WinForms por los componentes de visualización
- Microsoft.Chart.Controls por los gráficos

---

**Nota**: Esta aplicación está diseñada para propósitos educativos y de monitoreo personal. Para entornos de producción, considere herramientas empresariales especializadas.