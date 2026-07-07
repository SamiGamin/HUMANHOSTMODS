using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.IO;

namespace DeveloperItemSpawner
{
    [BepInPlugin("com.salomon.humanhost.item.spawner", "Developer Item Spawner", "1.0.0")]
    public class PluginPrincipal : BaseUnityPlugin
    {
        private bool mostrarMenu = false;
        private string itemGUID = "";
        private string cantidadTexto = "1";
        
        // Se desplaza la consola a y = 120 (padding superior) para que no tape la barra superior del juego
        private Rect ventanaRect = new Rect(20, 120, 800, 480);

        // Variables de Estado para Control UI
        private Vector2 scrollPos = Vector2.zero;
        private string filtroBusqueda = "";

        // Catálogo dinámico cargado en memoria (Llave: GUID [hash] | Valor: Nombre Humano [legible])
        private static System.Collections.Generic.Dictionary<string, string> itemCatalog = new System.Collections.Generic.Dictionary<string, string>();
        private static bool catalogCargado = false;

        // Instancia estática para los parches de Harmony
        private static PluginPrincipal instancia;

        // Diccionario de Traducciones en Caliente
        private static readonly System.Collections.Generic.Dictionary<string, string> DiccionarioTraducciones = new System.Collections.Generic.Dictionary<string, string>
        {
            // --- RECURSOS BÁSICOS Y MINERALES ---
            { "Stone", "Piedra" },
            { "Wood", "Madera" },
            { "Sand", "Arena" },
            { "Clay", "Arcilla / Barro" },
            { "Iron_Ore", "Mineral de Hierro" },
            { "Copper_Ore", "Mineral de Cobre" },
            { "Coal", "Carbón" },
            { "Sulfur", "Azufre" },

            // --- CONSUMIBLES Y MEDICINA ---
            { "Bottled_Water", "Agua Embotellada" },
            { "Energy_Drink", "Bebida Energética" },
            { "Fruit_Juice", "Jugo de Frutas" },
            { "Soda", "Refresco / Gaseosa" },
            { "Canned_Beef", "Carne de Res en Lata" },
            { "Canned_Fruit", "Fruta en Lata" },
            { "Canned_Noodles", "Fideos en Lata" },
            { "Canned_Rice", "Arroz en Lata" },
            { "Canned_Tuna", "Atún en Lata" },
            { "Sausage", "Salchicha" },
            { "Expired_Sandwich", "Sándwich Caducado" },
            { "Antibiotics", "Antibióticos" },
            { "Medical_Bandage", "Vendaje Médico" },
            { "Medical_Case", "Maletín de Medicamentos" },
            { "Medkit", "Botiquín Avanzado" },
            { "Pain_Killer", "Analgésicos / Analgesia" },

            // --- MATERIALES DE CONSTRUCCIÓN Y CRAP ---
            { "Bone", "Hueso Recolectado" },
            { "Leather", "Cuero Animal" },
            { "Torn_Cloth", "Tela Rasgada" },
            { "Scrap_Plastic", "Chatarra de Plástico" },
            { "Scrap_Iron", "Chatarra de Hierro" },
            { "Scrap_Brass", "Chatarra de Latón" },
            { "Spring", "Resorte / Muelle Mecánico" },
            { "Gear", "Engranaje de Metal" },
            { "Nails", "Caja de Clavos" },
            { "Rope", "Cuerda Resistente" },
            { "Rubber", "Caucho / Goma" },
            { "Copper_Wire", "Cable de Cobre" },
            { "Paper", "Papel / Hojas" },
            { "Duct_Tape", "Cinta Americana (Duct Tape)" },
            { "Glue", "Pegamento Fuerte" },
            { "Feather", "Pluma" },

            // --- ARMAS Y HERRAMIENTAS ---
            { "Wooden_Spiked_Club", "Garrote de Madera con Clavos" },
            { "Wooden_Spear", "Lanza de Madera" },
            { "Wooden_Club", "Garrote de Madera Básico" },
            { "Stone_Spear", "Lanza de Piedra" },
            { "Stone_Pickaxe", "Pico de Piedra" },
            { "Stone_Knife_01", "Cuchillo de Piedra Rústico" },
            { "Stone_Axe", "Hacha de Piedra" },
            { "Machete", "Machete de Supervivencia" },
            { "Iron_Spear", "Lanza de Hierro" },
            { "Iron_Pickaxe", "Pico de Hierro Profesional" },
            { "Iron_Hammer", "Martillo de Hierro" },
            { "Iron_Axe", "Hacha de Hierro de Tala" },
            { "AKM", "Fusil de Asalto AKM" },
            { "MP5", "Subfusil MP5" },
            { "R870_01", "Escopeta Remington 870" },
            { "M1911_01", "Pistola Colt M1911" },

            // --- MUNICIÓN ---
            { "9x19_AmmoBox", "Caja de Balas 9x19mm" },
            { "7.62x39_AmmoBox", "Caja de Balas 7.62x39mm (AK)" },
            { "5.56x45_AmmoBox", "Caja de Balas 5.56x45mm" },
            { "12x70mm_AmmoBox", "Caja de Cartuchos de Escopeta 12g" }
        };

        private void Awake()
        {
            instancia = this;
            Logger.LogInfo("[Developer Item Spawner] ¡Mod cargado con éxito!");
            var harmony = new Harmony("com.salomon.humanhost.item.spawner");
            harmony.PatchAll();

            // Cargar el catálogo al iniciar si el archivo ya existe
            CargarCatalogDesdeArchivo();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                mostrarMenu = !mostrarMenu;
                AjustarCursor(mostrarMenu);
            }

            // Escucha de Tecla F9 para el extractor dinámico
            if (Input.GetKeyDown(KeyCode.F9))
            {
                GenerarDumpDeItems();
            }
        }

        // Helper para comprobar si el puntero está dentro de la consola del spawner
        public static bool IsMouseOverConsole()
        {
            if (instancia == null || !instancia.mostrarMenu)
            {
                return false;
            }

            // Invertir coordenada Y (Input es de abajo-arriba, GUI es de arriba-abajo)
            Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return instancia.ventanaRect.Contains(mousePos);
        }

        // Método inteligente para ajustar el estado del cursor sin romper la UI o el movimiento del juego
        private void AjustarCursor(bool visible)
        {
            if (CamController.ins != null)
            {
                if (visible)
                {
                    CamController.ins.Show_Cursor(true);
                }
                else
                {
                    // Solo ocultamos y bloqueamos el cursor si el jugador no tiene la mochila o menús abiertos
                    if (!IsGameMenuOpen())
                    {
                        CamController.ins.Hide_Cursor();
                    }
                }
            }
        }

        // Determina si algún menú del juego está actualmente abierto (Mochila, Cofres, Ajustes, etc.)
        private bool IsGameMenuOpen()
        {
            if (Item_Slot_Mgr.Ins != null)
            {
                try
                {
                    var field = typeof(Item_Slot_Mgr).GetField("_lastEnableMenus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    var list = field?.GetValue(Item_Slot_Mgr.Ins) as System.Collections.IList;
                    if (list != null && list.Count > 0)
                    {
                        return true;
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.LogWarning($"[Developer Item Spawner] Error al consultar menús abiertos: {ex.Message}");
                }
            }
            return false;
        }

        private void OnGUI()
        {
            if (!mostrarMenu)
            {
                return;
            }

            // Evitar Contaminación Estética en OnGUI
            Color colorFondoOriginal = GUI.backgroundColor;
            Color colorContenidoOriginal = GUI.contentColor;

            ventanaRect = GUI.Window(0, ventanaRect, DibujarConsola, "⚙️ CONSOLA DE SPAWNEO DE ÍTEMS");

            GUI.backgroundColor = colorFondoOriginal;
            GUI.contentColor = colorContenidoOriginal;
        }

        private void DibujarConsola(int windowID)
        {
            // Configurar tema oscuro para el dibujo actual
            GUI.backgroundColor = new Color(0.11f, 0.14f, 0.20f, 0.98f);
            GUI.contentColor = Color.white;

            // GUIStyle personalizado para que el texto de los botones normales use color Cyan
            GUIStyle estiloBoton = new GUIStyle(GUI.skin.button);
            estiloBoton.normal.textColor = Color.cyan;
            estiloBoton.hover.textColor = Color.cyan;
            estiloBoton.active.textColor = Color.cyan;
            estiloBoton.focused.textColor = Color.cyan;

            GUILayout.Space(10);

            // Layout Horizontal de Control (Fila Superior) - Expansión de campos habilitada
            GUILayout.BeginHorizontal();
            GUILayout.Label("🔍 Buscar:", GUILayout.Width(80));
            filtroBusqueda = GUILayout.TextField(filtroBusqueda, GUILayout.ExpandWidth(true));
            GUILayout.Label("📦 Cant.:", GUILayout.Width(65));
            cantidadTexto = GUILayout.TextField(cantidadTexto, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Coincidencias del catálogo (Haz clic para spawnear):");

            // Aumento de altura a 250px para la consola ultra-panorámica
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUI.skin.box, GUILayout.Height(250));

            string filtroLower = filtroBusqueda.ToLower();
            bool hasFilter = filtroBusqueda.Length >= 1;

            if (hasFilter)
            {
                if (!catalogCargado)
                {
                    CargarCatalogDesdeArchivo();
                }

                if (itemCatalog.Count == 0)
                {
                    GUILayout.Label("El catálogo está vacío. Presiona F9 para generarlo.");
                }
                else
                {
                    int matchCount = 0;
                    foreach (System.Collections.Generic.KeyValuePair<string, string> item in itemCatalog)
                    {
                        // Limpieza inicial para la visualización del nombre
                        string nombreLimpio = item.Value.Contains("_Icon") ? item.Value.Replace("_Icon", "") : item.Value;
                        
                        // Remover prefijos comunes de desarrollo para buscar traducción limpia
                        string nombreBuscado = nombreLimpio.Replace("RII_", "").Replace("AA_", "").Replace("Item_", "").Trim();

                        // Intentar traducir usando el diccionario de internacionalización
                        string nombreMostrar = nombreLimpio;
                        if (DiccionarioTraducciones.ContainsKey(nombreBuscado))
                        {
                            nombreMostrar = DiccionarioTraducciones[nombreBuscado];
                        }
                        else if (DiccionarioTraducciones.ContainsKey(nombreLimpio))
                        {
                            nombreMostrar = DiccionarioTraducciones[nombreLimpio];
                        }

                        // Filtro de búsqueda: Evalúa coincidencias tanto en inglés como en español
                        if (item.Value.ToLower().Contains(filtroLower) || nombreMostrar.ToLower().Contains(filtroLower))
                        {
                            // Texto del Botón traducido
                            if (GUILayout.Button($"➕ {nombreMostrar}", estiloBoton, GUILayout.ExpandWidth(true)))
                            {
                                itemGUID = item.Key; // Pasa el GUID real (Key)
                                EjecutarSpawneo();
                            }
                            
                            matchCount++;
                            if (matchCount >= 100)
                            {
                                GUILayout.Label("... y más coincidencias. Refina tu búsqueda.");
                                break;
                            }
                        }
                    }

                    if (matchCount == 0)
                    {
                        GUILayout.Label("No se encontraron coincidencias.");
                    }
                }
            }
            else
            {
                GUILayout.Label("Introduce al menos 1 letra para buscar coincidencias...");
            }

            GUILayout.EndScrollView();

            // Entrada Manual de Respaldo (Fila Inferior) - Expansión habilitada
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("GUID Manual:", GUILayout.Width(85));
            itemGUID = GUILayout.TextField(itemGUID, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("📦 GENERAR ÍTEM (Spawn)", GUILayout.Height(30)))
            {
                EjecutarSpawneo();
            }

            // Arrastre de Ventana
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void EjecutarSpawneo()
        {
            if (string.IsNullOrEmpty(itemGUID))
            {
                return;
            }

            // Sanitización de inputs
            string sanitizedGUID = itemGUID.Trim();

            // Asegurar que el catálogo esté cargado para validar
            if (!catalogCargado)
            {
                CargarCatalogDesdeArchivo();
            }

            // Validación de existencia en la base de datos en memoria (Lista Blanca por GUID)
            if (!itemCatalog.ContainsKey(sanitizedGUID))
            {
                Logger.LogWarning($"[Developer Item Spawner] Bloqueado: El recurso '{sanitizedGUID}' no existe en el catálogo de items_dump.txt. Inyección abortada.");
                
                if (NotificationSystem.ins != null)
                {
                    NotificationSystem.ins.Add_Notice("¡Ítem no encontrado en la base de datos!", false);
                }
                return;
            }

            if (int.TryParse(cantidadTexto, out int cantidad))
            {
                if (cantidad > 0)
                {
                    if (Item_Slot_Mgr.Ins != null)
                    {
                        // Diagnóstico pre-spawneo
                        var dataSlotAllField = typeof(Item_Slot_Mgr).GetField("_DataSlotAll", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        var dataSlotAllValue = dataSlotAllField?.GetValue(Item_Slot_Mgr.Ins);
                        if (dataSlotAllValue == null)
                        {
                            Logger.LogWarning("[Developer Item Spawner] Diagnóstico: _DataSlotAll es nulo en Item_Slot_Mgr.Ins.");
                        }

                        if (Player_Input.ins == null)
                        {
                            Logger.LogWarning("[Developer Item Spawner] Diagnóstico: Player_Input.ins es nulo.");
                        }
                        else
                        {
                            var charIcons = Player_Input.ins.GetComponent<Char_Item_Icons>();
                            if (charIcons == null)
                            {
                                Logger.LogWarning("[Developer Item Spawner] Diagnóstico: Char_Item_Icons no se encuentra en el Player.");
                            }
                            else if (charIcons._allSlots == null)
                            {
                                Logger.LogWarning("[Developer Item Spawner] Diagnóstico: Char_Item_Icons._allSlots es nulo.");
                            }
                        }

                        // Creación de la referencia usando el GUID real
                        AssetReference itemRef = new AssetReference(sanitizedGUID);
                        
                        try
                        {
                            // PRE-CARGAR el asset en memoria de forma síncrona
                            // Esto previene que el juego intente una carga asíncrona tardía en un frame posterior,
                            // lo que dejaba el campo '_iconInfoPrefab' como nulo en el frame actual y provocaba un NullReferenceException nativo.
                            var handle = Addressables.LoadAssetAsync<GameObject>(itemRef);
                            GameObject preloadedAsset = handle.WaitForCompletion();

                            if (preloadedAsset != null)
                            {
                                Item_Slot_Mgr.Ins.Add_PurchasedItem_To_Player(itemRef, cantidad);
                            }
                            else
                            {
                                Logger.LogWarning($"[Developer Item Spawner] No se pudo precargar el asset '{sanitizedGUID}' en memoria.");
                                if (NotificationSystem.ins != null)
                                {
                                    NotificationSystem.ins.Add_Notice("Este asset no es un objeto spawneable válido.", false);
                                }
                            }
                            
                            // Liberamos el handle de precarga. La UI del juego mantendrá la referencia interna necesaria.
                            Addressables.Release(handle);
                        }
                        catch (System.Exception ex)
                        {
                            Logger.LogWarning($"[Developer Item Spawner] Error nativo al inyectar el asset '{sanitizedGUID}': {ex.Message}");
                            if (NotificationSystem.ins != null)
                            {
                                NotificationSystem.ins.Add_Notice("Error al inyectar el objeto.", false);
                            }
                        }
                    }
                }
            }
        }

        // Extractor Limpio y Seguro: Lee directamente de Loot_Mgr (_All_Loot_Icons + _SmashLoots)
        // Esto garantiza que se vuelquen exclusivamente armas, consumibles, herramientas y minerales de crafteo reales,
        // obteniendo sus nombres reales y GUIDs verdaderos de 32 caracteres sin crasheos y sin basura de escenarios.
        private void GenerarDumpDeItems()
        {
            try
            {
                // Intentar leer desde la base de datos de loot master del juego (Loot_Mgr)
                var lootMgrType = typeof(Loot_Mgr);
                var insField = lootMgrType.GetField("_ins", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                Loot_Mgr lootMgr = insField?.GetValue(null) as Loot_Mgr;

                if (lootMgr == null)
                {
                    Logger.LogWarning("[ItemDump] Loot_Mgr._ins es nulo. Asegúrate de estar dentro del juego (partida iniciada) para volcar.");
                    if (NotificationSystem.ins != null)
                    {
                        NotificationSystem.ins.Add_Notice("Inicia partida antes de presionar F9.", false);
                    }
                    return;
                }

                System.Collections.Generic.Dictionary<string, string> uniqueKeys = new System.Collections.Generic.Dictionary<string, string>();

                // 1. Extraer ítems normales desde _All_Loot_Icons
                var allLootIconsField = lootMgrType.GetField("_All_Loot_Icons", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                var allLootIcons = allLootIconsField?.GetValue(lootMgr) as System.Collections.IEnumerable;

                if (allLootIcons != null)
                {
                    foreach (var lootGroup in allLootIcons)
                    {
                        if (lootGroup == null) continue;

                        var groupType = lootGroup.GetType();
                        var iconsRefField = groupType.GetField("_all_Icons_Ref", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        var iconsRefArray = iconsRefField?.GetValue(lootGroup) as AssetReference[];

                        if (iconsRefArray != null)
                        {
                            foreach (var assetRef in iconsRefArray)
                            {
                                if (assetRef == null) continue;
                                
                                try
                                {
                                    // Cargar temporalmente de forma síncrona para obtener el nombre real de prefab
                                    var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(assetRef);
                                    UnityEngine.Object itemAsset = handle.WaitForCompletion();
                                    
                                    if (itemAsset != null)
                                    {
                                        string realName = itemAsset.name;
                                        string guid = assetRef.AssetGUID; // GUID real de 32 caracteres
                                        
                                        if (!string.IsNullOrEmpty(realName) && !string.IsNullOrEmpty(guid))
                                        {
                                            if (!uniqueKeys.ContainsKey(guid))
                                            {
                                                uniqueKeys.Add(guid, realName);
                                            }
                                        }
                                    }
                                    Addressables.Release(handle);
                                }
                                catch
                                {
                                    // Silenciar posibles fallos de carga
                                }
                            }
                        }
                    }
                }

                // 2. Extraer recursos minerales y básicos desde _SmashLoots (Piedra, Madera, Arena, Arcilla, etc.)
                var smashLootsField = lootMgrType.GetField("_SmashLoots", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                var smashLoots = smashLootsField?.GetValue(lootMgr) as System.Collections.IEnumerable;

                if (smashLoots != null)
                {
                    foreach (var smashLoot in smashLoots)
                    {
                        if (smashLoot == null) continue;

                        var smashType = smashLoot.GetType();
                        var iconRefField = smashType.GetField("_IconRef", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        var assetRef = iconRefField?.GetValue(smashLoot) as AssetReference;

                        if (assetRef != null)
                        {
                            try
                            {
                                // Cargar temporalmente de forma síncrona para obtener el nombre real de prefab
                                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(assetRef);
                                UnityEngine.Object itemAsset = handle.WaitForCompletion();
                                
                                if (itemAsset != null)
                                {
                                    string realName = itemAsset.name;
                                    string guid = assetRef.AssetGUID; // GUID real de 32 caracteres
                                    
                                    if (!string.IsNullOrEmpty(realName) && !string.IsNullOrEmpty(guid))
                                    {
                                        if (!uniqueKeys.ContainsKey(guid))
                                        {
                                            uniqueKeys.Add(guid, realName);
                                        }
                                    }
                                }
                                Addressables.Release(handle);
                            }
                            catch
                            {
                                // Silenciar posibles fallos de carga
                            }
                        }
                    }
                }

                string filePath = Path.Combine(Paths.ConfigPath, "items_dump.txt");
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine("==================================================");
                    writer.WriteLine("   DEVELOPER ITEM SPAWNER - EXTRACTOR DINÁMICO DE GUIDS");
                    writer.WriteLine($"   Generado el: {System.DateTime.Now}");
                    writer.WriteLine("==================================================");
                    writer.WriteLine();

                    foreach (var kvp in uniqueKeys)
                    {
                        // Guardar en el formato estándar Nombre real + GUID de 32 caracteres real
                        writer.WriteLine($"Nombre: {kvp.Value} | GUID: {kvp.Key}");
                    }
                }

                Logger.LogInfo($"[ItemDump] Base de datos de items volcada con éxito en: {filePath}");
                CargarCatalogDesdeArchivo();

                if (NotificationSystem.ins != null)
                {
                    NotificationSystem.ins.Add_Notice($"¡Dump generado con éxito! {uniqueKeys.Count} items.", false);
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[ItemDump] Error al generar dump de items: {ex.Message}");
            }
        }

        // Carga el catálogo usando Dictionary y separador por tubería
        private void CargarCatalogDesdeArchivo()
        {
            try
            {
                string filePath = Path.Combine(Paths.ConfigPath, "items_dump.txt");
                if (!File.Exists(filePath))
                {
                    catalogCargado = false;
                    return;
                }

                itemCatalog.Clear();
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    // Parsear formato: "Nombre: [nombre] | GUID: [guid]"
                    if (line.Contains("Nombre:") && line.Contains("GUID:"))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            string nombre = parts[0].Replace("Nombre:", "").Trim();
                            string guid = parts[1].Replace("GUID:", "").Trim();
                            
                            // Evitar duplicados: Key = GUID (para spawneo), Value = Nombre Humano (para UI)
                            if (!itemCatalog.ContainsKey(guid))
                            {
                                itemCatalog.Add(guid, nombre);
                            }
                        }
                    }
                }
                catalogCargado = true;
                Logger.LogInfo($"[ItemDump] Catálogo cargado con éxito. Se encontraron {itemCatalog.Count} ítems.");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[ItemDump] Error al cargar catálogo: {ex.Message}");
            }
        }
    }

    // ============================================================
    //  PARCHES DE HARMONY PARA EVITAR CLICK-THROUGH NO INVASIVO
    // ============================================================
    [HarmonyPatch(typeof(Input), "GetMouseButton")]
    public static class Patch_Input_GetMouseButton
    {
        [HarmonyPrefix]
        public static bool Prefix(int button, ref bool __result)
        {
            if (PluginPrincipal.IsMouseOverConsole())
            {
                __result = false;
                return false; // Bloquear que el juego detecte el click presionado
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButtonDown")]
    public static class Patch_Input_GetMouseButtonDown
    {
        [HarmonyPrefix]
        public static bool Prefix(int button, ref bool __result)
        {
            if (PluginPrincipal.IsMouseOverConsole())
            {
                __result = false;
                return false; // Bloquear que el juego detecte el click inicial
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButtonUp")]
    public static class Patch_Input_GetMouseButtonUp
    {
        [HarmonyPrefix]
        public static bool Prefix(int button, ref bool __result)
        {
            if (PluginPrincipal.IsMouseOverConsole())
            {
                __result = false;
                return false; // Bloquear que el juego detecte cuando se suelta el click
            }
            return true;
        }
    }

    // ============================================================
    //  PARCHE DE HARMONY: Slot_Info.LoadNewIcon (Prefix)
    //  Protección defensiva contra carga asíncrona de iconos inválidos
    // ============================================================
    [HarmonyPatch(typeof(Slot_Info), "LoadNewIcon")]
    public static class Patch_Slot_Info_LoadNewIcon
    {
        [HarmonyPrefix]
        public static bool Prefix(AssetReference needLoadAssetRef)
        {
            // Si la referencia del ícono del ítem es nula o inválida, cancelamos la carga para evitar NRE en hilos async
            if (needLoadAssetRef == null || !needLoadAssetRef.RuntimeKeyIsValid())
            {
                return false; // Salto seguro
            }
            return true; // Continuar normalmente
        }
    }
}
