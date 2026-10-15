using UnityEngine;
using BubbleShooter.Grid;

namespace BubbleShooter.Shooter
{
    /// <summary>
    /// Is object (jaise Shooter ki CurrentBubble ya NextBubble) ka size BubbleGrid ke
    /// Bubble Diameter ke barabar kar deta hai. Grid ka diameter badlo to ye khud sath badalta hai.
    ///
    /// Setup: Hierarchy mein Shooter > CurrentBubble select karo > Add Component > BubbleSizeFitter.
    /// NextBubble par bhi yahi karo.
    /// </summary>
    public class BubbleSizeFitter : MonoBehaviour
    {
        [SerializeField] private BubbleGrid grid;

        [Tooltip("ON: agar koi aur script size badal de to ye dobara sahi kar deta hai. " +
                 "Pop/scale animation wale object par ON mat rakho.")]
        [SerializeField] private bool refitContinuously = false;

        [SerializeField] private float tolerance = 0.02f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (grid == null) grid = FindObjectOfType<BubbleGrid>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            Fit();
        }

        private void Start()
        {
            Fit();
        }

        private void LateUpdate()
        {
            if (!refitContinuously || spriteRenderer == null || grid == null) return;

            if (Mathf.Abs(spriteRenderer.bounds.size.x - grid.BubbleDiameter) > tolerance)
                Fit();
        }

        /// <summary>Size dobara grid ke diameter ke barabar karo.</summary>
        public void Fit()
        {
            if (grid == null) grid = FindObjectOfType<BubbleGrid>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (grid == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            transform.localScale = Vector3.one;   // pehle reset, phir naap

            float currentSize = spriteRenderer.bounds.size.x;
            if (currentSize <= 0.0001f) return;

            transform.localScale = Vector3.one * (grid.BubbleDiameter / currentSize);
        }
    }
}