using UnityEngine;

public class ThemeData
{
    public string name;

    // Track grid
    public Color trackBase;
    public Color gridColor1;  // Major lines
    public Color gridColor2;  // Minor lines
    public Color gridColor3;  // Accent
    public Color gridFarColor;
    public Color sparkColor1;
    public Color sparkColor2;
    public Color sparkColor3;

    // Gates
    public Color gateFrontColor;
    public Color gateFrontEmission;
    public Color gateTopColor;
    public Color gateTopEmission;

    // Ball
    public Color ballColor;
    public Color ballRimColor;
    public Color ballEmissionBase;

    // Rails
    public Color railLeftNear;
    public Color railLeftFar;
    public Color railLeftEmissionNear;
    public Color railLeftEmissionFar;
    public Color railRightNear;
    public Color railRightFar;
    public Color railRightEmissionNear;
    public Color railRightEmissionFar;

    // Nebula
    public Color nebulaColor1;
    public Color nebulaColor2;
    public Color nebulaColor3;
    public float nebulaBrightness;

    // God rays
    public Color rayColor1;
    public Color rayColor2;
    public float rayIntensity;

    // Stars — 4 accent colors + default base
    public Color starAccent1; // Matches gate
    public Color starAccent2; // Matches ball/UI
    public Color starAccent3; // Matches left rail
    public Color starAccent4; // Matches right rail
    public Color starBase;    // Default white-ish

    // Ball trail
    public Color trailColorNear;  // Bright, close to ball
    public Color trailColorFar;   // Dim, fading out behind

    // Camera background
    public Color cameraBg;

    // ── THEME PRESETS ────────────────────────────────────────

    public static ThemeData NeonVoid() => new ThemeData
    {
        name = "NEON VOID",
        trackBase      = new Color(0.14f, 0.13f, 0.15f),
        gridColor1     = new Color(0.0f, 0.45f, 0.7f, 0.55f),
        gridColor2     = new Color(0.8f, 0.15f, 0.5f, 0.5f),
        gridColor3     = new Color(0.85f, 0.65f, 0.1f, 0.35f),
        gridFarColor   = new Color(0.2f, 0.05f, 0.3f, 0.25f),
        sparkColor1    = new Color(0.2f, 0.8f, 1.0f),
        sparkColor2    = new Color(1.0f, 0.4f, 0.8f),
        sparkColor3    = new Color(0.3f, 1.0f, 0.5f),
        gateFrontColor    = new Color(1.0f, 0.6f, 0.15f),
        gateFrontEmission = new Color(0.9f, 0.4f, 0.08f),
        gateTopColor      = new Color(1.0f, 0.75f, 0.25f),
        gateTopEmission   = new Color(1.0f, 0.55f, 0.1f),
        ballColor        = new Color(0.9f, 0.92f, 1.0f),
        ballRimColor     = new Color(0.0f, 0.75f, 1.0f),
        ballEmissionBase = new Color(0.15f, 0.15f, 0.2f),
        railLeftNear         = new Color(0.95f, 0.1f, 0.55f),
        railLeftFar          = new Color(0.25f, 0.05f, 0.35f),
        railLeftEmissionNear = new Color(0.55f, 0.03f, 0.3f),
        railLeftEmissionFar  = new Color(0.08f, 0.01f, 0.12f),
        railRightNear         = new Color(0.2f, 0.9f, 0.4f),
        railRightFar          = new Color(0.05f, 0.2f, 0.35f),
        railRightEmissionNear = new Color(0.05f, 0.45f, 0.15f),
        railRightEmissionFar  = new Color(0.01f, 0.06f, 0.12f),
        nebulaColor1    = new Color(0.06f, 0.02f, 0.12f),
        nebulaColor2    = new Color(0.02f, 0.08f, 0.14f),
        nebulaColor3    = new Color(0.10f, 0.02f, 0.08f),
        nebulaBrightness = 0.7f,
        rayColor1    = new Color(0.08f, 0.03f, 0.18f),
        rayColor2    = new Color(0.03f, 0.10f, 0.18f),
        rayIntensity = 0.35f,
        starAccent1 = new Color(1.0f, 0.85f, 0.3f),
        starAccent2 = new Color(0.3f, 0.8f, 1.0f),
        starAccent3 = new Color(0.9f, 0.3f, 0.6f),
        starAccent4 = new Color(0.3f, 0.9f, 0.5f),
        starBase    = new Color(0.6f, 0.7f, 0.95f),
        trailColorNear = new Color(0.2f, 0.85f, 1.0f, 0.9f),
        trailColorFar  = new Color(0.1f, 0.25f, 0.6f, 0.15f),
        cameraBg = new Color(0.05f, 0.05f, 0.08f),
    };

    public static ThemeData SolarFlare() => new ThemeData
    {
        name = "SOLAR FLARE",
        trackBase      = new Color(0.15f, 0.10f, 0.08f),
        gridColor1     = new Color(0.9f, 0.4f, 0.05f, 0.55f),
        gridColor2     = new Color(0.7f, 0.15f, 0.05f, 0.45f),
        gridColor3     = new Color(1.0f, 0.75f, 0.2f, 0.3f),
        gridFarColor   = new Color(0.25f, 0.08f, 0.02f, 0.25f),
        sparkColor1    = new Color(1.0f, 0.6f, 0.1f),
        sparkColor2    = new Color(1.0f, 0.85f, 0.3f),
        sparkColor3    = new Color(1.0f, 0.3f, 0.05f),
        gateFrontColor    = new Color(0.3f, 0.85f, 1.0f),
        gateFrontEmission = new Color(0.15f, 0.55f, 0.8f),
        gateTopColor      = new Color(0.45f, 0.9f, 1.0f),
        gateTopEmission   = new Color(0.25f, 0.65f, 0.85f),
        ballColor        = new Color(1.0f, 0.95f, 0.85f),
        ballRimColor     = new Color(1.0f, 0.5f, 0.0f),
        ballEmissionBase = new Color(0.2f, 0.1f, 0.05f),
        railLeftNear         = new Color(1.0f, 0.3f, 0.05f),
        railLeftFar          = new Color(0.35f, 0.08f, 0.02f),
        railLeftEmissionNear = new Color(0.6f, 0.15f, 0.02f),
        railLeftEmissionFar  = new Color(0.12f, 0.03f, 0.01f),
        railRightNear         = new Color(1.0f, 0.75f, 0.15f),
        railRightFar          = new Color(0.3f, 0.15f, 0.02f),
        railRightEmissionNear = new Color(0.5f, 0.35f, 0.05f),
        railRightEmissionFar  = new Color(0.1f, 0.05f, 0.01f),
        nebulaColor1    = new Color(0.12f, 0.03f, 0.01f),
        nebulaColor2    = new Color(0.08f, 0.04f, 0.01f),
        nebulaColor3    = new Color(0.15f, 0.05f, 0.02f),
        nebulaBrightness = 0.8f,
        rayColor1    = new Color(0.18f, 0.06f, 0.01f),
        rayColor2    = new Color(0.15f, 0.10f, 0.02f),
        rayIntensity = 0.4f,
        starAccent1 = new Color(1.0f, 0.7f, 0.2f),
        starAccent2 = new Color(1.0f, 0.5f, 0.1f),
        starAccent3 = new Color(1.0f, 0.3f, 0.1f),
        starAccent4 = new Color(1.0f, 0.85f, 0.4f),
        starBase    = new Color(0.95f, 0.8f, 0.6f),
        trailColorNear = new Color(0.2f, 0.7f, 1.0f, 0.9f),
        trailColorFar  = new Color(0.05f, 0.15f, 0.4f, 0.15f),
        cameraBg = new Color(0.06f, 0.03f, 0.02f),
    };

    public static ThemeData DeepAbyss() => new ThemeData
    {
        name = "DEEP ABYSS",
        trackBase      = new Color(0.06f, 0.10f, 0.14f),
        gridColor1     = new Color(0.0f, 0.5f, 0.55f, 0.45f),
        gridColor2     = new Color(0.0f, 0.3f, 0.4f, 0.35f),
        gridColor3     = new Color(0.15f, 0.7f, 0.4f, 0.25f),
        gridFarColor   = new Color(0.02f, 0.08f, 0.15f, 0.3f),
        sparkColor1    = new Color(0.1f, 0.8f, 0.7f),
        sparkColor2    = new Color(0.0f, 0.5f, 0.9f),
        sparkColor3    = new Color(0.2f, 0.9f, 0.4f),
        gateFrontColor    = new Color(1.0f, 0.45f, 0.35f),
        gateFrontEmission = new Color(0.7f, 0.25f, 0.15f),
        gateTopColor      = new Color(1.0f, 0.6f, 0.5f),
        gateTopEmission   = new Color(0.8f, 0.35f, 0.25f),
        ballColor        = new Color(0.85f, 0.95f, 0.95f),
        ballRimColor     = new Color(0.0f, 0.8f, 0.65f),
        ballEmissionBase = new Color(0.05f, 0.15f, 0.18f),
        railLeftNear         = new Color(0.0f, 0.6f, 0.8f),
        railLeftFar          = new Color(0.02f, 0.12f, 0.25f),
        railLeftEmissionNear = new Color(0.0f, 0.3f, 0.45f),
        railLeftEmissionFar  = new Color(0.01f, 0.04f, 0.1f),
        railRightNear         = new Color(0.15f, 0.85f, 0.45f),
        railRightFar          = new Color(0.03f, 0.2f, 0.12f),
        railRightEmissionNear = new Color(0.05f, 0.4f, 0.2f),
        railRightEmissionFar  = new Color(0.01f, 0.08f, 0.04f),
        nebulaColor1    = new Color(0.01f, 0.04f, 0.10f),
        nebulaColor2    = new Color(0.02f, 0.08f, 0.12f),
        nebulaColor3    = new Color(0.01f, 0.06f, 0.08f),
        nebulaBrightness = 0.5f,
        rayColor1    = new Color(0.02f, 0.08f, 0.15f),
        rayColor2    = new Color(0.01f, 0.12f, 0.10f),
        rayIntensity = 0.25f,
        starAccent1 = new Color(0.2f, 0.9f, 0.7f),
        starAccent2 = new Color(0.1f, 0.6f, 0.9f),
        starAccent3 = new Color(0.2f, 0.85f, 0.4f),
        starAccent4 = new Color(0.0f, 0.7f, 0.8f),
        starBase    = new Color(0.5f, 0.75f, 0.8f),
        trailColorNear = new Color(1.0f, 0.55f, 0.15f, 0.9f),
        trailColorFar  = new Color(0.3f, 0.1f, 0.02f, 0.15f),
        cameraBg = new Color(0.02f, 0.04f, 0.07f),
    };

    public static ThemeData Synthwave() => new ThemeData
    {
        name = "SYNTHWAVE",
        trackBase      = new Color(0.10f, 0.06f, 0.16f),
        gridColor1     = new Color(1.0f, 0.1f, 0.55f, 0.55f),
        gridColor2     = new Color(0.6f, 0.0f, 0.9f, 0.45f),
        gridColor3     = new Color(1.0f, 0.5f, 0.1f, 0.3f),
        gridFarColor   = new Color(0.2f, 0.02f, 0.3f, 0.3f),
        sparkColor1    = new Color(1.0f, 0.2f, 0.6f),
        sparkColor2    = new Color(0.6f, 0.1f, 1.0f),
        sparkColor3    = new Color(1.0f, 0.6f, 0.15f),
        gateFrontColor    = new Color(1.0f, 0.95f, 0.2f),
        gateFrontEmission = new Color(0.8f, 0.7f, 0.08f),
        gateTopColor      = new Color(1.0f, 1.0f, 0.4f),
        gateTopEmission   = new Color(0.9f, 0.8f, 0.15f),
        ballColor        = new Color(0.95f, 0.9f, 1.0f),
        ballRimColor     = new Color(0.9f, 0.15f, 0.65f),
        ballEmissionBase = new Color(0.18f, 0.08f, 0.22f),
        railLeftNear         = new Color(0.7f, 0.0f, 1.0f),
        railLeftFar          = new Color(0.2f, 0.02f, 0.35f),
        railLeftEmissionNear = new Color(0.4f, 0.0f, 0.6f),
        railLeftEmissionFar  = new Color(0.08f, 0.01f, 0.12f),
        railRightNear         = new Color(1.0f, 0.5f, 0.15f),
        railRightFar          = new Color(0.3f, 0.1f, 0.02f),
        railRightEmissionNear = new Color(0.55f, 0.25f, 0.05f),
        railRightEmissionFar  = new Color(0.1f, 0.04f, 0.01f),
        nebulaColor1    = new Color(0.10f, 0.02f, 0.15f),
        nebulaColor2    = new Color(0.04f, 0.01f, 0.12f),
        nebulaColor3    = new Color(0.12f, 0.03f, 0.08f),
        nebulaBrightness = 0.75f,
        rayColor1    = new Color(0.15f, 0.02f, 0.2f),
        rayColor2    = new Color(0.12f, 0.01f, 0.15f),
        rayIntensity = 0.35f,
        starAccent1 = new Color(1.0f, 0.3f, 0.65f),
        starAccent2 = new Color(0.65f, 0.15f, 1.0f),
        starAccent3 = new Color(1.0f, 0.55f, 0.2f),
        starAccent4 = new Color(1.0f, 0.2f, 0.45f),
        starBase    = new Color(0.8f, 0.6f, 0.95f),
        trailColorNear = new Color(0.2f, 0.85f, 1.0f, 0.9f),
        trailColorFar  = new Color(0.04f, 0.2f, 0.35f, 0.15f),
        cameraBg = new Color(0.05f, 0.02f, 0.08f),
    };

    public static ThemeData ArcticVoid() => new ThemeData
    {
        name = "ARCTIC VOID",
        trackBase      = new Color(0.11f, 0.13f, 0.16f),
        gridColor1     = new Color(0.4f, 0.7f, 0.9f, 0.5f),
        gridColor2     = new Color(0.6f, 0.8f, 1.0f, 0.35f),
        gridColor3     = new Color(0.3f, 0.9f, 0.7f, 0.25f),
        gridFarColor   = new Color(0.1f, 0.15f, 0.25f, 0.3f),
        sparkColor1    = new Color(0.5f, 0.85f, 1.0f),
        sparkColor2    = new Color(0.8f, 0.9f, 1.0f),
        sparkColor3    = new Color(0.3f, 1.0f, 0.75f),
        gateFrontColor    = new Color(1.0f, 0.7f, 0.2f),
        gateFrontEmission = new Color(0.75f, 0.45f, 0.08f),
        gateTopColor      = new Color(1.0f, 0.8f, 0.35f),
        gateTopEmission   = new Color(0.85f, 0.55f, 0.15f),
        ballColor        = new Color(0.92f, 0.95f, 1.0f),
        ballRimColor     = new Color(0.4f, 0.75f, 1.0f),
        ballEmissionBase = new Color(0.12f, 0.15f, 0.22f),
        railLeftNear         = new Color(0.3f, 0.6f, 1.0f),
        railLeftFar          = new Color(0.08f, 0.15f, 0.35f),
        railLeftEmissionNear = new Color(0.15f, 0.3f, 0.55f),
        railLeftEmissionFar  = new Color(0.03f, 0.05f, 0.12f),
        railRightNear         = new Color(0.3f, 0.95f, 0.7f),
        railRightFar          = new Color(0.05f, 0.2f, 0.18f),
        railRightEmissionNear = new Color(0.1f, 0.45f, 0.3f),
        railRightEmissionFar  = new Color(0.02f, 0.08f, 0.06f),
        nebulaColor1    = new Color(0.03f, 0.05f, 0.10f),
        nebulaColor2    = new Color(0.02f, 0.06f, 0.12f),
        nebulaColor3    = new Color(0.04f, 0.08f, 0.10f),
        nebulaBrightness = 0.55f,
        rayColor1    = new Color(0.04f, 0.10f, 0.18f),
        rayColor2    = new Color(0.02f, 0.15f, 0.12f),
        rayIntensity = 0.3f,
        starAccent1 = new Color(0.7f, 0.9f, 1.0f),
        starAccent2 = new Color(0.4f, 0.75f, 1.0f),
        starAccent3 = new Color(0.3f, 0.95f, 0.7f),
        starAccent4 = new Color(0.85f, 0.9f, 1.0f),
        starBase    = new Color(0.7f, 0.8f, 1.0f),
        trailColorNear = new Color(1.0f, 0.5f, 0.55f, 0.9f),
        trailColorFar  = new Color(0.3f, 0.1f, 0.12f, 0.15f),
        cameraBg = new Color(0.04f, 0.05f, 0.08f),
    };

    public static ThemeData Terminal() => new ThemeData
    {
        name = "TERMINAL",
        trackBase      = new Color(0.05f, 0.05f, 0.05f),
        gridColor1     = new Color(0.0f, 0.8f, 0.2f, 0.55f),
        gridColor2     = new Color(0.0f, 0.5f, 0.15f, 0.35f),
        gridColor3     = new Color(0.0f, 0.6f, 0.1f, 0.2f),
        gridFarColor   = new Color(0.0f, 0.12f, 0.03f, 0.3f),
        sparkColor1    = new Color(0.1f, 1.0f, 0.3f),
        sparkColor2    = new Color(0.0f, 0.7f, 0.15f),
        sparkColor3    = new Color(0.2f, 0.9f, 0.4f),
        gateFrontColor    = new Color(0.7f, 0.3f, 1.0f),
        gateFrontEmission = new Color(0.45f, 0.12f, 0.75f),
        gateTopColor      = new Color(0.8f, 0.45f, 1.0f),
        gateTopEmission   = new Color(0.55f, 0.2f, 0.85f),
        ballColor        = new Color(0.8f, 0.95f, 0.82f),
        ballRimColor     = new Color(0.0f, 0.9f, 0.25f),
        ballEmissionBase = new Color(0.03f, 0.12f, 0.04f),
        railLeftNear         = new Color(0.0f, 0.85f, 0.2f),
        railLeftFar          = new Color(0.0f, 0.2f, 0.05f),
        railLeftEmissionNear = new Color(0.0f, 0.45f, 0.1f),
        railLeftEmissionFar  = new Color(0.0f, 0.06f, 0.02f),
        railRightNear         = new Color(0.0f, 0.7f, 0.15f),
        railRightFar          = new Color(0.0f, 0.15f, 0.04f),
        railRightEmissionNear = new Color(0.0f, 0.35f, 0.08f),
        railRightEmissionFar  = new Color(0.0f, 0.05f, 0.01f),
        nebulaColor1    = new Color(0.0f, 0.04f, 0.01f),
        nebulaColor2    = new Color(0.0f, 0.06f, 0.02f),
        nebulaColor3    = new Color(0.0f, 0.03f, 0.01f),
        nebulaBrightness = 0.4f,
        rayColor1    = new Color(0.0f, 0.12f, 0.03f),
        rayColor2    = new Color(0.0f, 0.08f, 0.02f),
        rayIntensity = 0.25f,
        starAccent1 = new Color(0.15f, 1.0f, 0.3f),
        starAccent2 = new Color(0.0f, 0.8f, 0.2f),
        starAccent3 = new Color(0.1f, 0.7f, 0.15f),
        starAccent4 = new Color(0.2f, 0.9f, 0.35f),
        starBase    = new Color(0.3f, 0.7f, 0.35f),
        trailColorNear = new Color(1.0f, 0.7f, 0.15f, 0.9f),
        trailColorFar  = new Color(0.25f, 0.12f, 0.02f, 0.15f),
        cameraBg = new Color(0.02f, 0.02f, 0.02f),
    };

    public static ThemeData Bloodline() => new ThemeData
    {
        name = "BLOODLINE",
        trackBase      = new Color(0.12f, 0.06f, 0.06f),
        gridColor1     = new Color(0.85f, 0.08f, 0.12f, 0.55f),
        gridColor2     = new Color(0.5f, 0.02f, 0.08f, 0.4f),
        gridColor3     = new Color(0.9f, 0.85f, 0.75f, 0.2f),
        gridFarColor   = new Color(0.2f, 0.03f, 0.05f, 0.3f),
        sparkColor1    = new Color(1.0f, 0.15f, 0.2f),
        sparkColor2    = new Color(0.8f, 0.05f, 0.1f),
        sparkColor3    = new Color(0.95f, 0.85f, 0.75f),
        gateFrontColor    = new Color(1.0f, 0.8f, 0.2f),
        gateFrontEmission = new Color(0.75f, 0.5f, 0.08f),
        gateTopColor      = new Color(1.0f, 0.9f, 0.35f),
        gateTopEmission   = new Color(0.85f, 0.6f, 0.15f),
        ballColor        = new Color(0.95f, 0.9f, 0.88f),
        ballRimColor     = new Color(0.9f, 0.1f, 0.15f),
        ballEmissionBase = new Color(0.18f, 0.06f, 0.06f),
        railLeftNear         = new Color(0.85f, 0.05f, 0.1f),
        railLeftFar          = new Color(0.3f, 0.02f, 0.05f),
        railLeftEmissionNear = new Color(0.5f, 0.02f, 0.05f),
        railLeftEmissionFar  = new Color(0.1f, 0.01f, 0.02f),
        railRightNear         = new Color(0.9f, 0.8f, 0.65f),
        railRightFar          = new Color(0.2f, 0.15f, 0.1f),
        railRightEmissionNear = new Color(0.4f, 0.35f, 0.25f),
        railRightEmissionFar  = new Color(0.08f, 0.06f, 0.04f),
        nebulaColor1    = new Color(0.10f, 0.01f, 0.02f),
        nebulaColor2    = new Color(0.06f, 0.02f, 0.03f),
        nebulaColor3    = new Color(0.08f, 0.01f, 0.01f),
        nebulaBrightness = 0.6f,
        rayColor1    = new Color(0.15f, 0.02f, 0.03f),
        rayColor2    = new Color(0.1f, 0.01f, 0.02f),
        rayIntensity = 0.3f,
        starAccent1 = new Color(1.0f, 0.2f, 0.25f),
        starAccent2 = new Color(0.85f, 0.08f, 0.12f),
        starAccent3 = new Color(0.95f, 0.85f, 0.7f),
        starAccent4 = new Color(0.8f, 0.15f, 0.2f),
        starBase    = new Color(0.85f, 0.65f, 0.6f),
        trailColorNear = new Color(0.4f, 0.65f, 1.0f, 0.9f),
        trailColorFar  = new Color(0.08f, 0.12f, 0.35f, 0.15f),
        cameraBg = new Color(0.05f, 0.02f, 0.02f),
    };

    public static ThemeData Gilded() => new ThemeData
    {
        name = "GILDED",
        trackBase      = new Color(0.12f, 0.10f, 0.07f),
        gridColor1     = new Color(0.9f, 0.7f, 0.2f, 0.5f),
        gridColor2     = new Color(0.7f, 0.45f, 0.1f, 0.4f),
        gridColor3     = new Color(0.85f, 0.55f, 0.15f, 0.3f),
        gridFarColor   = new Color(0.18f, 0.12f, 0.04f, 0.3f),
        sparkColor1    = new Color(1.0f, 0.85f, 0.3f),
        sparkColor2    = new Color(0.9f, 0.6f, 0.15f),
        sparkColor3    = new Color(0.8f, 0.7f, 0.5f),
        gateFrontColor    = new Color(0.2f, 0.9f, 0.8f),
        gateFrontEmission = new Color(0.08f, 0.6f, 0.5f),
        gateTopColor      = new Color(0.35f, 0.95f, 0.85f),
        gateTopEmission   = new Color(0.15f, 0.7f, 0.6f),
        ballColor        = new Color(1.0f, 0.97f, 0.9f),
        ballRimColor     = new Color(1.0f, 0.8f, 0.25f),
        ballEmissionBase = new Color(0.18f, 0.14f, 0.06f),
        railLeftNear         = new Color(0.85f, 0.55f, 0.1f),
        railLeftFar          = new Color(0.25f, 0.15f, 0.03f),
        railLeftEmissionNear = new Color(0.5f, 0.3f, 0.05f),
        railLeftEmissionFar  = new Color(0.1f, 0.06f, 0.01f),
        railRightNear         = new Color(0.7f, 0.75f, 0.55f),
        railRightFar          = new Color(0.15f, 0.18f, 0.1f),
        railRightEmissionNear = new Color(0.3f, 0.35f, 0.2f),
        railRightEmissionFar  = new Color(0.06f, 0.07f, 0.04f),
        nebulaColor1    = new Color(0.08f, 0.05f, 0.02f),
        nebulaColor2    = new Color(0.06f, 0.04f, 0.01f),
        nebulaColor3    = new Color(0.1f, 0.06f, 0.02f),
        nebulaBrightness = 0.6f,
        rayColor1    = new Color(0.15f, 0.10f, 0.03f),
        rayColor2    = new Color(0.12f, 0.08f, 0.02f),
        rayIntensity = 0.3f,
        starAccent1 = new Color(1.0f, 0.85f, 0.3f),
        starAccent2 = new Color(0.9f, 0.7f, 0.2f),
        starAccent3 = new Color(0.8f, 0.55f, 0.15f),
        starAccent4 = new Color(0.75f, 0.8f, 0.55f),
        starBase    = new Color(0.85f, 0.78f, 0.6f),
        trailColorNear = new Color(0.6f, 0.5f, 1.0f, 0.9f),
        trailColorFar  = new Color(0.12f, 0.08f, 0.3f, 0.15f),
        cameraBg = new Color(0.05f, 0.04f, 0.02f),
    };

    // ── REGISTRY ─────────────────────────────────────────────

    public static ThemeData[] All()
    {
        return new ThemeData[]
        {
            NeonVoid(),
            SolarFlare(),
            DeepAbyss(),
            Synthwave(),
            ArcticVoid(),
            Terminal(),
            Bloodline(),
            Gilded(),
        };
    }

    private const string PREF_THEME = "SelectedTheme";

    public static int LoadSavedIndex()
    {
        return PlayerPrefs.GetInt(PREF_THEME, 0);
    }

    public static void SaveIndex(int index)
    {
        PlayerPrefs.SetInt(PREF_THEME, index);
        PlayerPrefs.Save();
    }
}
