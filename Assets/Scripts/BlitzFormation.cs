using UnityEngine;

/// <summary>
/// Level-design data for BLITZ. A formation is a named group of boxes — each
/// placement is a (crossOffset, angleOffset, hp) triple relative to a chosen
/// anchor. The spawner picks a formation each cycle and stamps it into the tube.
///
/// To add a formation: append to All() and extend Torus.sFormationWeights with
/// a weight in each phase row.
/// </summary>
public struct BlitzElement
{
    public float crossOffset;   // degrees from formation's cross-angle anchor
    public float angleOffset;   // degrees along travel from formation anchor (0 = front row)
    public int hp;              // 1 = cube, 3 = sentinel

    public BlitzElement(float co, float ao, int h)
    {
        crossOffset = co; angleOffset = ao; hp = h;
    }
}

public class BlitzFormation
{
    public string name;
    public BlitzElement[] elements;
    public float crossAnchorMin;     // anchor clamp so widest element stays in [30,150]
    public float crossAnchorMax;
    public float longitudinalDepth;  // max angleOffset — used to pace the next spawn

    public BlitzFormation(string n, BlitzElement[] e, float depth)
    {
        name = n;
        elements = e;
        longitudinalDepth = depth;

        float maxAbs = 0f;
        for (int i = 0; i < e.Length; i++)
        {
            float abs = Mathf.Abs(e[i].crossOffset);
            if (abs > maxAbs) maxAbs = abs;
        }
        crossAnchorMin = 30f + maxAbs;
        crossAnchorMax = 150f - maxAbs;
    }

    // ── Seed set (v1) ─────────────────────────────────────────────
    // A = 1HP cube (~8° of cross-arc wide), B = 3HP sentinel (~10°).
    // Tight clusters sit ~9–12° center-to-center = one cube-width gap or less,
    // so the player routes around the group, not through it.
    //
    // Index → name: 0 A, 1 B, 2 AAA, 3 ABA, 4 AA_stream, 5 Pyramid_A_AAA
    static BlitzFormation[] sFormations;

    public static BlitzFormation[] All()
    {
        if (sFormations != null) return sFormations;

        sFormations = new BlitzFormation[]
        {
            new BlitzFormation("A",
                new[] { new BlitzElement(0f, 0f, 1) },
                0f),

            new BlitzFormation("B",
                new[] { new BlitzElement(0f, 0f, 3) },
                0f),

            new BlitzFormation("AAA",
                new[]
                {
                    new BlitzElement(-9f, 0f, 1),
                    new BlitzElement( 0f, 0f, 1),
                    new BlitzElement( 9f, 0f, 1),
                },
                0f),

            new BlitzFormation("ABA",
                new[]
                {
                    new BlitzElement(-12f, 0f, 1),
                    new BlitzElement(  0f, 0f, 3),
                    new BlitzElement( 12f, 0f, 1),
                },
                0f),

            new BlitzFormation("AA_stream",
                new[]
                {
                    new BlitzElement(0f, 0f, 1),
                    new BlitzElement(0f, 5f, 1),
                },
                5f),

            new BlitzFormation("Pyramid_A_AAA",
                new[]
                {
                    new BlitzElement( 0f, 0f, 1),
                    new BlitzElement(-9f, 6f, 1),
                    new BlitzElement( 0f, 6f, 1),
                    new BlitzElement( 9f, 6f, 1),
                },
                6f),
        };
        return sFormations;
    }
}
