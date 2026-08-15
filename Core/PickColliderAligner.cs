using UnityEngine;

namespace CustomBlocks.Core
{
    // Keeps a pickable's clickable box on top of the artwork the player sees at
    // rest.
    //
    // There is no callback meaning "this block now looks the way it normally
    // looks". PickableBlock.Enable(true) is actively the worst moment to
    // measure: it calls Update(), which resets every renderer's alpha to 1.
    // Glue-based blocks carry a whole rig — a crate and a tire — that is
    // invisible at rest and animates in on mouse-over, so a sample taken right
    // after that reset sizes the hitbox to the entire 1.57x2.91 assembly
    // instead of the 0.54x0.55 icon. Measured that way, Acid's and
    // RCReceiver's boxes did not overlap their artwork at all.
    //
    // Hover only ever makes the artwork bigger, so the resting appearance is
    // the SMALLEST the visible bounds ever get. Sample every LateUpdate and
    // adopt a new box only when it shrinks. That needs no frame counting, no
    // hover flag, and no knowledge of which renderers belong to which
    // animation — and it is self-correcting if a block's art changes later.
    //
    // Sampling is cheap and stops on its own: a hidden page disables the
    // renderers, VisibleBounds then finds nothing and returns immediately.
    public class PickColliderAligner : MonoBehaviour
    {
        public CustomBlock Owner;
        public PickableBlock Pick;

        // Shrinking below the best seen by less than this is noise, not the
        // artwork settling.
        const float Epsilon = 1e-4f;

        bool have;
        Bounds best;

        public void Rearm()
        {
            have = false;
        }

        void LateUpdate()
        {
            if (Owner == null || Pick == null)
            {
                enabled = false;
                return;
            }

            Bounds now;
            if (!CustomBlock.VisibleBounds(transform, out now)) return;

            float area = now.size.x * now.size.y;
            if (have && area >= best.size.x * best.size.y - Epsilon) return;

            best = now;
            have = true;
            Owner.AlignPickCollider(Pick, now);
        }
    }
}
