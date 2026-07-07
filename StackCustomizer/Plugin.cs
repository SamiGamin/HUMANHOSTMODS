using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace StackCustomizer
{
    // ============================================================
    //  PLUGIN: Stack Tamano
    //  Modifica el tamaño máximo de stack de todos los ítems.
    //  Archivo de config: BepInEx\config\humanhost.stack.tamano.cfg
    // ============================================================
    [BepInPlugin("humanhost.stack.tamano", "Stack Tamano", "1.0.0")]
    public class PluginPrincipal : BaseUnityPlugin
    {
        public static ConfigEntry<int>  MaxStackGlobal;
        public static ConfigEntry<bool> StackIlimitado;
        public static ConfigEntry<bool> ForzarStackeable;

        private void Awake()
        {
            MaxStackGlobal = Config.Bind(
                "Stack",
                "MaxStack", 999,
                "Tamaño máximo de stack para TODOS los ítems apilables. (Vanilla suele ser 64 o 100)");

            StackIlimitado = Config.Bind(
                "Stack",
                "StackIlimitado", false,
                "Si es true, ignora MaxStack y establece el stack al máximo absoluto (2.147.483.647).");

            ForzarStackeable = Config.Bind(
                "Stack",
                "ForzarStackeable", false,
                "Si es true, fuerza que TODOS los ítems sean apilables, incluso los que el juego marca como no apilables.");

            Logger.LogInfo("[Stack Tamano] ¡Mod cargado! Aplicando parches avanzados de stack...");

            var harmony = new Harmony("humanhost.stack.tamano");
            harmony.PatchAll();
        }

        // Método utilitario: aplica la config de stack a un Icon_Info dado
        public static void AplicarStack(Icon_Info info)
        {
            if (info == null) return;
            if (!info._Can_Stack && !ForzarStackeable.Value) return;

            if (ForzarStackeable.Value)
                info._Can_Stack = true;

            info.MaxStack = StackIlimitado.Value
                ? int.MaxValue
                : MaxStackGlobal.Value;
        }

        // Utilidad para procesar un arreglo de slots
        public static void ProcesarSlots(Slot_Info[] slots)
        {
            if (slots == null) return;
            foreach (Slot_Info slot in slots)
            {
                if (slot == null)             continue;
                if (slot._IsEmptySlot)        continue;
                if (slot._inLoading)          continue;
                if (slot._iconInfoPrefab == null) continue;

                AplicarStack(slot._iconInfoPrefab);
            }
        }
    }

    // ============================================================
    //  PARCHE 1: Item_Slot_Mgr.Start (Postfix)
    //  Aplica el stack a todos los slots iniciales cuando arranca.
    // ============================================================
    [HarmonyPatch(typeof(Item_Slot_Mgr), "Start")]
    public class ParcheStack_Start
    {
        static void Postfix(Item_Slot_Mgr __instance)
        {
            if (__instance._allSlots_Player == null) return;

            foreach (Slot_Info slot in __instance._allSlots_Player)
            {
                if (slot == null)             continue;
                if (slot._IsEmptySlot)        continue;
                if (slot._inLoading)          continue;
                if (slot._iconInfoPrefab == null) continue;

                PluginPrincipal.AplicarStack(slot._iconInfoPrefab);
            }

            UnityEngine.Debug.Log("[Stack Tamano] MaxStack aplicado a todos los slots iniciales.");
        }
    }

    // ============================================================
    //  PARCHE 2: Item_Slot_Mgr.Find_CombineSlot_EnviroItem (Prefix)
    //  Se ejecuta al recoger ítems del suelo.
    // ============================================================
    [HarmonyPatch(typeof(Item_Slot_Mgr), "Find_CombineSlot_EnviroItem")]
    public class ParcheStack_Pickup
    {
        static void Prefix(Slot_Info[] slotInfos)
        {
            PluginPrincipal.ProcesarSlots(slotInfos);
        }
    }

    // ============================================================
    //  PARCHE 3: Item_Slot_Mgr.Find_CombineSlot (Prefix)
    //  Se ejecuta al combinar ítems normales (drag & drop o traspasos).
    // ============================================================
    [HarmonyPatch(typeof(Item_Slot_Mgr), "Find_CombineSlot")]
    public class ParcheStack_Combine
    {
        static void Prefix(Slot_Info[] slotInfos)
        {
            PluginPrincipal.ProcesarSlots(slotInfos);
        }
    }

    // ============================================================
    //  PARCHE 4: Item_Slot_Mgr.Find_CombineSlot_FastSwapTo (Prefix)
    //  Se ejecuta al hacer intercambio rápido (e.g. shift+click).
    // ============================================================
    [HarmonyPatch(typeof(Item_Slot_Mgr), "Find_CombineSlot_FastSwapTo")]
    public class ParcheStack_FastSwap
    {
        static void Prefix(Slot_Info[] targetSlots)
        {
            PluginPrincipal.ProcesarSlots(targetSlots);
        }
    }

    // ============================================================
    //  PARCHE 5: Slot_Info.set__StackText (Prefix)
    //  Se ejecuta cada vez que se actualiza visualmente la cantidad
    //  del stack (crafteos, compras, movimientos, etc.).
    // ============================================================
    [HarmonyPatch(typeof(Slot_Info), "set__StackText")]
    public class ParcheSlot_StackText
    {
        static void Prefix(Slot_Info __instance)
        {
            if (__instance == null) return;
            if (__instance._IsEmptySlot) return;
            if (__instance._inLoading) return;
            if (__instance._iconInfoPrefab == null) return;

            PluginPrincipal.AplicarStack(__instance._iconInfoPrefab);
        }
    }
}
