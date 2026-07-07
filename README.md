# HUMANHOSTMODS - Mods para Human Host

> [!NOTE]
> Colección de mods avanzados para el videojuego de supervivencia **Human Host**, desarrollados en C# utilizando el framework **BepInEx 5** y **Harmony** para la inyección de código y parches en tiempo de ejecución. Esta suite está diseñada tanto para la depuración y desarrollo de contenido como para ofrecer una experiencia personalizada a administradores de servidores y jugadores.

---

## 🛠️ Contenido del Repositorio

El repositorio está compuesto por los siguientes **5 mods**:

* 🔍 **[DeveloperItemSpawner](#1-developeritemspawner-consola-de-spawneo-de-ítems)**: Consola gráfica (`GUI`) en caliente para la búsqueda, generación e inserción de objetos y recursos del juego.
* 🪓 **[GatheringMultiplier](#2-gatheringmultiplier-multiplicador-de-recolección)**: Multiplicador dinámico para la recolección de madera y minería de recursos básicos.
* 📦 **[MiModDeAlmacenamiento](#3-mimoddealmacenamiento-expansión-de-cofres)**: Expansión personalizada de las filas y columnas de los cofres de almacenamiento Tier 1, Tier 2 y las maletas del jugador.
* 🧮 **[StackCustomizer](#4-stackcustomizer-personalización-de-stacks)**: Ajuste global y forzado de límites de apilamiento para todo tipo de ítems.
* 📡 **[radar](#5-radar-human-host-radar--radarsniffer)**: Herramienta de depuración técnica basada en Raycast e inspección reflexiva de objetos en tiempo de ejecución.

---

## 📦 Detalle de los Mods

### 1. DeveloperItemSpawner (Consola de Spawneo de Ítems)

Este mod añade una potente consola de desarrollo gráfica integrada en la interfaz de usuario de **Human Host**, ideal para creadores de contenido, administradores o desarrolladores.

#### Características
* **Extractor de Catálogo Dinámico**: Escanea los recursos (`Addressables`) del juego en memoria y exporta todos los ítems detectados con sus identificadores de recursos internos en el archivo `BepInEx\config\items_dump.txt`.
* **Filtros de Búsqueda Activos**: Campo de texto que filtra instantáneamente los ítems mientras se escribe.
* **Traducción en Caliente (Hot-Translation)**: Convierte los nombres técnicos en inglés del juego a descripciones localizadas al español (ej. `Stone` a `Piedra`, `Wood` a `Madera`, `Bottled_Water` a `Agua Embotellada`).
* **Selector de Cantidad**: Control de entrada numérica para definir el tamaño de la pila de ítems a generar en el inventario.
* **Diseño Ajustado**: Interfaz posicionada a una altura segura (`Y = 120`) para no solapar los paneles superiores nativos y navegación fluida mediante un contenedor con barra de desplazamiento (`ScrollView`).

#### Configuración
Este mod **no genera un archivo `.cfg`**, ya que obtiene los recursos de forma dinámica y autogestionada directamente de la base de datos interna de **Human Host** al momento del escaneo.

#### Controles
* **`F10`**: Activa o desactiva la consola en pantalla (también habilita y bloquea el cursor del ratón en el juego).
* **`F9`**: Fuerza el volcado (`dump`) y exportación del catálogo completo de ítems al archivo de texto `items_dump.txt` dentro de la carpeta `BepInEx\config\`.

---

### 2. GatheringMultiplier (Multiplicador de Recolección)

Aumenta el rendimiento y la eficiencia en la obtención de suministros y materias primas al realizar actividades de recolección de recursos naturales en el mapa.

#### Características
* **Multiplicador de Madera**: Aumenta la cantidad de troncos recolectados al talar árboles.
* **Multiplicador de Minerales**: Multiplica los retornos de piedra, minerales de hierro, cobre, azufre, carbón y plantas procedentes de nodos de minería o recolección.
* **Cálculo No-Destructivo**: Solo multiplica el botín obtenido al dañar/destruir el nodo de recursos, sin alterar la durabilidad nativa de las herramientas o los propios nodos.

#### Configuración (`BepInEx\config\humanhost.gathering.multiplier.cfg`)

```ini
[Multiplicadores]

## Multiplicador de recursos al talar árboles (Ej: 3.0 = Triple de madera).
# Setting type: Single
# Default value: 3
WoodMultiplier = 3.0

## Multiplicador de piedra, plantas y minerales al picar nodos de recursos (Ej: 3.0 = Triple).
# Setting type: Single
# Default value: 3
OreMultiplier = 3.0
```

---

### 3. MiModDeAlmacenamiento (Expansión de Cofres)

Este mod te permite eludir las severas limitaciones de espacio del juego base, dándote control absoluto sobre el grid de inventario de los contenedores de almacenamiento.

#### Características
* **Modificación Independiente**: Capacidad de redimensionar el ancho y alto del inventario por separado para cada categoría de contenedor.
* **Compatibilidad de Categorías**:
  * **Cofres Tier 1**: Cofres básicos de madera construibles.
  * **Cofres Tier 2**: Cofres avanzados de metal reforzado.
  * **Maletas**: Maletas y mochilas de viaje de almacenamiento general.

#### Configuración (`BepInEx\config\humanhost.cofres.cfg`)

```ini
[Cofre Tier 1]

## Número de COLUMNAS del cofre Tier 1 (Madera). Vanilla = 5.
# Setting type: Int32
# Default value: 10
Columnas = 10

## Número de FILAS del cofre Tier 1 (Madera). Vanilla = 4.
# Setting type: Int32
# Default value: 6
Filas = 6

[Cofre Tier 2]

## Número de COLUMNAS del cofre Tier 2 (Metal). Vanilla = 5.
# Setting type: Int32
# Default value: 10
Columnas = 10

## Número de FILAS del cofre Tier 2 (Metal). Vanilla = 6.
# Setting type: Int32
# Default value: 10
Filas = 10

[Maleta]

## Número de COLUMNAS de la Maleta. Vanilla = 7.
# Setting type: Int32
# Default value: 10
Columnas = 10

## Número de FILAS de la Maleta. Vanilla = 8.
# Setting type: Int32
# Default value: 10
Filas = 10
```

---

### 4. StackCustomizer (Personalización de Stacks)

Permite alterar el comportamiento y los límites físicos con los que los ítems se apilan unos encima de otros en la interfaz del inventario del personaje.

#### Características
* **Modificador Global**: Configura un número entero exacto como el nuevo tamaño máximo para cualquier ítem apilable (ideal para servidores con recursos abundantes).
* **Opción Ilimitada**: Habilita pilas infinitas estableciendo el máximo absoluto del motor (`2.147.483.647`), haciendo innecesario volver a crear contenedores adicionales.
* **Forzar Apilamiento de Equipamiento**: Desbloquea la posibilidad de apilar herramientas, consumibles especiales o armas, rompiendo la lógica nativa no-apilable del juego.

#### Configuración (`BepInEx\config\humanhost.stack.tamano.cfg`)

```ini
[Stack]

## Tamaño máximo de stack para TODOS los ítems apilables. (Vanilla suele ser 64 o 100)
# Setting type: Int32
# Default value: 999
MaxStack = 999

## Si es true, ignora MaxStack y establece el stack al máximo absoluto (2.147.483.647).
# Setting type: Boolean
# Default value: false
StackIlimitado = false

## Si es true, fuerza que TODOS los ítems sean apilables, incluso los que el juego marca como no apilables.
# Setting type: Boolean
# Default value: false
ForzarStackeable = false
```

---

### 5. radar (Human Host Radar / RadarSniffer)

Una herramienta avanzada orientada a la ingeniería inversa y depuración en tiempo real del motor Unity sin necesidad de pausar la ejecución del juego.

#### Características
* **Raycast Tridimensional**: Lanza un haz invisible directo desde la cámara hacia la posición central del puntero hasta un rango de 10 metros, detectando colisiones físicas con cualquier `GameObject`.
* **Exploración de Componentes**: Busca de manera ascendente todos los componentes y scripts asociados al objeto impactado en la jerarquía de Unity y los muestra detalladamente.
* **Reflexión Dinámica**: Inspecciona los tipos de clases y vuelca propiedades internas de variables, nombres y valores de los scripts activos detectados.
* **Inspección de Interfaces**: Monitorea de manera asíncrona mediante corrutinas los gestores de menús del juego (`UI_Control`) y las ventanas de inventarios abiertos.

#### Configuración
Este mod **no genera un archivo `.cfg`**, ya que todas sus funciones se ejecutan en consola a través de interacción por teclas físicas directas en caliente.
Todos los mensajes de depuración, escaneo e inspección se vuelcan directamente en la consola de BepInEx en tiempo real o en el archivo de registro `BepInEx\LogOutput.log`.

#### Controles
* **`F`**: Lanza el Raycast tridimensional de detección sobre el objeto al que estás apuntando.
* **`Tab`**: Inicia un escaneo en segundo plano de las interfaces de usuario activas en pantalla.

---

## ⚙️ Guía de Instalación y Configuración

> [!IMPORTANT]
> Los mods de esta suite requieren de una instalación limpia de **BepInEx 5** de 64 bits para Unity. Asegúrate de respaldar tus partidas guardadas antes de instalar cualquier modificación.

### Paso 1: Instalación de BepInEx 5 en el juego
1. Descarga el paquete de **BepInEx 5 (versión x64)** desde su repositorio oficial en GitHub.
2. Ve a la carpeta de instalación de tu juego **Human Host** (donde se encuentra `Human Host.exe`).
   * Si usas Steam, haz clic derecho sobre el juego -> *Administrar* -> *Ver archivos locales*.
3. Copia y extrae el contenido del zip de BepInEx en esa carpeta. Deberías ver la carpeta `BepInEx`, el archivo `doorstop_config.ini` y `winhttp.dll` en la raíz.
4. Arranca el juego al menos una vez para que BepInEx inicialice su entorno y cree los subdirectorios indispensables (`config`, `plugins`, `patchers`).
5. Cierra el juego tras llegar al menú principal.

### Paso 2: Colocación de los Mods (DLLs)
1. Descarga las DLLs compiladas de los mods o realiza tu propia compilación local usando un compilador de C#/.NET.
2. Navega al directorio de plugins de BepInEx en tu instalación de **Human Host**:
   * Ruta: `[Directorio del Juego]\BepInEx\plugins\`
3. Copia y pega las siguientes librerías `.dll` según tus necesidades de juego:
   * `DeveloperItemSpawner.dll`
   * `GatheringMultiplier.dll`
   * `MiModDeAlmacenamiento.dll`
   * `StackCustomizer.dll`
   * `radar.dll`

### Paso 3: Configuración y Personalización
1. Vuelve a iniciar el juego. BepInEx detectará y cargará automáticamente los archivos `.dll`.
2. Una vez cargado el menú principal o tu partida, los archivos de configuración inicial se habrán generado.
3. Cierra el juego y dirígete al directorio de configuración:
   * Ruta: `[Directorio del Juego]\BepInEx\config\`
4. Encontrarás los archivos editables:
   * `humanhost.gathering.multiplier.cfg`
   * `humanhost.cofres.cfg`
   * `humanhost.stack.tamano.cfg`
5. Abre y edita cualquiera de estos archivos con un editor de texto plano (como Bloc de notas o VS Code) para ajustar las variables numéricas o booleanas a tu gusto.
6. Guarda el archivo e inicia el juego. ¡Los mods aplicarán las nuevas configuraciones inmediatamente!
