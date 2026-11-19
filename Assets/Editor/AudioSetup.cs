using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Herramienta de configuracion del sistema de audio 2D (Actividad 08).
// Menu: Tools > Audio 2D > ...
public static class AudioSetup
{
    const string BGM_DIR = "Assets/Audio/BGM";
    const string SFX_DIR = "Assets/Audio/SFX";
    const string MIXER_PATH = "Assets/Audio/MainAudioMixer.mixer";

    // ---------------- Paso 1: importacion ----------------
    [MenuItem("Tools/Audio 2D/1 - Configurar importacion de clips")]
    public static void ConfigureImport()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { BGM_DIR }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (AudioImporter)AssetImporter.GetAtPath(p);
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            imp.defaultSampleSettings = s;
            imp.preloadAudioData = false;
            imp.loadInBackground = true;
            imp.SaveAndReimport();
            Debug.Log("[AudioSetup] BGM " + p + " -> Streaming / Vorbis");
        }

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SFX_DIR }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (AudioImporter)AssetImporter.GetAtPath(p);
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.ADPCM;
            imp.defaultSampleSettings = s;
            imp.preloadAudioData = true;
            imp.forceToMono = true;
            imp.SaveAndReimport();
            Debug.Log("[AudioSetup] SFX " + p + " -> Decompress On Load / ADPCM");
        }
    }

    // ---------------- Paso 2: AudioMixer ----------------
    [MenuItem("Tools/Audio 2D/2 - Crear MainAudioMixer")]
    public static void CreateMixer()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER_PATH);
        if (existing != null)
        {
            if (existing.FindMatchingGroups("Music").Length > 0 && existing.FindMatchingGroups("SFX").Length > 0)
            {
                Debug.Log("[AudioSetup] MainAudioMixer ya existe.");
                return;
            }
            AssetDatabase.DeleteAsset(MIXER_PATH); // mixer incompleto: se recrea
        }

        const BindingFlags F = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Assembly ed = typeof(Editor).Assembly;
        Type tCtrl = ed.GetType("UnityEditor.Audio.AudioMixerController");
        Type tGroup = ed.GetType("UnityEditor.Audio.AudioMixerGroupController");
        Type tPath = ed.GetType("UnityEditor.Audio.AudioGroupParameterPath");

        object ctrl = tCtrl.GetMethod("CreateMixerControllerAtPath", F).Invoke(null, new object[] { MIXER_PATH });
        object master = tCtrl.GetProperty("masterGroup", F).GetValue(ctrl, null);
        object snapshot = tCtrl.GetProperty("TargetSnapshot", F).GetValue(ctrl, null);

        object music = AddGroup(tCtrl, ctrl, master, "Music", F);
        object sfx = AddGroup(tCtrl, ctrl, master, "SFX", F);
        CreateView(tCtrl, tGroup, ctrl, new[] { master, music, sfx }, F);

        // Balance inicial: Musica -4 dB, SFX -2 dB, Master 0 dB (headroom para evitar clipping)
        SetVolume(tGroup, music, ctrl, snapshot, -4f, F);
        SetVolume(tGroup, sfx, ctrl, snapshot, -2f, F);

        Expose(tCtrl, tGroup, tPath, ctrl, master, "MasterVolume", F);
        Expose(tCtrl, tGroup, tPath, ctrl, music, "MusicVolume", F);
        Expose(tCtrl, tGroup, tPath, ctrl, sfx, "SFXVolume", F);

        EditorUtility.SetDirty((UnityEngine.Object)ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AudioSetup] MainAudioMixer creado (Master > Music, SFX) con volumenes expuestos.");
    }

    static object AddGroup(Type tCtrl, object ctrl, object parent, string name, BindingFlags F)
    {
        object g = tCtrl.GetMethod("CreateNewGroup", F).Invoke(ctrl, new object[] { name, false });
        tCtrl.GetMethod("AddChildToParent", F).Invoke(ctrl, new object[] { g, parent });
        return g;
    }

    // Crea la vista "View" del Mixer con todos los grupos visibles
    static void CreateView(Type tCtrl, Type tGroup, object ctrl, object[] groups, BindingFlags F)
    {
        Type tView = typeof(Editor).Assembly.GetType("UnityEditor.Audio.MixerGroupView");
        object view = Activator.CreateInstance(tView);
        GUID[] guids = new GUID[groups.Length];
        for (int i = 0; i < groups.Length; i++)
            guids[i] = (GUID)tGroup.GetProperty("groupID", F).GetValue(groups[i], null);
        tView.GetField("guids", F).SetValue(view, guids);
        tView.GetField("name", F).SetValue(view, "View");
        Array views = Array.CreateInstance(tView, 1);
        views.SetValue(view, 0);
        tCtrl.GetProperty("views", F).SetValue(ctrl, views, null);
        PropertyInfo cur = tCtrl.GetProperty("currentViewIndex", F);
        if (cur != null) cur.SetValue(ctrl, 0, null);
    }

    static void SetVolume(Type tGroup, object group, object ctrl, object snapshot, float dB, BindingFlags F)
    {
        MethodInfo m = tGroup.GetMethod("SetValueForVolume", F);
        if (m != null) m.Invoke(group, new object[] { ctrl, snapshot, dB });
    }

    static void Expose(Type tCtrl, Type tGroup, Type tPath, object ctrl, object group, string paramName, BindingFlags F)
    {
        object guid = tGroup.GetMethod("GetGUIDForVolume", F).Invoke(group, null);
        object path = Activator.CreateInstance(tPath, F, null, new object[] { group, guid }, null);
        tCtrl.GetMethod("AddExposedParameter", F).Invoke(ctrl, new object[] { path });

        // Renombrar el parametro expuesto
        PropertyInfo pExp = tCtrl.GetProperty("exposedParameters", F);
        Array arr = (Array)pExp.GetValue(ctrl, null);
        for (int i = 0; i < arr.Length; i++)
        {
            object e = arr.GetValue(i);
            FieldInfo fGuid = e.GetType().GetField("guid", F);
            FieldInfo fName = e.GetType().GetField("name", F);
            if (fGuid.GetValue(e).Equals(guid))
            {
                fName.SetValue(e, paramName);
                arr.SetValue(e, i);
            }
        }
        pExp.SetValue(ctrl, arr, null);
    }

    // ---------------- Paso 3: AudioManager + UI en escena ----------------
    [MenuItem("Tools/Audio 2D/3 - Crear AudioManager y boton Pausa en escena")]
    public static void SetupScene()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER_PATH);
        if (mixer == null) { Debug.LogError("[AudioSetup] Primero crea el MainAudioMixer (paso 2)."); return; }

        AudioMixerGroup musicGroup = mixer.FindMatchingGroups("Music")[0];
        AudioMixerGroup sfxGroup = mixer.FindMatchingGroups("SFX")[0];

        // AudioManager
        GameObject am = GameObject.Find("AudioManager");
        if (am == null) am = new GameObject("AudioManager");
        Undo.RegisterCreatedObjectUndo(am, "AudioManager");

        AudioSource musicSrc = GetOrCreateSource(am.transform, "MusicSource", musicGroup);
        musicSrc.loop = true;
        musicSrc.volume = 0.6f;

        AudioSource sfxSrc = GetOrCreateSource(am.transform, "SFXSource", sfxGroup);
        sfxSrc.loop = false;
        sfxSrc.volume = 1f;

        AudioManager mgr = am.GetComponent<AudioManager>();
        if (mgr == null) mgr = am.AddComponent<AudioManager>();
        mgr.musicSource = musicSrc;
        mgr.sfxSource = sfxSrc;
        mgr.mainMixer = mixer;
        mgr.bgmNivel01 = Clip(BGM_DIR + "/bgm_nivel01.wav");
        mgr.sfxJump = Clip(SFX_DIR + "/sfx_jump.wav");
        mgr.sfxLand = Clip(SFX_DIR + "/sfx_land.wav");
        mgr.sfxCoin = Clip(SFX_DIR + "/sfx_coin.wav");
        mgr.sfxDamage = Clip(SFX_DIR + "/sfx_damage.wav");
        mgr.sfxEnemyDefeat = Clip(SFX_DIR + "/sfx_enemy_defeat.wav");
        mgr.sfxUISelect = Clip(SFX_DIR + "/sfx_ui_select.wav");
        EditorUtility.SetDirty(mgr);

        // UI: Canvas + EventSystem + boton de pausa
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.layer = LayerMask.NameToLayer("UI");
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            Undo.RegisterCreatedObjectUndo(cgo, "Canvas");
        }
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        }

        if (GameObject.Find("PauseButton") == null)
        {
            var bgo = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PauseButton));
            bgo.layer = LayerMask.NameToLayer("UI");
            bgo.transform.SetParent(canvas.transform, false);
            var rt = bgo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(180, 56);
            bgo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

            var tgo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tgo.layer = bgo.layer;
            tgo.transform.SetParent(bgo.transform, false);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var txt = tgo.GetComponent<Text>();
            txt.text = "PAUSA";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            var pb = bgo.GetComponent<PauseButton>();
            pb.label = txt;
            UnityEventTools.AddPersistentListener(bgo.GetComponent<Button>().onClick, pb.TogglePause);
            Undo.RegisterCreatedObjectUndo(bgo, "PauseButton");
        }

        // Verificar un unico AudioListener
        var listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>();
        Debug.Log("[AudioSetup] AudioListeners en escena: " + listeners.Length);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[AudioSetup] Escena configurada y guardada.");
    }

    static AudioSource GetOrCreateSource(Transform parent, string name, AudioMixerGroup group)
    {
        Transform t = parent.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null) src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        src.outputAudioMixerGroup = group;
        return src;
    }

    static AudioClip Clip(string path)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (c == null) Debug.LogError("[AudioSetup] No se encontro " + path);
        return c;
    }
}
