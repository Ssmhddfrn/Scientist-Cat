using System.Collections;
using ScientistCat.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace ScientistCat.UI
{
    /// <summary>Boss health bar at the top of the screen. Bosses call Show()/Hide(); hidden by default.</summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image fill;
        [SerializeField] private Image delayedFill;
        [SerializeField] private Text nameLabel;
        [SerializeField] private float delayedSpeed = 0.5f;
        [SerializeField] private float hideDelayAfterDeath = 1.5f;

        [Header("Testing")]
        [Tooltip("Optional: shows this target on start (test scenes).")]
        [SerializeField] private Health autoShowTarget;
        [SerializeField] private string autoShowName = "Boss";

        public static BossHealthBar Instance { get; private set; }

        private Health target;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
        }

        private void Start()
        {
            if (autoShowTarget != null) Show(autoShowName, autoShowTarget);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        public void Show(string bossName, Health health)
        {
            Unsubscribe();
            target = health;
            target.Changed += OnChanged;
            target.Died += OnDied;

            if (nameLabel != null) nameLabel.text = bossName;
            OnChanged(target.Current, target.Max);
            if (delayedFill != null) delayedFill.fillAmount = fill.fillAmount;
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            Unsubscribe();
            if (root != null) root.SetActive(false);
        }

        private void OnChanged(int current, int max)
        {
            if (fill != null) fill.fillAmount = (float)current / max;
        }

        private void OnDied() => StartCoroutine(HideLater());

        private IEnumerator HideLater()
        {
            yield return new WaitForSeconds(hideDelayAfterDeath);
            Hide();
        }

        private void Unsubscribe()
        {
            if (target == null) return;
            target.Changed -= OnChanged;
            target.Died -= OnDied;
            target = null;
        }

        private void Update()
        {
            if (delayedFill == null || fill == null) return;
            delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, fill.fillAmount, delayedSpeed * Time.deltaTime);
        }
    }
}
