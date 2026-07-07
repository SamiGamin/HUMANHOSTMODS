using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CofresAlmacenamiento
{
    // ============================================================
    //  PLUGIN: Expansion de Cofres
    //  Amplía el número de filas y columnas de los cofres y maleta de forma independiente.
    //  Archivo de config: BepInEx\config\humanhost.cofres.cfg
    // ============================================================
    [BepInPlugin("humanhost.cofres", "Expansion de Cofres", "1.0.0")]
    public class PluginPrincipal : BaseUnityPlugin
    {
        public static ConfigEntry<int> Tier1_Columnas;
        public static ConfigEntry<int> Tier1_Filas;
        public static ConfigEntry<int> Tier2_Columnas;
        public static ConfigEntry<int> Tier2_Filas;
        public static ConfigEntry<int> Maleta_Columnas;
        public static ConfigEntry<int> Maleta_Filas;
        public static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            Tier1_Columnas = Config.Bind(
                "Cofre Tier 1",
                "Columnas", 10,
                "Número de COLUMNAS del cofre Tier 1 (Madera). Vanilla = 5.");

            Tier1_Filas = Config.Bind(
                "Cofre Tier 1",
                "Filas", 6,
                "Número de FILAS del cofre Tier 1 (Madera). Vanilla = 4.");

            Tier2_Columnas = Config.Bind(
                "Cofre Tier 2",
                "Columnas", 10,
                "Número de COLUMNAS del cofre Tier 2 (Metal). Vanilla = 5.");

            Tier2_Filas = Config.Bind(
                "Cofre Tier 2",
                "Filas", 10,
                "Número de FILAS del cofre Tier 2 (Metal). Vanilla = 6.");

            Maleta_Columnas = Config.Bind(
                "Maleta",
                "Columnas", 10,
                "Número de COLUMNAS de la Maleta. Vanilla = 7.");

            Maleta_Filas = Config.Bind(
                "Maleta",
                "Filas", 10,
                "Número de FILAS de la Maleta. Vanilla = 8.");

            Logger.LogInfo("[Expansion de Cofres] ¡Mod cargado con soporte para Cofres y Maleta!");

            var harmony = new Harmony("humanhost.cofres");
            harmony.PatchAll();
        }
    }

    // ============================================================
    //  PARCHE: Loot_Mgr.Enable_Loot_Window
    //  Reescribe filas y columnas ANTES de que el juego dibuje el cofre.
    //  Solo afecta contenedores tipo "playerContainer" (cofres del jugador).
    // ============================================================
    [HarmonyPatch(typeof(Loot_Mgr), "Enable_Loot_Window")]
    public class ParcheCofres
    {
        static void Prefix(Transform openedTrans, ref int lineVertCount, ref int lineHorizCount, string belong_BI_Key, Per_Owner_Info.ContainerType containerType)
        {
            if (openedTrans != null)
            {
                PluginPrincipal.Logger.LogInfo($"[Cofres Debug] GameObject: '{openedTrans.name}' | Key: '{belong_BI_Key}' | Vanilla: {lineVertCount}x{lineHorizCount}");
            }

            // Ignorar por completo la mochila del jugador
            if (belong_BI_Key == "Player")
            {
                return;
            }

            if (containerType == Per_Owner_Info.ContainerType.playerContainer)
            {
                // Discriminación por dimensiones vanilla originales
                if (lineVertCount == 5 && lineHorizCount == 4) // Cofre de Madera (Tier 1)
                {
                    lineVertCount = PluginPrincipal.Tier1_Columnas.Value;
                    lineHorizCount = PluginPrincipal.Tier1_Filas.Value;
                    PluginPrincipal.Logger.LogInfo($"[Prefix] Modificando Cofre de Madera (Tier 1) a {lineVertCount}x{lineHorizCount}");
                }
                else if (lineVertCount == 5 && lineHorizCount == 6) // Cofre de Metal (Tier 2)
                {
                    lineVertCount = PluginPrincipal.Tier2_Columnas.Value;
                    lineHorizCount = PluginPrincipal.Tier2_Filas.Value;
                    PluginPrincipal.Logger.LogInfo($"[Prefix] Modificando Cofre de Metal (Tier 2) a {lineVertCount}x{lineHorizCount}");
                }
                else if ((lineVertCount == 7 && lineHorizCount == 8) || (lineVertCount == 8 && lineHorizCount == 7)) // Maleta
                {
                    lineVertCount = PluginPrincipal.Maleta_Columnas.Value;
                    lineHorizCount = PluginPrincipal.Maleta_Filas.Value;
                    PluginPrincipal.Logger.LogInfo($"[Prefix] Modificando Maleta a {lineVertCount}x{lineHorizCount}");
                }
            }
        }

        static void Postfix(Loot_Mgr __instance, Transform openedTrans, int lineVertCount, int lineHorizCount, string belong_BI_Key, Per_Owner_Info.ContainerType containerType)
        {
            // Ignorar por completo la mochila del jugador
            if (belong_BI_Key == "Player")
            {
                return;
            }

            if (containerType == Per_Owner_Info.ContainerType.playerContainer && __instance._DataLoot != null)
            {
                if (__instance._DataLoot._OwnersData.TryGetValue(belong_BI_Key, out Per_Owner_Info info) && info != null)
                {
                    if (info._grid == null)
                    {
                        info._grid = new Per_Owner_Info.Grid();
                    }
                    info._grid._ContainerGrid = new Vector2Int(lineVertCount, lineHorizCount);
                    PluginPrincipal.Logger.LogInfo($"[Postfix] Sincronizado grid lógico de '{openedTrans?.name}' (Key: '{belong_BI_Key}') a {lineVertCount}x{lineHorizCount}");
                }
            }
        }
    }
}
