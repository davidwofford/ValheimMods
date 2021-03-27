using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MeadBaseIconFix
{
    [BepInPlugin("org.davidwofford.plugins.meadbaseiconfix", "Mead Base Icon Fix", "1.0.2")]
    public class MeadBaseIconFix : BaseUnityPlugin
    {
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<int> nexusId;
        private Harmony _harmony;

        public static void Dbgl(string str = "", bool pref = true)
        {
            Debug.Log((pref ? typeof(MeadBaseIconFix).Namespace + " " : "") + str);
        }

        void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusId = Config.Bind<int>("General", "NexusID", 710, "Nexus mod ID for updates");
            nexusId.Value = 710;
            Config.Save();

            if (!modEnabled.Value)
            {
                return;
            }

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "MeadBaseIconFix");
        }

        private void OnDestroy()
        {
            Harmony harmony = _harmony;
            if (harmony == null)
            {
                return;
            }
            harmony.UnpatchAll(null);
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        static class ObjectDB_Awake_Patch
        {
            static void Postfix()
            {
                Dbgl("ObjectDb Awake Patch Start");
                TryUpdateItemIcons();
                Dbgl("ObjectDb Awake Patch End");
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDB_CopyOtherDB_Patch
        {
            // Token: 0x06000016 RID: 22 RVA: 0x00002D9D File Offset: 0x00000F9D
            public static void Postfix()
            {
                Dbgl("ObjectDB CopyOtherDb Patch Start");
                TryUpdateItemIcons();
                Dbgl("ObjectDB CopyOtherDb Patch End");
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

        public static void TryUpdateItemIcons()
        {
            var assetBundle = LoadAssetBundle("meadbaseiconfix");

            if (assetBundle != null)
            {
                var meads = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, "MeadBase");
                // There is currently only one BarleyWineBase item in the game, and it's for fire resist
                var wines = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, "BarleyWineBase");

                foreach (ItemDrop mead in meads)
                {
                    Dbgl($"Updating icon for {mead.name}");
                    var sprite = assetBundle.LoadAsset<Sprite>(mead.name);

                    if (sprite != null)
                    {
                        mead.m_itemData.m_shared.m_icons[mead.m_itemData.m_variant] = sprite;
                    }
                }

                var wineSprite = assetBundle.LoadAsset<Sprite>("MeadBaseFireResist");
                if (wineSprite != null)
                {
                    foreach (ItemDrop wine in wines)
                    {
                        Dbgl($"Updating icon for {wine.name}");
                        wine.m_itemData.m_shared.m_icons[wine.m_itemData.m_variant] = wineSprite;
                    }
                }


                assetBundle.Unload(false);
            }
        }
    }
}
