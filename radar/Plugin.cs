using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

namespace radar
{
    [BepInPlugin("com.salomon.humanhost.interaction.radar", "Human Host Radar", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource log;

        // Active coroutine reference to prevent concurrent execution
        private Coroutine activeMenusCoroutine;

        // Reflection cache for Item_Slot_Mgr
        private Type cachedItemSlotMgrType;
        private FieldInfo cachedInsField;
        private PropertyInfo cachedInsProp;
        private FieldInfo cachedLastEnableMenusField;

        // Reflection cache for UI_Control
        private Type cachedUiControlType;
        private FieldInfo cachedUiInsField;
        private FieldInfo cachedShowingUIsField;

        private bool isReflectionCached = false;

        private void Awake()
        {
            log = Logger;
            log.LogInfo("RadarSniffer plugin loaded successfully!");
        }

        private void Update()
        {
            try
            {
                // Capture F key for Raycast and Inspection
                if (Input.GetKeyDown(KeyCode.F))
                {
                    ExecuteRaycastAndInspection();
                }

                // Capture TAB key for Active Menus
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (activeMenusCoroutine != null)
                    {
                        StopCoroutine(activeMenusCoroutine);
                    }
                    activeMenusCoroutine = StartCoroutine(CheckActiveMenusCoroutine());
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"[RadarSniffer] Error in Update loop: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ExecuteRaycastAndInspection()
        {
            try
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }

                if (mainCamera == null)
                {
                    log.LogWarning("[RadarSniffer] Main camera not found in scene. Cannot perform raycast.");
                    return;
                }

                Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                
                log.LogWarning($"[RadarSniffer] Launching Raycast from center of the screen (10m range)...");

                if (Physics.Raycast(ray, out hit, 10f))
                {
                    GameObject hitGo = hit.collider.gameObject;
                    string parentName = hitGo.transform.parent != null ? hitGo.transform.parent.name : "None";
                    log.LogWarning($"[RadarSniffer] Raycast Hit: {hitGo.name} | Parent: {parentName} | Distance: {hit.distance:F2}m | Point: {hit.point}");

                    Component[] components = hitGo.GetComponentsInParent<Component>(true);
                    if (components != null && components.Length > 0)
                    {
                        log.LogWarning($"[RadarSniffer] Found {components.Length} components in parent hierarchy.");
                        foreach (var comp in components)
                        {
                            if (comp == null) continue;
                            InspectComponent(comp);
                        }
                    }
                    else
                    {
                        log.LogWarning("[RadarSniffer] No components found in parent hierarchy of the hit object.");
                    }
                }
                else
                {
                    log.LogWarning("[RadarSniffer] Raycast did not hit anything within 10 meters.");
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"[RadarSniffer] Error during raycast/inspection: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void InspectComponent(Component comp)
        {
            try
            {
                Type type = comp.GetType();
                string typeName = type.Name;

                // Skip built-in Unity components to avoid spam
                if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
                {
                    return;
                }

                log.LogWarning($"[RadarSniffer] [Inspect] Component Type: {typeName} | GameObject: {comp.gameObject.name}");

                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                FieldInfo[] fields = type.GetFields(flags);
                PropertyInfo[] properties = type.GetProperties(flags);

                bool isTargetType = typeName == "Loot_Mgr" || typeName == "Icon_Info" || typeName == "Storage_Box" ||
                                    typeName.Contains("Loot") || typeName.Contains("Icon") || typeName.Contains("Storage");

                if (isTargetType)
                {
                    log.LogWarning($"[RadarSniffer] [Inspect] TARGET COMPONENT MATCHED: {typeName}");
                }

                // If it's a target component, we log all fields and properties.
                // Otherwise, we only log those that look like IDs, GUIDs or Keys.
                foreach (var field in fields)
                {
                    if (isTargetType || IsIdOrKeyName(field.Name))
                    {
                        try
                        {
                            object val = field.GetValue(comp);
                            log.LogWarning($"  * [Field] {field.Name} ({field.FieldType.Name}): {FormatValue(val)}");
                        }
                        catch (Exception ex)
                        {
                            log.LogWarning($"  * Error reading field {field.Name}: {ex.Message}");
                        }
                    }
                }

                foreach (var prop in properties)
                {
                    if (isTargetType || IsIdOrKeyName(prop.Name))
                    {
                        try
                        {
                            if (prop.GetIndexParameters().Length == 0)
                            {
                                object val = prop.GetValue(comp, null);
                                log.LogWarning($"  * [Property] {prop.Name} ({prop.PropertyType.Name}): {FormatValue(val)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogWarning($"  * Error reading property {prop.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"[RadarSniffer] Error inspecting component {comp.GetType().Name}: {ex.Message}");
            }
        }

        private bool IsIdOrKeyName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string lower = name.ToLowerInvariant();

            if (lower.Contains("guid") || lower.Contains("uuid") || lower.Contains("clave"))
                return true;

            if (lower.EndsWith("id") || lower.EndsWith("key") || lower.EndsWith("name") || lower.EndsWith("info"))
                return true;

            if (lower.StartsWith("id") || lower.StartsWith("key"))
                return true;

            if (lower.Contains("_id") || lower.Contains("_key") || lower.Contains("id_") || lower.Contains("key_"))
                return true;

            return false;
        }

        private string FormatValue(object val)
        {
            if (val == null) return "null";
            return val.ToString();
        }

        private void EnsureReflectionCached()
        {
            if (isReflectionCached) return;

            try
            {
                cachedItemSlotMgrType = FindType("Item_Slot_Mgr");
                if (cachedItemSlotMgrType != null)
                {
                    cachedInsField = cachedItemSlotMgrType.GetField("Ins", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    cachedInsProp = cachedItemSlotMgrType.GetProperty("Ins", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    cachedLastEnableMenusField = cachedItemSlotMgrType.GetField("_lastEnableMenus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                cachedUiControlType = FindType("UI_Control");
                if (cachedUiControlType != null)
                {
                    cachedUiInsField = cachedUiControlType.GetField("_ins", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    cachedShowingUIsField = cachedUiControlType.GetField("OnShowingUIs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                isReflectionCached = true;
            }
            catch (Exception ex)
            {
                log.LogWarning($"[RadarSniffer] Error caching reflection details: {ex.Message}");
            }
        }

        private IEnumerator CheckActiveMenusCoroutine()
        {
            yield return null; // Wait one frame
            
            try
            {
                log.LogInfo("[RadarSniffer] Checking active menus...");

                EnsureReflectionCached();

                if (cachedItemSlotMgrType == null)
                {
                    log.LogInfo("[RadarSniffer] Item_Slot_Mgr type not found.");
                    yield break;
                }

                // Find Singleton instance 'Ins'
                object insInstance = null;
                if (cachedInsField != null)
                {
                    insInstance = cachedInsField.GetValue(null);
                }
                else if (cachedInsProp != null)
                {
                    insInstance = cachedInsProp.GetValue(null, null);
                }

                if (insInstance == null)
                {
                    log.LogInfo("[RadarSniffer] Item_Slot_Mgr.Ins instance is null.");
                    yield break;
                }

                // Find field '_lastEnableMenus'
                if (cachedLastEnableMenusField == null)
                {
                    log.LogInfo("[RadarSniffer] Field '_lastEnableMenus' not found in Item_Slot_Mgr.");
                    yield break;
                }

                object lastEnableMenusVal = cachedLastEnableMenusField.GetValue(insInstance);
                if (lastEnableMenusVal == null)
                {
                    log.LogInfo("[RadarSniffer] _lastEnableMenus is null.");
                    yield break;
                }

                if (lastEnableMenusVal is IEnumerable enumerable)
                {
                    log.LogInfo("[RadarSniffer] Active Menus listing (_lastEnableMenus):");
                    int count = 0;
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        count++;
                        
                        string menuName = item.ToString();

                        // Try to extract a more descriptive name if possible
                        if (item is GameObject go)
                        {
                            menuName = go.name;
                        }
                        else if (item is Component c)
                        {
                            menuName = $"{c.gameObject.name} ({c.GetType().Name})";
                        }
                        else
                        {
                            Type itemType = item.GetType();
                            FieldInfo nameField = itemType.GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) 
                                                ?? itemType.GetField("Name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            PropertyInfo nameProp = itemType.GetProperty("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) 
                                                  ?? itemType.GetProperty("Name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (nameField != null)
                            {
                                menuName = nameField.GetValue(item)?.ToString() ?? menuName;
                            }
                            else if (nameProp != null)
                            {
                                menuName = nameProp.GetValue(item, null)?.ToString() ?? menuName;
                            }
                        }

                        log.LogInfo($"  - Active Menu: {menuName}");
                    }

                    if (count == 0)
                    {
                        log.LogInfo("[RadarSniffer] No active menus found in the list.");
                    }
                }
                else
                {
                    log.LogInfo($"[RadarSniffer] _lastEnableMenus value (not IEnumerable): {lastEnableMenusVal}");
                }

                // Add checking for UI_Control.OnShowingUIs (covers fullscreen HUD/Map panels)
                if (cachedUiControlType != null && cachedUiInsField != null && cachedShowingUIsField != null)
                {
                    object uiInsInstance = cachedUiInsField.GetValue(null);
                    if (uiInsInstance != null)
                    {
                        object showingUIsVal = cachedShowingUIsField.GetValue(uiInsInstance);
                        if (showingUIsVal is IEnumerable showingEnumerable)
                        {
                            log.LogInfo("[RadarSniffer] Active UI Panels (UI_Control.OnShowingUIs):");
                            int uiCount = 0;
                            foreach (var item in showingEnumerable)
                            {
                                if (item == null) continue;
                                uiCount++;
                                string uiName = item.ToString();
                                if (item is GameObject go)
                                {
                                    uiName = go.name;
                                }
                                else if (item is Component c)
                                {
                                    uiName = $"{c.gameObject.name} ({c.GetType().Name})";
                                }
                                log.LogInfo($"  - Active Panel: {uiName}");
                            }
                            if (uiCount == 0)
                            {
                                log.LogInfo("[RadarSniffer] No active UI panels in OnShowingUIs.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogInfo($"[RadarSniffer] Exception during menu check: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                activeMenusCoroutine = null;
            }
        }

        private Type FindType(string typeName)
        {
            Type t = Type.GetType(typeName);
            if (t != null) return t;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = assembly.GetType(typeName);
                    if (t != null) return t;
                }
                catch { }

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type != null && (type.Name == typeName || type.FullName == typeName))
                        {
                            return type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    if (ex.Types != null)
                    {
                        foreach (var type in ex.Types)
                        {
                            if (type != null && (type.Name == typeName || type.FullName == typeName))
                            {
                                return type;
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }
    }
}
