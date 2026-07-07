using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HumanHostGatheringMultiplier
{
    // ============================================================
    //  PLUGIN: Gathering Multiplier
    //  Multiplica los recursos obtenidos al talar árboles y picar menas.
    //  Archivo de configuración: BepInEx\config\humanhost.gathering.multiplier.cfg
    // ============================================================
    [BepInPlugin("humanhost.gathering.multiplier", "Gathering Multiplier", "1.0.0")]
    public class PluginPrincipal : BaseUnityPlugin
    {
        // Opciones de configuración (.cfg)
        public static ConfigEntry<float> MultiplicadorMadera;
        public static ConfigEntry<float> MultiplicadorMinerales;

        private void Awake()
        {
            // Vinculamos las opciones de configuración
            MultiplicadorMadera = Config.Bind(
                "Multiplicadores",
                "WoodMultiplier", 3.0f,
                "Multiplicador de recursos al talar árboles (Ej: 3.0 = Triple de madera).");

            MultiplicadorMinerales = Config.Bind(
                "Multiplicadores",
                "OreMultiplier", 3.0f,
                "Multiplicador de piedra, plantas y minerales al picar nodos de recursos (Ej: 3.0 = Triple).");

            Logger.LogInfo("[Gathering Multiplier] ¡Mod cargado! Aplicando parches de recolección...");

            // Harmony se encarga de buscar y aplicar los parches automáticamente
            var harmony = new Harmony("humanhost.gathering.multiplier");
            harmony.PatchAll();
        }
    }

    // ============================================================
    //  PARCHE 1: MULTIPLICADOR DE MADERA (ÁRBOLES)
    //  Intercepta Spawn_Logs_And_Destroy_The_Tree en Tree_Falling_Handler
    // ============================================================
    [HarmonyPatch(typeof(Tree_Falling_Handler), "Spawn_Logs_And_Destroy_The_Tree")]
    public class ParcheTalarArboles
    {
        private static float tasaLootOriginal = 1f;
        private static bool seModifico = false;

        static void Prefix()
        {
            // Verificamos que la configuración del juego esté cargada
            if (G_Save._config != null)
            {
                tasaLootOriginal = G_Save._config._Loot_Rate_Total;
                G_Save._config._Loot_Rate_Total *= PluginPrincipal.MultiplicadorMadera.Value;
                seModifico = true;
            }
        }

        static void Postfix()
        {
            // Restauramos el valor original inmediatamente después de la ejecución
            if (seModifico && G_Save._config != null)
            {
                G_Save._config._Loot_Rate_Total = tasaLootOriginal;
                seModifico = false;
            }
        }
    }

    // ============================================================
    //  PARCHE 2: MULTIPLICADOR DE MINERALES (PICAR)
    //  Intercepta Process_Smashed_Shard en TopOnHit
    // ============================================================
    [HarmonyPatch(typeof(TopOnHit), "Process_Smashed_Shard")]
    public class ParcheMinarMinerales
    {
        private static float tasaLootOriginal = 1f;
        private static bool seModifico = false;

        static void Prefix()
        {
            if (G_Save._config != null)
            {
                tasaLootOriginal = G_Save._config._Loot_Rate_Total;
                G_Save._config._Loot_Rate_Total *= PluginPrincipal.MultiplicadorMinerales.Value;
                seModifico = true;
            }
        }

        static void Postfix()
        {
            if (seModifico && G_Save._config != null)
            {
                G_Save._config._Loot_Rate_Total = tasaLootOriginal;
                seModifico = false;
            }
        }
    }
}
