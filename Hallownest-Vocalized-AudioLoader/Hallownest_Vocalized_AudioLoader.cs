using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Modding.Menu;
using UnityEngine;
using UnityEngine.UI;
using Satchel;
using Object = UnityEngine.Object;
using HKVocals;
using HKMirror.Hooks.OnHooks;

#pragma warning disable CS8618
namespace HallownestVocalizedAudioLoader;

[PublicAPI]
public class HallownestVocalizedAudioLoaderMod : Mod
{
    public static HallownestVocalizedAudioLoaderMod Instance;
    public static AssetBundle audioBundle;
    public static AssetBundle styleBundle;
    public static AssetBundle creditsBundle;
    //private static bool AudioLoadSuccess = false;
    //public static List<string> AudioNames { get; } = new();
    public static bool MainModExists => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "HKVocal");
    private static AssetBundleCreateRequest loadRequest;

    public static NonBouncer CoroutineHolder;
    private static GameObject TextCanvas;
    private static Text TextPanelText;
    private const string WaitText = "Please wait while Hallownest Vocalized audio is loading...";

    public HallownestVocalizedAudioLoaderMod() : base("Hallownest Vocalized AudioLoader")
    {
        Instance = this;
        
        //although we can't guarantee main mod exists, i feel its a worth it trade off
        //to load the bundle async during preloads and having to unload the bundle if mod doesnt exist
        //instead of loading it async after preloads and wasting all that time
        //if some idiot has the audio without main mod its their loss ig

        //actually we can tell if the mod exists - frog

        if (!MainModExists) return;

        OnMenuStyleTitle.AfterOrig.SetTitle += AddCustomBanner;
        SFCore.MenuStyleHelper.AddMenuStyleHook += MajorFeatures.MenuTheme.AddTheme;

        HKVocals.HKVocals.Icon = Satchel.AssemblyUtils.GetSpriteFromResources("Resources.icon.png");

        CoroutineHolder = new GameObject().AddComponent<NonBouncer>();
        Object.DontDestroyOnLoad(CoroutineHolder);

        Instance.Log("Starting Hallownest Vocalised Misc. Load");
        styleBundle = AssetBundle.LoadFromMemory(SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux ? AssemblyUtils.GetBytesFromResources("Resources.stylebundlelinux") : AssemblyUtils.GetBytesFromResources("Resources.stylebundle"));
        creditsBundle = AssetBundle.LoadFromMemory(AssemblyUtils.GetBytesFromResources("Resources.creditsbundle"));
        
        Instance.Log("Starting Hallownest Vocalised Audio Load");
        LoadAssetBundle();
    }

    public override void Initialize()
    {
        //no need to hook if main mod doesnt exist
        if (MainModExists)
        {
            On.GameManager.StartNewGame += StopStartNewGame;
            On.GameManager.ContinueGame += StopContinueGame;
            UIManager.EditMenus += UI.ExtrasMenu.AddCreditsButton;

            if (!HKVocals.HKVocals._globalSettings.ForceMenuTheme)
            {
                HKVocals.HKVocals._globalSettings.ForceMenuTheme = true;
                var tmpStyle = MenuStyles.Instance.styles.First(x => x.styleObject.name.Contains("HKVStyle"));
                MenuStyles.Instance.SetStyle(MenuStyles.Instance.styles.ToList().IndexOf(tmpStyle), false);
            }
        }
        else
        {
            LogError("You dont have hallownest vocalized audio without the main hallownest vocalized mod. please download that");
            if (loadRequest == null) return;

            
            //we dont want to unnecessarily keep 200MB of RAM captive if main mod doesnt exist
            /*Log("Unloading assetbundle because main mod doesnt exist");
        
            //either of them can be true only if the complete event has already run
            if (loadRequest.isDone || AudioLoadSuccess)
            {
                _audioBundle.Unload(true);
                AudioLoadSuccess = false;
                Log("Sucessfully freed up ram");
            }
            else
            {
                //since its not done, we can't remove the original compeleted event and insert a new one
                //that will unload it
                loadRequest.completed -= SaveLoadedBundle;
                loadRequest.completed += opp =>
                {
                    _audioBundle = loadRequest.assetBundle;
                    _audioBundle.Unload(true);
                    AudioLoadSuccess = false;
                };
            }*/
        }
    }

    private void StopContinueGame(On.GameManager.orig_ContinueGame orig, GameManager self)
    {
        CoroutineHolder.StartCoroutine(BlockContinueGame(orig, self));
    }

    private void StopStartNewGame(On.GameManager.orig_StartNewGame orig, GameManager self, bool permadeathmode, bool bossrushmode)
    {
        CoroutineHolder.StartCoroutine(BlockStartNewGame(orig, self, permadeathmode, bossrushmode));
    }

    private IEnumerator BlockContinueGame(On.GameManager.orig_ContinueGame orig, GameManager self)
    {
        yield return WaitForBundleToLoad();
        orig(self);
    }
    private IEnumerator BlockStartNewGame(On.GameManager.orig_StartNewGame orig, GameManager self, bool p, bool b)
    {
        yield return WaitForBundleToLoad();
        orig(self, p, b);
    }

    private IEnumerator WaitForBundleToLoad()
    {
        if (loadRequest == null || loadRequest.isDone) yield break;
        
        CreateTextPanel();
        string prevText = "";
        while (!loadRequest.isDone)
        {
            yield return null;
            string newText = WaitText + $"({Math.Round(loadRequest.progress, 1)*100}%)";
            if (newText != prevText)
            {
                TextPanelText.text = newText;
                prevText = newText;
            }
        }
        Object.Destroy(TextCanvas);
    }
    

    private static void LoadAssetBundle()
    {
        //if someone for some reason has an audiobundle, prioritize that instead of the embedded one
        if (File.Exists(AssemblyExtensions.GetCurrentDirectory() + "/audiobundle"))
        {
            loadRequest = AssetBundle.LoadFromFileAsync(AssemblyExtensions.GetCurrentDirectory() + "/audiobundle");
        }
        else
        {
            loadRequest = AssetBundle.LoadFromMemoryAsync(AssemblyExtensions.GetBytesFromResources("Resources.audiobundle"));
        }

        if (loadRequest == null)
        {
            Instance.LogError("Hallownest Vocalized audio not loaded. no audio bundle was found neither embedded nor as an external file placed next to the dll");
            return;
        }

        HKVocals.AudioAPI.AddAudioProvider(-1, new AssetBundleCreateRequestAudioProvider(loadRequest, true));
        foreach (var audio in loadRequest.assetBundle.GetAllAssetNames())
        {
            if (new[] { ".mp3", ".wav" }.Any(extension => audio.EndsWith(extension)))
            {
                HKVocals.AudioAPI.AudioNames.Add(Path.GetFileNameWithoutExtension(audio).ToUpper());
                Instance.LogDebug($"Loaded audio: {audio}");
            }
        }
        foreach (string s in ClipsToMute)
            HKVocals.AudioAPI.AddMuteAudio(s);
        
        //we can't check here for main mod existence because it is not guaranteed to happen after mod inits
        //even tho realistically it probably will happen after mod inits
        loadRequest.completed += SaveLoadedBundle;
    }
    
    private void AddCustomBanner(OnMenuStyleTitle.Delegates.Params_SetTitle args)
    {
        //only change for english language. i doubt people on other languages want it
        //if (Language.Language.CurrentLanguage() == LanguageCode.EN)
        {
            if (MainModExists)
            {
                args.self.Title.sprite = AssemblyUtils.GetSpriteFromResources(UnityEngine.Random.Range(1,1000) == 1 && HKVocals.HKVocals._globalSettings.settingsOpened 
                    ? "Resources.Title_alt.png" 
                    : "Resources.Title.png");
            }
            else
            {
                args.self.Title.sprite = AssemblyUtils.GetSpriteFromResources("Resources.Title_missingDeps.png");
            }
        }
        
    }

    public static void SaveLoadedBundle(AsyncOperation operation)
    {
        audioBundle = loadRequest.assetBundle;
        /*foreach (var audio in _audioBundle.GetAllAssetNames())
        {
            if (new[] { ".mp3", ".wav" }.Any(extension => audio.EndsWith(extension)))
            {
                AudioNames.Add(Path.GetFileNameWithoutExtension(audio).ToUpper());
                Instance.LogDebug($"Loaded audio: {audio}");
            }
        }

        AudioLoadSuccess = true;*/
        Instance.Log("Sucessfully loaded hallownest vocalized audio");
    }

    public static void CreateTextPanel()
    {
        TextCanvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
        TextCanvas.name = "Hallownest Vocalized Wait message";
        CanvasGroup cg = TextCanvas.GetComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        GameObject background = CanvasUtil.CreateImagePanel
        (
            TextCanvas,
            CanvasUtil.NullSprite(new byte[] {0x80, 0x00, 0x00, 0x00}),
            new CanvasUtil.RectData(Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one)
        );

        var TextPanel = CanvasUtil.CreateTextPanel
        (
            background,
            WaitText,
            60,
            TextAnchor.MiddleCenter,
            new CanvasUtil.RectData(new Vector2(-5, -5), Vector2.zero, Vector2.zero, Vector2.one),
            MenuResources.Perpetua
        );

        TextPanelText = TextPanel.GetComponent<Text>();
    }

    public override string GetVersion() => GetType().Assembly.GetName().Version.ToString() + (MainModExists ? "" : "Error Hallownest Vocalized not present");
    
    private static readonly List<string> ClipsToMute = new ()
    {
        "Salubra_Laugh_Loop",
        "Sly_talk",
        "Sly_talk_02",
        "Sly_talk_03",
        "Sly_talk_04",
        "Sly_talk_05",
        "Banker_talk_01",
        "Stag_ambient_loop",
        "junk_fluke_long_loop",
        "junk_fluke_long_loop_nervous",
        "Moss_Cultist_Loop",
        "Grimm_talk_02",
        "Grimm_talk_03",
        "Grimm_talk_05",
        "Grimm_talk_06", 
        "Nailmsith_greet",
        "Nailmsith_talk_02",
        "Nailmsith_talk_03",
        "Nailmsith_talk_04",
        "Nailmsith_talk_05",
        "Hornet_Dialogue_Generic_02",
        "Hornet_Dialogue_Generic_03",
        "Hornet_Dialogue_Generic_04",
        "Hornet_Dialogue_Generic_05",
        "Bow_Repeat",
        "WD_outro",
        "Stag_02",
        "Hornet_Greenpath_01",
        "Salubra_Talk",
        "Mr_Mush_talk_03",
        "GS_standard_01",
        "GS_standard_02",
        "GS_standard_05",
        "GS_standard_06",
        "GS_standard_07",
        "GS_engine_room",
        "GS_engine_room_03",
        "GS_engine_room_04",
        "Hunter_journal_02",
        "Relic_Dealer_04",
    };
}