using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Satchel;
using HKVocals;
using UnityEngine;
using JetBrains.Annotations;

#pragma warning disable CS8618
namespace PaleCourtAudioPack;

[PublicAPI]
public class PaleCourtAudioPackMod : Mod
{
    public static PaleCourtAudioPackMod Instance;
    public static bool MainModExists => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "HKVocal");
    private static AssetBundleCreateRequest loadRequest;

    public PaleCourtAudioPackMod() : base("Hallownest Vocalized AudioLoader")
    {
        Instance = this;

        if (!MainModExists) 
        {
            LogError("You dont have hallownest vocalized - pale court audio without the main hallownest vocalized mod. please download that");
            return;
        }
        
        Instance.Log("Starting Hallownest Vocalised - Pale Court Audio Load");
        LoadAssetBundle();
    }
    

    private static void LoadAssetBundle()
    {
        //if someone for some reason has an audiobundle, prioritize that instead of the embedded one
        if (File.Exists(AssemblyExtensions.GetCurrentDirectory() + "/paleaudio"))
        {
            loadRequest = AssetBundle.LoadFromFileAsync(AssemblyExtensions.GetCurrentDirectory() + "/paleaudio");
        }
        else
        {
            loadRequest = AssetBundle.LoadFromMemoryAsync(AssemblyExtensions.GetBytesFromResources("Resources.paleaudio"));
        }

        if (loadRequest == null)
        {
            Instance.LogError("Hallownest Vocalized - Pale Court audio not loaded. no audio bundle was found neither embedded nor as an external file placed next to the dll");
            return;
        }

        HKVocals.AudioAPI.AddAudioProvider(0, new AssetBundleCreateRequestAudioProvider(loadRequest, true));
        foreach (string s in ClipsToMute)
            HKVocals.AudioAPI.AddMuteAudio(s);
    }

    public override string GetVersion() => GetType().Assembly.GetName().Version.ToString() + (MainModExists ? "" : "Error Hallownest Vocalized not present");
    
    private static readonly List<string> ClipsToMute = new ()
    {
        "DryyaVoiceConvo1",
        "DryyaVoiceConvo2",
        "DryyaVoiceConvo3",
        "DTalk1",
        "DTalk2",
        "DTalk3",
        "DTalk4",
        "DTalk5",
        "DTalk6",
        "DTalk7",
        "IsmaAudTalkHi",
        "IsmaAudTalk1",
        "IsmaAudTalkCharm",
        "IsmaAudTalkBye",
        "IsmaAudTalk3", 
        "IsmaAudTalk5",
        "IsmaAudTalk6",
        "Nailmsith_talk_03",
        "Nailmsith_talk_04",
        "Nailmsith_talk_05",
        "ZAudTalk1",
        "ZAudTalk2",
        "ZAudTalk3",
        "ZAudTalk4",
        "ZAudTalk1B",
        "sheo_1",
        "sheo_2",
        "smith_1",
        "smith_2",
    };
}