using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BuildingCutaway : MonoBehaviour
{
    public enum HideMode { Fade, HideCompletely }

    [Header("The part that blocks the diagonal view: the 2 walls + roof (NOT the main building)")]
    public GameObject sideAndRoof;

    [Header("Behaviour")]
    public HideMode mode = HideMode.Fade;
    [Range(0f, 1f)] public float hiddenBelowZoom = 0.4f;   // zoomLevel at or below this = fully hidden (zoomed in close)
    [Range(0f, 1f)] public float visibleAboveZoom = 0.65f; // zoomLevel at or above this = fully visible
    public float fadeSpeed = 4f;                            // visibility units per second (4 = about a quarter of a second)
    public bool keepShadowsWhenHidden = false;

    [Tooltip("Optional, for builds only. Any material with the same shader set to a TRANSPARENT surface. It is never used; it only makes the build keep the transparent shader variant.")]
    public Material transparentVariantKeeper;

    [Header("Set at runtime by GameplaySceneBootstrapper")]
    public GameplayCameraController gameplayCamera;
    public CameraModeManager cameraModeManager;

    class Part
    {
        public Renderer renderer;
        public bool originalEnabled;
        public ShadowCastingMode originalShadows;
        public Material[] originals;  // the real shared materials - never modified
        public Material[] fades;      // runtime transparent copies (Fade mode only)
        public Color[] baseColors;
    }

    enum State { Opaque, Fading, Hidden }

    readonly List<Part> parts = new();
    float visibility = 1f;   // 1 = fully visible, 0 = fully hidden
    bool hiddenLatch;        // hysteresis for HideCompletely so it can't flicker around the threshold
    State applied = State.Opaque;
    bool firstApply = true;

    void Awake()
    {
        if (sideAndRoof == null)
        {
            Debug.LogWarning("BuildingCutaway has no Side And Roof assigned - nothing will be hidden.", this);
            enabled = false;
            return;
        }

        foreach (Renderer r in sideAndRoof.GetComponentsInChildren<Renderer>(true))
        {
            var part = new Part
            {
                renderer = r,
                originalEnabled = r.enabled,
                originalShadows = r.shadowCastingMode,
                originals = r.sharedMaterials
            };

            if (mode == HideMode.Fade)
            {
                part.fades = new Material[part.originals.Length];
                part.baseColors = new Color[part.originals.Length];
                for (int i = 0; i < part.originals.Length; i++)
                {
                    if (part.originals[i] == null) continue;

                    Material copy = new Material(part.originals[i]);
                    MakeTransparent(copy);
                    part.fades[i] = copy;
                    part.baseColors[i] = copy.HasProperty("_BaseColor") ? copy.GetColor("_BaseColor") : Color.white;
                }
            }
            parts.Add(part);
        }
    }

    void OnDestroy()
    {
        foreach (Part p in parts)
            if (p.fades != null)
                foreach (Material m in p.fades)
                    if (m != null) Destroy(m);
    }

    // 1 = show the whole building, 0 = hide the walls/roof, in between = partly transparent.
    float TargetVisibility()
    {
        if (cameraModeManager != null && cameraModeManager.ShowFullBuilding) return 1f;
        if (gameplayCamera == null) return 1f;
        return Mathf.InverseLerp(hiddenBelowZoom, visibleAboveZoom, gameplayCamera.ZoomLevel);
    }

    void Update()
    {
        float target = TargetVisibility();

        if (mode == HideMode.HideCompletely)
        {
            if (target <= 0f) hiddenLatch = true;        // zoomed in close enough
            else if (target >= 1f) hiddenLatch = false;  // zoomed out enough, or first person
            visibility = hiddenLatch ? 0f : 1f;
        }
        else
        {
            visibility = Mathf.MoveTowards(visibility, target, fadeSpeed * Time.deltaTime);
        }

        Apply();
    }

    void Apply()
    {
        State wanted = visibility >= 0.999f ? State.Opaque : (visibility <= 0.001f ? State.Hidden : State.Fading);

        if (wanted != applied || firstApply)
        {
            foreach (Part p in parts) EnterState(p, wanted);
            applied = wanted;
            firstApply = false;
        }

        if (wanted == State.Fading)
            foreach (Part p in parts) SetAlpha(p, visibility);
    }

    void EnterState(Part p, State state)
    {
        switch (state)
        {
            case State.Opaque:
                p.renderer.enabled = p.originalEnabled;
                p.renderer.sharedMaterials = p.originals;
                p.renderer.shadowCastingMode = p.originalShadows;
                break;

            case State.Fading:
                p.renderer.enabled = p.originalEnabled;
                p.renderer.sharedMaterials = p.fades;
                p.renderer.shadowCastingMode = p.originalShadows;
                break;

            case State.Hidden:
                if (keepShadowsWhenHidden)
                {
                    p.renderer.enabled = p.originalEnabled;
                    p.renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly; // invisible but still casts shadows
                }
                else
                {
                    p.renderer.enabled = false;
                }
                break;
        }
    }

    static void SetAlpha(Part p, float alpha)
    {
        for (int i = 0; i < p.fades.Length; i++)
        {
            Material m = p.fades[i];
            if (m == null || !m.HasProperty("_BaseColor")) continue;

            Color c = p.baseColors[i];
            c.a *= alpha;
            m.SetColor("_BaseColor", c);
        }
    }

    // The same settings URP's own material inspector applies when you set Surface Type to Transparent.
    // SetFloat on a property the shader doesn't have is harmless, so this is safe across URP versions.
    static void MakeTransparent(Material m)
    {
        m.SetFloat("_Surface", 1f); // 0 = opaque, 1 = transparent
        m.SetFloat("_Blend", 0f);   // alpha blending
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
    }
}