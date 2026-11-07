using System;
using System.Collections;
using ScientistCat.Combat;
using ScientistCat.TopDown;
using ScientistCat.UI;
using UnityEngine;

namespace ScientistCat.Bosses
{
    /// <summary>
    /// Shared boss logic: activation, health-based phases, attack loop, death.
    /// Subclasses only implement RunAttack(phase) (and optionally the phase/defeat hooks).
    /// Moves with a CharacterController so it collides with walls and cover like the player.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(CharacterController))]
    public abstract class BossBase : MonoBehaviour
    {
        [Header("Boss")]
        [SerializeField] private string bossName = "BOSS";
        [Tooltip("Health percentages (0-1) where a new phase starts, highest first. {0.5} = phase 2 at half health.")]
        [SerializeField] private float[] phaseThresholds = { 0.5f };
        [Tooltip("Starts fighting when the player gets this close.")]
        [SerializeField] private float activationRange = 16f;
        [SerializeField] private float idleBetweenAttacks = 0.8f;
        [Tooltip("Idle time multiplier per extra phase (lower = more aggressive).")]
        [SerializeField, Range(0.3f, 1f)] private float idleScalePerPhase = 0.7f;
        [SerializeField] private float phaseTransitionTime = 1.2f;
        [SerializeField] private float gravity = -25f;

        [Header("Defeat")]
        [SerializeField] private float defeatSinkTime = 1.5f;

        public string BossName => bossName;
        public int Phase { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDefeated { get; private set; }

        public event Action<int> PhaseChanged;
        public event Action Defeated;

        protected Health Health { get; private set; }
        protected CharacterController Body { get; private set; }
        protected Transform Player { get; private set; }

        private int pendingPhase = -1;
        private float verticalVelocity;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Body = GetComponent<CharacterController>();
        }

        protected virtual void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) Player = p.transform;
            Health.Changed += OnHealthChanged;
            Health.Died += OnDied;
        }

        protected virtual void OnDestroy()
        {
            if (Health == null) return;
            Health.Changed -= OnHealthChanged;
            Health.Died -= OnDied;
        }

        protected virtual void Update()
        {
            if (!IsActive && !IsDefeated && Player != null && FlatDistanceToPlayer() <= activationRange)
                Activate();

            // Keep grounded even while not moving.
            if (!IsDefeated && Body.enabled)
            {
                if (Body.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
                verticalVelocity += gravity * Time.deltaTime;
                Body.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            }
        }

        /// <summary>Starts the fight (also callable from cutscenes / arena triggers).</summary>
        public void Activate()
        {
            if (IsActive || IsDefeated) return;
            IsActive = true;
            if (BossHealthBar.Instance != null) BossHealthBar.Instance.Show(bossName, Health);
            StartCoroutine(MainLoop());
        }

        private IEnumerator MainLoop()
        {
            yield return Intro();
            while (!IsDefeated)
            {
                if (pendingPhase > Phase)
                {
                    int next = pendingPhase;
                    Health.SetInvulnerable(true);
                    Phase = next;
                    PhaseChanged?.Invoke(Phase);
                    yield return OnPhaseTransition(Phase);
                    yield return new WaitForSeconds(phaseTransitionTime);
                    Health.SetInvulnerable(false);
                }

                yield return RunAttack(Phase);
                if (IsDefeated) yield break;
                yield return Recover(idleBetweenAttacks * Mathf.Pow(idleScalePerPhase, Phase));
            }
        }

        // ---------- Hooks for subclasses ----------

        /// <summary>Runs one attack pattern for the given phase (0 = first phase).</summary>
        protected abstract IEnumerator RunAttack(int phase);

        /// <summary>Played once when the fight starts.</summary>
        protected virtual IEnumerator Intro() { yield break; }

        /// <summary>Played when entering a new phase (boss is invulnerable meanwhile).</summary>
        protected virtual IEnumerator OnPhaseTransition(int phase)
        {
            ShakeCamera(0.6f);
            yield break;
        }

        /// <summary>Pause between attacks. Default: stand still.</summary>
        protected virtual IEnumerator Recover(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        /// <summary>Defeat visuals. Default: stop hurting, sink into the ground, disappear.</summary>
        protected virtual IEnumerator DefeatSequence()
        {
            var contact = GetComponent<ContactDamage>();
            if (contact != null) contact.enabled = false;
            Body.enabled = false;
            ShakeCamera(0.8f);

            Vector3 start = transform.position;
            Vector3 startScale = transform.localScale;
            for (float t = 0f; t < defeatSinkTime; t += Time.deltaTime)
            {
                float k = t / defeatSinkTime;
                transform.position = start + Vector3.down * (k * 2f);
                transform.localScale = startScale * (1f - k * 0.6f);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        // ---------- Helpers ----------

        protected float FlatDistanceToPlayer()
        {
            if (Player == null) return float.MaxValue;
            Vector3 d = Player.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        protected Vector3 DirectionToPlayer()
        {
            if (Player == null) return transform.forward;
            Vector3 d = Player.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : transform.forward;
        }

        /// <summary>Planar move that respects walls. Returns the collision flags.</summary>
        protected CollisionFlags MovePlanar(Vector3 velocity)
        {
            velocity.y = 0f;
            return Body.Move(velocity * Time.deltaTime);
        }

        protected void ShakeCamera(float amount)
        {
            if (Camera.main == null) return;
            var cam = Camera.main.GetComponent<TopDownCameraController>();
            if (cam != null) cam.Shake(amount);
        }

        private void OnHealthChanged(int current, int max)
        {
            float pct = (float)current / max;
            int phase = 0;
            foreach (float threshold in phaseThresholds)
                if (pct <= threshold) phase++;
            if (phase > Phase && phase > pendingPhase) pendingPhase = phase;
        }

        private void OnDied()
        {
            if (IsDefeated) return;
            IsDefeated = true;
            StopAllCoroutines();
            Defeated?.Invoke();
            StartCoroutine(DefeatSequence());
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, activationRange);
        }
    }
}
