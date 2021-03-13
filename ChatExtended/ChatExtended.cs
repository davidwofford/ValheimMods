using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace ChatExtended
{
    [BepInPlugin("org.davidwofford.plugins.chatextended", "Chat Extended", "1.0.0.0")]
    public class ChatExtended : BaseUnityPlugin
    {
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<float> chatTimeout;
        private static ConfigEntry<bool> showChatOnShout;

        public static void Dbgl(string str = "", bool pref = true)
        {
            Debug.Log((pref ? typeof(ChatExtended).Namespace + " " : "") + str);
        }

        // Awake is called once when both the game and the plug-in are loaded
        void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            chatTimeout = Config.Bind<float>("General", "ChatTimeout", 10.00f, "chat timeout in seconds");
            showChatOnShout = Config.Bind<bool>("General", "ShowChatOnShout", true, "show the chat window when someone shouts");
            Config.Save();

            if (!modEnabled.Value)
            {
                return;
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Chat), "Awake")]
        static class Chat_Awake_Patch
        {
            static bool Prefix(Chat __instance)
            {
                __instance.m_hideDelay = chatTimeout.Value;

                return true;
            }
        }

        [HarmonyPatch(typeof(Chat), "UpdateWorldTexts")]
        static class Chat_UpdateWorldTexts_Patch
        {
            static void Postfix(Chat __instance)
            {
                var worldTexts = new List<Chat.WorldTextInstance>();

                if (showChatOnShout.Value)
                {
                    __instance.GetShoutWorldTexts(worldTexts);
                }

                if (worldTexts.Count > 0 && !__instance.IsChatDialogWindowVisible())
                {
                    __instance.m_chatWindow.gameObject.SetActive(true);
                }
            }
        }
    }
}
