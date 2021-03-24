using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MeadBaseIconFix
{
    [BepInPlugin("org.davidwofford.plugins.meadbaseiconfix", "Mead Base Icon Fix", "1.0.1.0")]
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
            nexusId.Value = 710;
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
                    var meads = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, "MeadBase");
                    // There is currently only one BarleyWineBase item in the game, and it's for fire resist
                    var wines = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, "BarleyWineBase");

                    foreach (ItemDrop mead in meads)
                    {
                        var sprite = assetBundle.LoadAsset<Sprite>(mead.name);

                        if (sprite != null) {
                            mead.m_itemData.m_shared.m_icons[mead.m_itemData.m_variant] = sprite;
                        }
                    }

                    var wineSprite = assetBundle.LoadAsset<Sprite>("MeadBaseFireResist");
                    if (wineSprite != null) {
                        foreach (ItemDrop wine in wines)
                        {
                            wine.m_itemData.m_shared.m_icons[wine.m_itemData.m_variant] = wineSprite;
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
