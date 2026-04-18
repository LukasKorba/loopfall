using UnityEngine;

/// <summary>
/// Level-design data for BLITZ. A formation is a named group of boxes — each
/// placement is a (crossOffset, angleOffset, hp) triple relative to a chosen
/// anchor. The spawner picks a formation each cycle and stamps it into the tube.
///
/// To add a formation: append to All() and add a weight entry in Torus.PickFormation
/// (one lerp line covering the full intensity range).
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

    // ── Seed set (v2) ─────────────────────────────────────────────
    // A = 1HP cube (~8° of cross-arc wide), B = 3HP sentinel (~10°).
    // Tight clusters sit ~9–10° center-to-center = ~1° gap, so the player
    // routes around the group or fires to clear a lane. Wide rows use the
    // same 9° cadence extended to 5 across (-18..18).
    //
    // Index → name:
    //    0 A, 1 B, 2 AAA, 3 ABA, 4 AA_stream, 5 Pyramid_A_AAA,
    //    6 Diamond_A_AAA_A, 7 Expand_A_AAA_AAAAA, 8 Shrink_AAAAA_AAA_A,
    //    9 Wall_AAAAA, 10 Zigzag_A_A_A, 11 Column_BB,
    //    12 BAB, 13 ABBA_row, 14 B_AA_B_depth
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
                    new BlitzElement(-9f, 3f, 1),
                    new BlitzElement( 0f, 3f, 1),
                    new BlitzElement( 9f, 3f, 1),
                },
                3f),

            // Front single, wide middle, back single — tight cluster, reads as one shape
            new BlitzFormation("Diamond_A_AAA_A",
                new[]
                {
                    new BlitzElement( 0f, 0f, 1),
                    new BlitzElement(-9f, 3f, 1),
                    new BlitzElement( 0f, 3f, 1),
                    new BlitzElement( 9f, 3f, 1),
                    new BlitzElement( 0f, 6f, 1),
                },
                6f),

            // Expanding wall — last row demands firing, rows packed so it reads as one wedge
            new BlitzFormation("Expand_A_AAA_AAAAA",
                new[]
                {
                    new BlitzElement(  0f, 0f, 1),
                    new BlitzElement( -9f, 3f, 1),
                    new BlitzElement(  0f, 3f, 1),
                    new BlitzElement(  9f, 3f, 1),
                    new BlitzElement(-18f, 6f, 1),
                    new BlitzElement( -9f, 6f, 1),
                    new BlitzElement(  0f, 6f, 1),
                    new BlitzElement(  9f, 6f, 1),
                    new BlitzElement( 18f, 6f, 1),
                },
                6f),

            // Opens with a wall (must fire) then narrows into a spear — relief through effort
            new BlitzFormation("Shrink_AAAAA_AAA_A",
                new[]
                {
                    new BlitzElement(-18f, 0f, 1),
                    new BlitzElement( -9f, 0f, 1),
                    new BlitzElement(  0f, 0f, 1),
                    new BlitzElement(  9f, 0f, 1),
                    new BlitzElement( 18f, 0f, 1),
                    new BlitzElement( -9f, 3f, 1),
                    new BlitzElement(  0f, 3f, 1),
                    new BlitzElement(  9f, 3f, 1),
                    new BlitzElement(  0f, 6f, 1),
                },
                6f),

            // Single row, 5 wide — pure "must fire" moment with no forgiveness layer
            new BlitzFormation("Wall_AAAAA",
                new[]
                {
                    new BlitzElement(-18f, 0f, 1),
                    new BlitzElement( -9f, 0f, 1),
                    new BlitzElement(  0f, 0f, 1),
                    new BlitzElement(  9f, 0f, 1),
                    new BlitzElement( 18f, 0f, 1),
                },
                0f),

            // Side-to-side weave — forces active swing even from the bottom
            new BlitzFormation("Zigzag_A_A_A",
                new[]
                {
                    new BlitzElement(-10f, 0f, 1),
                    new BlitzElement( 10f, 4f, 1),
                    new BlitzElement(-10f, 8f, 1),
                },
                8f),

            // Twin sentinels in line — forces sustained fire from gun/cadency upgrades
            new BlitzFormation("Column_BB",
                new[]
                {
                    new BlitzElement(0f, 0f, 3),
                    new BlitzElement(0f, 7f, 3),
                },
                7f),

            // Mirror of ABA — two sentinels flanking a 1HP. Point-rich (50 pts) and
            // forces the player to read "3HP on the sides" rather than always center.
            new BlitzFormation("BAB",
                new[]
                {
                    new BlitzElement(-12f, 0f, 3),
                    new BlitzElement(  0f, 0f, 1),
                    new BlitzElement( 12f, 0f, 3),
                },
                0f),

            // Mixed 4-wide row: 1HP, 3HP, 3HP, 1HP. 60 pts, demands sustained fire in
            // middle; outer cubes clear with any single beam pass.
            new BlitzFormation("ABBA_row",
                new[]
                {
                    new BlitzElement(-13.5f, 0f, 1),
                    new BlitzElement( -4.5f, 0f, 3),
                    new BlitzElement(  4.5f, 0f, 3),
                    new BlitzElement( 13.5f, 0f, 1),
                },
                0f),

            // Depth formation — sentinel front, 1HP pair in middle, sentinel back.
            // 60 pts, rewards committing to a lane and holding fire through the chain.
            new BlitzFormation("B_AA_B_depth",
                new[]
                {
                    new BlitzElement( 0f, 0f, 3),
                    new BlitzElement(-8f, 4f, 1),
                    new BlitzElement( 8f, 4f, 1),
                    new BlitzElement( 0f, 8f, 3),
                },
                8f),
        };
        return sFormations;
    }
}
