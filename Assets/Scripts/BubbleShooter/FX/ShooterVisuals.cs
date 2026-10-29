using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;
using BubbleShooter.Shooter;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Shooter ke gird roshni: current bubble ke peeche rangeen glow (dhadakta hua)
    /// aur ghoomta hua nishana (reticle) ring. Aim karte waqt ring tez aur roshan hota hai.
    /// </summary>
    public class ShooterVisuals : MonoBehaviour
    {
        private ShooterController shooter;
        private AimController aim;
        private BubbleGrid grid;

        private SpriteRenderer halo;
        private SpriteRenderer ring;
        private SpriteRenderer ringOuter;
        private float aimBlend;
        private Color currentColor = Color.white;

        public static ShooterVisuals Attach(ShooterController shooter, AimController aim, BubbleGrid grid)
        {
            if (shooter == null) return null;
            ShooterVisuals v = shooter.GetComponent<ShooterVisuals>();
            if (v == null) v = shooter.gameObject.AddComponent<ShooterVisuals>();
            v.shooter = shooter;
            v.aim = aim;
            v.grid = grid;
            v.Build();
            return v;
        }

        private void Build()
        {
            if (halo != null) return;

            int order = shooter.BubbleSortingOrder;

            halo = Make("ShooterHalo", FxSprites.SoftDot, order - 3);
            ring = Make("ShooterReticle", FxSprites.Ring, order - 2);
            ringOuter = Make("ShooterReticleOuter", FxSprites.Ring, order - 2);
        }

        private SpriteRenderer Make(string n, Sprite s, int order)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        private void LateUpdate()
        {
            if (shooter == null || halo == null) return;

            Transform anchor = shooter.CurrentBubbleAnchor;
            BubbleController cur = shooter.CurrentBubble;
            bool visible = anchor != null && cur != null;

            halo.enabled = ring.enabled = ringOuter.enabled = visible;
            if (!visible) return;

            Vector3 pos = anchor.position;
            float d = grid != null ? grid.BubbleDiameter : 0.5f;

            Color target = BubbleColorUtility.GetColorValue(cur.Color);
            currentColor = Color.Lerp(currentColor, target, 1f - Mathf.Exp(-12f * Time.deltaTime));

            bool aiming = aim != null && aim.IsAiming;
            aimBlend = Mathf.MoveTowards(aimBlend, aiming ? 1f : 0f, Time.deltaTime * 5f);

            float t = Time.time;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3f);

            // Glow
            float haloSize = d * Mathf.Lerp(2.4f, 3f, aimBlend) * (0.95f + 0.08f * pulse);
            halo.transform.position = new Vector3(pos.x, pos.y, 0.1f);
            halo.transform.localScale = new Vector3(haloSize, haloSize, 1f);
            Color hc = currentColor;
            hc.a = Mathf.Lerp(0.45f, 0.75f, aimBlend) * (0.8f + 0.2f * pulse);
            halo.color = hc;

            // Andar wala ring: ghoomta hai
            float rs = d * 1.45f;
            ring.transform.position = pos;
            ring.transform.localScale = new Vector3(rs, rs, 1f);
            ring.transform.Rotate(0f, 0f, Mathf.Lerp(40f, 220f, aimBlend) * Time.deltaTime);
            Color rc = Color.Lerp(currentColor, Color.white, 0.5f);
            rc.a = Mathf.Lerp(0.35f, 0.85f, aimBlend);
            ring.color = rc;

            // Bahar wala ring: dheere phailta / sikudta (sonar jaisa)
            float k = Mathf.Repeat(t * 0.8f, 1f);
            float os = d * Mathf.Lerp(1.3f, 2.4f, k);
            ringOuter.transform.position = pos;
            ringOuter.transform.localScale = new Vector3(os, os, 1f);
            Color oc = currentColor;
            oc.a = (1f - k) * Mathf.Lerp(0.25f, 0.5f, aimBlend);
            ringOuter.color = oc;
        }
    }
}
