using Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

#pragma warning disable CS8618
namespace HallownestVocalizedAudioLoader;

[PublicAPI]
public class HallownestVocalizedAudioLoaderMod : Mod
{
    public static HallownestVocalizedAudioLoaderMod Instance;
    private static AssetBundle _audioBundle;
    private static bool AudioLoadSuccess = false;
    public static List<string> AudioNames { get; } = new();
    public static bool MainModExists => ModHooks.GetMod("Hallownest Vocalized") is Mod;
    public static AssetBundle AudioBundle
    {
        get
        {
            if (!AudioLoadSuccess)
            {
                throw new Exception("Audiobundle has not loaded successfully yet");
            }

            return _audioBundle;
        }
        set => _audioBundle = value;
    }

    public HallownestVocalizedAudioLoaderMod() : base("Hallownest Vocalized AudioLoader")
    {
        Instance = this;
    }

    public override void Initialize()
    {
        //all mods are added to ModInstanceNameMap before any Inits are called. So we can safely
        //assume that if the check returns false, there is no main mod and no need to load any audio
        if (MainModExists)
        {
            Log("Loading Hallownest Vocalized Audio");
            LoadAssetBundle();
        }
        else
        {
            Log("Did not load Hallownest Vocalized Audio");
        }
    }

    private static void LoadAssetBundle()
    {
        AudioBundle = AssetBundle.LoadFromMemory(AssemblyExtensions.GetBytesFromResources("Resources.audiobundle")) 
                      ?? AssetBundle.LoadFromFile(AssemblyExtensions.GetCurrentDirectory() + "/audiobundle");

        if (AudioBundle == null)
        {
            Instance.LogError("Hallownest Vocalized audio not loaded");
            return;
        }

        foreach (var audio in AudioBundle.GetAllAssetNames())
        {
            if (new []{".mp3",".wav"}.Any(extension => audio.EndsWith(extension)))
            {
                AudioNames.Add(Path.GetFileNameWithoutExtension(audio).ToUpper());
                Instance.LogDebug($"Loaded audio: {audio}");
            }
        }

        AudioLoadSuccess = true;
    }

    public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();
}