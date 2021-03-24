using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MusicReplacement
{
    [BepInPlugin("org.davidwofford.plugins.audioreplacement", "Audio Replacement", "1.0.0.0")]
    public class MusicReplacement : BaseUnityPlugin
    {
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<int> nexusId;
        private static Dictionary<string, Dictionary<string, AudioClip>> audioClips = new Dictionary<string, Dictionary<string, AudioClip>>();
        private static MusicReplacement context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            Debug.Log((pref ? typeof(MusicReplacement).Namespace + " " : "") + str);
        }

        void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusId = Config.Bind<int>("General", "NexusID", 480, "Nexus mod ID for updates");
            nexusId.Value = 712;
            Config.Save();

            if (!modEnabled.Value)
            {
                return;
            }

            PreloadMusic();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(MusicMan), "Awake")]
        static class MusicMan_Awake_Patch
        {
            static void Postfix()
            {
                var musicList = MusicMan.instance.m_music;

                for (var musicIndex = 0; musicIndex < musicList.Count; musicIndex++)
                {
                    var music = musicList[musicIndex];
                    if (music != null && audioClips.ContainsKey(music.m_name) && music.m_clips != null)
                    {
                        for (var audioIndex = 0; audioIndex < music.m_clips.Length; audioIndex++)
                        {
                            var audio = music.m_clips[audioIndex];

                            if (audio != null && audioClips[music.m_name].ContainsKey(audio.name) && audioClips[music.m_name][audio.name] != null)
                            {
                                musicList[musicIndex].m_clips[audioIndex] = audioClips[music.m_name][audio.name];
                            }
                        }
                    }
                }
            }
        }

        private static DirectoryInfo[] GetDirectories()
        {
            var directoryPath = Path.Combine(Paths.PluginPath, "MusicReplacement");

            if (Directory.Exists(directoryPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);

                return dirInfo.GetDirectories();
            }

            // make the directories and corresponding file templates
            try
            {
                Directory.CreateDirectory(directoryPath);

                Dictionary<string, string[]> existingSongs = GetExistingSongs();

                foreach (KeyValuePair<string, string[]> existingSongCategory in existingSongs)
                {
                    Directory.CreateDirectory(Path.Combine(directoryPath, existingSongCategory.Key));

                    if (existingSongCategory.Value.Length > 0)
                    {
                        foreach (string existingSong in existingSongCategory.Value)
                        {
                            File.Create(Path.Combine(directoryPath, existingSongCategory.Key, existingSong + ".replacemewithawav"));
                        }
                    }
                }

                DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);

                return dirInfo.GetDirectories();
            }
            catch (System.Exception e) {
                Dbgl(e.Message);
            }

            return null;
        }

        private static void PreloadMusic()
        {
            var directories = GetDirectories();

            audioClips.Clear();

            if (directories != null)
            {
                foreach (DirectoryInfo dirInfo in directories)
                {
                    FileInfo[] files = dirInfo.GetFiles("*.wav");

                    if (!audioClips.ContainsKey(dirInfo.Name)) {
                        audioClips[dirInfo.Name] = new Dictionary<string, AudioClip>();
                    }

                    foreach (FileInfo fileInfo in files)
                    {
                        context.StartCoroutine(GetAudioClipFromFile(fileInfo.FullName, audioClips[dirInfo.Name]));
                    }
                }
            }
        }

        public static IEnumerator GetAudioClipFromFile(string filePath, Dictionary<string, AudioClip> audioClips)
        {
            filePath = "file:///" + filePath.Replace("\\", "/");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.WAV))
            {
                www.SendWebRequest();
                yield return null;

                if (www.isNetworkError || www.isHttpError)
                {
                    Dbgl(www.error);
                }
                else
                {
                    var name = Path.GetFileNameWithoutExtension(filePath);

                    var audioClip = DownloadHandlerAudioClip.GetContent(www);

                    if (!audioClips.ContainsKey(name) && audioClip != null)
                    {
                        audioClips.Add(name, audioClip);
                    }
                }
            }
        }

        public static Dictionary<string, string[]> GetExistingSongs()
        {
            return new Dictionary<string, string[]>()
            {
                { "respawn", new string[]{ } },
                { "intro", new string[]{ "Entering Valheim" } },
                { "menu", new string[]{ "MenuMusic" } },
                { "combat", new string[]{ "ForestIsMovingLv5" } },
                { "CombatEventL1", new string[]{ "ForestIsMovingLv1" } },
                { "CombatEventL2", new string[]{ "ForestIsMovingLv2" } },
                { "CombatEventL3", new string[]{ "ForestIsMovingLv3" } },
                { "CombatEventL4", new string[]{ "ForestIsMovingLv4" } },
                { "boss_eikthyr", new string[]{ "Battle 1 - Eikthyr" } },
                { "boss_gdking", new string[]{ "Battle 2 - Elder" } },
                { "boss_bonemass", new string[]{ "Boss 3 Bonemass" } },
                { "boss_moder", new string[]{ "Boss 4 Mother" } },
                { "boss_goblinking", new string[]{ "Boss 5 Yagluth" } },
                { "morning", new string[]{ "Dawn" } },
                { "evening", new string[]{ "Dusk" } },
                { "sailing", new string[]{ "Sailing4" } },
                { "blackforest", new string[]{ "Black Forest(day)" } },
                { "meadows", new string[]{ "Meadows(day)" } }
            };
        }
    }
}
