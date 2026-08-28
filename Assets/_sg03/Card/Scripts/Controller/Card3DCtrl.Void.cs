using UnityEngine;

namespace SG03
{
    public partial class Card3DCtrl
    {
        /// <summary>
        /// Moves the card to <paramref name="holder"/>'s position and ensures face-up state.
        /// Intended for void to line transitions.
        /// </summary>
        public void MoveToLineFaceUp(CardHolderCtrl holder, bool isAlpha, System.Action onReady = null)
        {
            if (this.movement == null) return;
            this.movement.MoveToLineFaceUp(holder, () =>
            {
                if (isAlpha)
                {
                    this.RotateZ180(() =>
                    {
                        this.FaceUp();
                        onReady?.Invoke();
                        this.SpawnHpBarAfterCardSettles(holder);
                    });
                }
                else
                {
                    this.FaceUp();
                    onReady?.Invoke();
                    this.SpawnHpBarAfterCardSettles(holder);
                }
            });
        }
    }
}

