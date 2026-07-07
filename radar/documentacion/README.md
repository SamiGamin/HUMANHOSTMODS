# Documentación de Investigación de Modding — Human Host (Radar Sniffer)

Este directorio contiene la documentación técnica y las herramientas utilizadas durante el desarrollo del mod **Human Host Radar Sniffer**. Aquí se describen los hallazgos sobre la arquitectura de ensamblados del juego, los componentes clave descifrados y las mejores prácticas para futuros desarrollos en esta plataforma.

---

## 1. DLLs del Juego Investigadas

El juego *Human Host* (instalado en `F:\steamtools\steamapps\common\Human Host\`) está construido bajo el motor Unity y modulariza su lógica en varios ensamblados administrados dentro de `Human Host_Data\Managed\`. Durante esta investigación, se identificaron y analizaron tres librerías principales:

| DLL | Tamaño (Bytes) | Propósito y Contenido Principal |
| :--- | :--- | :--- |
| **`UI.dll`** | ~167 KB | Controla toda la interfaz de usuario, ventanas flotantes, diálogos e indicadores en pantalla. Aquí reside la clase de control de botines `Loot_Mgr`. |
| **`Item_Info.dll`** | ~9 KB | Define las estructuras de datos y metadatos de los ítems del juego, incluyendo la plantilla de estadísticas de ítems `Icon_Info`. |
| **`Assembly-CSharp.dll`** | ~714 KB | Contiene la lógica central del juego, comportamientos de interacción (ej. `Use_F`), la gestión del inventario global (`Item_Slot_Mgr`), y las estructuras de datos de contenedores (`Per_Owner_Info`). |

---

## 2. Clases y Componentes Clave

Mediante técnicas de reflexión sobre los ensamblados cargados en tiempo de ejecución, se extrajo la estructura detallada de las clases críticas para el funcionamiento de la interfaz de botín, menús e ítems.

### A. `Loot_Mgr` (Localizado en `UI.dll`)
Este componente administra el ciclo de vida de la ventana de botín y la visualización de los contenedores abiertos.

*   **Instancia Singleton / Estática:**
    *   `ins` (`Loot_Mgr`): Instancia activa que controla la UI.
*   **Propiedades y Campos Clave:**
    *   `_Loot_Window` (`GameObject`): Lienzo (Canvas) o ventana de UI que muestra los ítems del contenedor actual.
    *   `_NoRefreshToolTip` (`Boolean`): Flag que indica si se debe evitar actualizar la descripción emergente del botín.
    *   `_LootDotPrefab` / `_LootDotNPCPrefab` (`GameObject`): Prefabs de los puntos indicadores en pantalla que guían al jugador hacia el botín.
    *   `_DataLoot` (`Data_Loot`): Referencia a la estructura lógica del botín que se está inspeccionando.
    *   `_lootSlots` / `_currDataSlots` (`IEnumerable` / `List`): Colección de ranuras visibles en la UI.
    *   `_InPullingBackWorld` (`Boolean`): Flag de estado interno.
    *   `_OnLootWindowClosed` (`Action`): Delegado invocado al cerrar la ventana de botín.

### B. `Icon_Info` (Localizado en `Item_Info.dll`)
Actúa como la base de datos de características para cada tipo de ítem en el juego. Contiene atributos de daño, durabilidad, tipo de munición y apilamiento.

*   **Propiedades y Campos Clave:**
    *   `_Can_Stack` (`Boolean`): Determina si el ítem es apilable.
    *   `MaxStack` (`Int32`): Cantidad máxima que se puede apilar en una sola ranura.
    *   `_BaseMaxDurability` (`Single`): Durabilidad máxima inicial del ítem.
    *   `_Can_Repair` (`Boolean`): Indica si el ítem admite reparación.
    *   `_SlotType` / `_Tag` / `_Tags` (`Enum` / `String` / `String[]`): Clasificación del ítem para equipamiento y comportamiento en el inventario.
    *   `_BuySellValue` (`Single` / `Int32`): Valor comercial con mercaderes.
    *   `_ToolTipText` (`String`): Descripción textual mostrada al usuario en pantalla.
    *   `Icon` (`Sprite`): Icono gráfico asociado.
    *   `ModelRef` (`GameObject`): Prefab 3D del ítem en el mundo.
    *   *Estadísticas de Combate / Herramientas:*
        *   `_baseDamage` (`Single`): Daño básico del ítem.
        *   `_maxMagCount` (`Int32`): Capacidad del cargador (si es un arma de fuego).
        *   `_ammoType` (`String` / `Enum`): Tipo de munición compatible.
        *   `_fireRate` (`Single`): Cadencia de disparo o velocidad de ataque.
        *   `_DuraCostPerAttack` (`Single`): Consumo de durabilidad por cada uso.

### C. `Item_Slot_Mgr` (Localizado en `Assembly-CSharp.dll`)
Es el gestor del inventario del jugador y el coordinador de las interfaces activas (menús).

*   **Instancia Singleton / Estática:**
    *   `Ins` (`Item_Slot_Mgr`): Punto de acceso estático global a la instancia del gestor de inventario.
*   **Propiedades y Campos Clave:**
    *   `_lastEnableMenus` (`IEnumerable` / `List`): Campo privado que mantiene la lista de todos los paneles de interfaz de usuario abiertos en ese momento (ej. inventario, cofre, horno, mesa de crafteo). El Sniffer de Radar intercepta este campo para listar las interfaces activas en consola al pulsar `TAB`.

### D. `Per_Owner_Info` (Localizado en `Assembly-CSharp.dll`)
Clase interna que representa la información de propiedad y contenido específico de un contenedor físico en el mundo del juego.

*   **Propiedades y Campos Clave:**
    *   `_dataSlots` (`Dictionary<int, Data_Slot>`): Diccionario lógico que asocia un índice de ranura con un objeto `Data_Slot`, que almacena qué ítem está en cada compartimento.
    *   `_spawnedThisLootTimeP` (`Single`): Probabilidad o tiempo de generación de botín calculado para el contenedor.
    *   `_containerType` (`Per_Owner_Info+ContainerType`): Enumerador que cataloga si es un cofre, armario, mochila, etc.
    *   `_grid` (`Per_Owner_Info+Grid`): Configuración de dimensiones de rejilla para la UI del contenedor.
    *   `_crateLabel` (`String`): Nombre o etiqueta visible del contenedor en el mundo (ej. "Cofre de Madera").

### E. `UI_Control` (Localizado en `UI.dll`)
Es el controlador central de las interfaces de usuarioHUD generales que no se basan en cuadrículas de inventario (como el mapa de pantalla completa, menús de juego, etc.).

*   **Instancia Singleton / Estática:**
    *   `_ins` (`UI_Control`): Instancia activa que gestiona las interfaces de pantalla completa y del sistema.
*   **Propiedades y Campos Clave:**
    *   `OnShowingUIs` (`List<GameObject>`): Campo privado que almacena los paneles HUD generales activos. Al examinar este campo, el mod captura estados de interfaz global como la apertura del **Mapa** o configuraciones.

---

## 3. Técnicas de Reflexión Utilizadas

Debido a que muchas de estas clases exponen miembros privados o se encuentran en ensamblados separados, el plugin del mod implementa técnicas avanzadas de reflexión en C# para garantizar robustez y rendimiento:

1.  **Resolución Dinámica de Tipos (`FindType`):**
    En lugar de acoplar estáticamente el código a las clases del juego (lo que podría romper el mod ante actualizaciones), se busca el tipo recorriendo los ensamblados cargados en el `AppDomain` actual:
    ```csharp
    Type t = Type.GetType("Item_Slot_Mgr");
    // O buscando a través de los ensamblados del dominio...
    ```
2.  **Caché de Reflexión:**
    Para evitar caídas de fotogramas (*lag spikes*) en el ciclo `Update` de Unity, el plugin resuelve las referencias a `FieldInfo` y `PropertyInfo` una sola vez y las almacena en memoria (`cachedItemSlotMgrType`, `cachedLastEnableMenusField`), en lugar de buscarlas repetidamente en cada frame.
3.  **Flags de Acceso Total:**
    Para leer miembros privados, protegidos o internos, se define una combinación de `BindingFlags`:
    ```csharp
    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    ```

---

## 4. Lecciones Aprendidas para Futuros Mods

1.  **Separación de Lógica y Datos:** El diseño del juego mantiene los datos crudos separados (`Item_Info.dll`) de la lógica interactiva (`Assembly-CSharp.dll`) y visual (`UI.dll`). Al desarrollar mods que añadan nuevos ítems, se debe registrar la estructura en `Item_Info` antes de intentar manipular su interfaz.
2.  **Rendimiento en BepInEx:** El uso irresponsable de métodos de reflexión recursivos (como `GetTypes()`) dentro de métodos de ejecución frecuente degrada drásticamente la tasa de FPS de Unity. La optimización del caché estático es mandatoria en mods listos para producción.
3.  **Inspección No Invasiva:** Al interceptar físicas (como `Raycast` desde la cámara central al pulsar `F`), es crucial evaluar únicamente los componentes que no heredan de los espacios de nombres nativos de Unity (`UnityEngine.*`) para evitar saturar el flujo de logs.

---

## 5. Instrucciones de Uso de la Documentación

### Ejecución del Script de PowerShell (`consultas.ps1`)
El archivo [consultas.ps1](file:///D:/mods/MiMods/radar/documentacion/consultas.ps1) permite realizar búsquedas dinámicas en las DLLs sin necesidad de iniciar el juego o compilar código C#.

1.  Abra una terminal de PowerShell.
2.  Navegue al directorio de documentación:
    ```powershell
    cd D:\mods\MiMods\radar\documentacion
    ```
3.  Ejecute el script (asegúrese de tener permisos de ejecución ajustados con `Set-ExecutionPolicy` si es necesario):
    ```powershell
    .\consultas.ps1
    ```
4.  Utilice los comandos integrados en la terminal de PowerShell cargada, por ejemplo:
    *   Inspeccionar a fondo la clase `Loot_Mgr`:
        ```powershell
        Get-GameTypeDetails -TypeName "Loot_Mgr"
        ```
    *   Buscar todas las clases que contengan la palabra "Owner":
        ```powershell
        Get-GameTypesMatching -Pattern "Owner"
        ```
