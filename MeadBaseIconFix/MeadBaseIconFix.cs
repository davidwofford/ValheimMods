using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MeadBaseIconFix
{
    [BepInPlugin("org.davidwofford.plugins.meadbaseiconfix", "Mead Base Icon Fix", "1.0.0.0")]
    public class MeadBaseIconFix : BaseUnityPlugin
    {
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<int> nexusId;

        public static void Dbgl(string str = "", bool pref = true)
        {
            Debug.Log((pref ? typeof(MeadBaseIconFix).Namespace + " " : "") + str);
        }

        void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusId = Config.Bind<int>("General", "NexusID", 480, "Nexus mod ID for updates");
            nexusId.Value = 480;
            Config.Save();

            if (!modEnabled.Value)
            {
                return;
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        static class ObjectDB_Awake_Patch
        {
            static void Postfix()
            {
                var assetBundle = LoadAssetBundle("meadbaseiconfix");

                if (assetBundle != null) {
                    var items = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, "Mead");

                    foreach (ItemDrop item in items)
                    {
                        var sprite = assetBundle.LoadAsset<Sprite>(item.name);

                        if (sprite != null) {
                            item.m_itemData.m_shared.m_icons[item.m_itemData.m_variant] = sprite;
                        }
                    }

                    assetBundle.Unload(false);
                }
            }
                
            private static AssetBundle LoadAssetBundle(string filename)
            {
                var assetBundlePath = GetAssetPath(filename);
                if (!string.IsNullOrEmpty(assetBundlePath))
                {
                    return AssetBundle.LoadFromFile(assetBundlePath);
                }

                return null;
            }

            private static string GetAssetPath(string assetName)
            {
                var assetFileName = Path.Combine(Paths.PluginPath, "MeadBaseIconFix", assetName);
                if (!File.Exists(assetFileName))
                {
                    Assembly assembly = typeof(MeadBaseIconFix).Assembly;
                    assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
                    if (!File.Exists(assetFileName))
                    {
                        Dbgl("Could not find asset ({assetName})");
                        return null;
                    }
                }
                
                return assetFileName;
            }
        }
    }
}
