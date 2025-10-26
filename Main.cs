using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Utilla;
using Utilla.Attributes;

namespace IronMonke
{
    public class ModInfo
    {
        public const string _id = "buzzbb.ironmonke";
        public const string _name = "Iron Monke";
    }

    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.6.2")]
    [BepInPlugin(ModInfo._id, ModInfo._name, "1.0.4")]
    public class Main : BaseUnityPlugin
    {
        bool modded;
        bool isEnabled;
        GameObject gL;
        AudioSource aL;
        ParticleSystem psL;
        GameObject gR;
        AudioSource aR;
        ParticleSystem psR;

        Main()
        {
            if (!File.Exists(BeeLog.LogPath(false))) File.Create(BeeLog.LogPath(false));
            if (!File.Exists(BeeLog.LogPath(true))) File.Create(BeeLog.LogPath(true));
        }

        void Start()
        {
            if (File.ReadAllLines(BeeLog.LogPath(false)).Length > 0)
            {
                var ogFile = File.ReadAllLines(BeeLog.LogPath(false));
                File.WriteAllLines(BeeLog.LogPath(true), ogFile);
                File.WriteAllText(BeeLog.LogPath(false), "");
            }
            GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
            if (modded) gL?.SetActive(true);
            isEnabled = true;
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
            gL?.SetActive(false);
            isEnabled = false;
        }

        void OnGameInitialized()
        {
            try
            {
                
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("IronMonke.gloven");
                var bundle = AssetBundle.LoadFromStream(stream);
                stream.Close();

                gL = Instantiate(bundle.LoadAsset<GameObject>("gloveL"));
                aL = gL.GetComponent<AudioSource>();
                gL.transform.SetParent(GorillaTagger.Instance.offlineVRRig.leftHandTransform.parent, false);
                psL = gL.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
                gL.SetActive(false);

                gR = Instantiate(bundle.LoadAsset<GameObject>("gloveR"));
                aR = gR.GetComponent<AudioSource>();
                gR.transform.SetParent(GorillaTagger.Instance.offlineVRRig.rightHandTransform.parent, false);
                psR = gR.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
                gR.SetActive(false);
            }
            catch (Exception ex)
            {
                BeeLog.Log(ex.ToString(), true, 1);
            }
        }

        void FixedUpdate()
        {
            if (modded)
            {
                try
                {
                    if (ControllerInputPoller.instance.leftControllerPrimaryButton)
                    {
                        GorillaLocomotion.GTPlayer.Instance.bodyCollider.attachedRigidbody.AddForce(10 * gL.transform.parent.right, ForceMode.Acceleration);
                        if (!psL.isPlaying) psL.Play();
                        if (!aL.isPlaying) aL.Play();
                        GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tapHapticStrength / 50f * GorillaLocomotion.GTPlayer.Instance.bodyCollider.attachedRigidbody.velocity.magnitude, GorillaTagger.Instance.tapHapticDuration);
                        aL.volume = 0.03f * GorillaLocomotion.GTPlayer.Instance.bodyCollider.attachedRigidbody.velocity.magnitude;
                    }
                    else
                    {
                        psL.Stop();
                        aL.Stop();
                    }

                    if (ControllerInputPoller.instance.rightControllerPrimaryButton)
                    {
                        GorillaLocomotion.GTPlayer.Instance.bodyCollider.attachedRigidbody.AddForce(10 * -gR.transform.parent.right, ForceMode.Acceleration);
                        if (!psR.isPlaying) psR.Play();
                        if (!aR.isPlaying) aR.Play();
                        GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tapHapticStrength / 50f * GorillaLocomotion.GTPlayer.Instance.bodyCollider.attachedRigidbody.velocity.magnitude, GorillaTagger.Instance.tapHapticDuration);
                        aR.volume = 0.03f * GorillaLocomotion.GTPlayer.Instance.bodyCollider.attachedRigidbody.velocity.magnitude;
                    }
                    else
                    {
                        psR.Stop();
                        aR.Stop();
                    }
                }
                catch (Exception e)
                {
                    BeeLog.Log(e.ToString(), true, 1);
                }
            }
        }

        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            modded = true;
            if (isEnabled)
            {
                gL?.SetActive(true);
                gR?.SetActive(true);
            }
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            modded = false;
            gL?.SetActive(false);
            gR?.SetActive(false);
        }
    }

    public class BeeLog
    {
        public static void Log(string toLog, bool nonUnity, int? idx)
        {
            if (idx.HasValue)
            {
                switch (idx)
                {
                    case 0:
                        if (!nonUnity) Debug.Log($"Log of {ModInfo._name}: {toLog}");
                        break;
                    case 1:
                        if (!nonUnity) Debug.LogError($"Error of {ModInfo._name}: {toLog}");
                        break;
                    case 2:
                        if (!nonUnity) Debug.LogWarning($"Warning of {ModInfo._name}: {toLog}");
                        break;
                }
            }
            SaveLog(toLog);
        }

        public static string LogPath(bool old) => string.Format("{0}/{1}.log", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ModInfo._name + (old ? "Previous" : ""));

        private static void SaveLog(string toLog)
        {
            var ogFile = File.ReadAllLines(LogPath(false));
            File.WriteAllLines(LogPath(false), ogFile.AddItem(toLog));
        }
    }

    public class HarmonyPatches
    {
        private static Harmony instance;

        public static bool IsPatched { get; private set; }

        internal static void ApplyHarmonyPatches()
        {
            if (!IsPatched)
            {
                if (instance == null)
                {
                    instance = new Harmony(ModInfo._id);
                }

                instance.PatchAll(Assembly.GetExecutingAssembly());
                IsPatched = true;
            }
        }

        internal static void RemoveHarmonyPatches()
        {
            if (instance != null && IsPatched)
            {
                instance.UnpatchSelf();
                IsPatched = false;
            }
        }
    }
}