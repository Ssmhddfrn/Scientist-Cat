using ScientistCat.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ScientistCat.UI
{
    /// <summary>Player health bar. Finds the object tagged Player if no target is set.</summary>
    public class PlayerHealthBar : MonoBehaviour
    {
        [SerializeField] private Health target;
        [SerializeField] private Image fill;
        [Tooltip("Lags behind the real value to show how much was lost.")]
        [SerializeField] private Image delayedFill;
        [SerializeField] private Text label;
        [SerializeField] private string labelFormat = "CAN {0}/{1}";
        [SerializeField] private float delayedSpeed = 0.8f;

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) target = player.GetComponent<Health>();
            }
            if (target == null) return;

            target.Changed += OnChanged;
            OnChanged(target.Current, target.Max);
            if (delayedFill != null) delayedFill.fillAmount = fill.fillAmount;
        }

        private void OnDestroy()
        {
            if (target != null) target.Changed -= OnChanged;
        }

        private void OnChanged(int current, int max)
        {
            if (fill != null) fill.fillAmount = (float)current / max;
            if (label != null) label.text = string.Format(labelFormat, current, max);
        }

        private void Update()
        {
            if (delayedFill == null || fill == null) return;
            delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, fill.fillAmount, delayedSpeed * Time.deltaTime);
        }
    }
}
