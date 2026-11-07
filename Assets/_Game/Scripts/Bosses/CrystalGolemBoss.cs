using System.Collections;
using ScientistCat.Combat;
using UnityEngine;

namespace ScientistCat.Bosses
{
    /// <summary>
    /// Boss 1 - Crystal Golem (Kristal Boyut). Heavy ground boss with three telegraphed attacks:
    ///   Charge       - red strip shows the path, then it rushes along it (stunned if it hits a wall).
    ///   Crystal Rain - red circles around the player, crystals fall from the sky a moment later.
    ///   Shard Ring   - shoots crystal shards in every direction.
    /// Phase 2 (below 50%): faster charge + shard ring on impact, more crystals, double ring.
    /// </summary>
    public class CrystalGolemBoss : BossBase
    {
        [Header("References")]
        [SerializeField] private Projectile shardPrefab;
        [SerializeField] private GameObject fallingCrystalPrefab;
        [SerializeField] private Material telegraphMaterial;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.25f, 0.2f);
        [Tooltip("Visual root that gets squashed/turned (keeps the CharacterController untouched).")]
        [SerializeField] private Transform visual;
        [SerializeField] private LayerMask playerLayers;

        [Header("Walk")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float keepDistance = 3.5f;
        [SerializeField] private float turnSpeed = 6f;

        [Header("Charge")]
        [SerializeField] private float chargeWindup = 0.9f;
        [SerializeField] private float chargeSpeed = 16f;
        [SerializeField] private float chargeDistance = 18f;
        [SerializeField] private float chargeStun = 0.9f;

        [Header("Crystal Rain")]
        [SerializeField] private int rainCount = 4;
        [SerializeField] private int rainCountPhase2 = 7;
        [SerializeField] private float rainRadius = 1.6f;
        [SerializeField] private float rainWarning = 1.1f;
        [SerializeField] private float rainSpread = 5f;
        [SerializeField] private int rainDamage = 1;

        [Header("Shard Ring")]
        [SerializeField] private int ringCount = 10;
        [SerializeField] private int ringCountPhase2 = 14;
        [SerializeField] private float shardSpeed = 9f;
        [SerializeField] private float ringWindup = 0.6f;

        [Header("Phase 2")]
        [SerializeField] private float phase2SpeedMultiplier = 1.3f;

        private int lastAttack = -1;
        private Vector3 visualBaseScale = Vector3.one;

        protected override void Awake()
        {
            base.Awake();
            if (visual != null) visualBaseScale = visual.localScale;
        }

        protected override IEnumerator RunAttack(int phase)
        {
            int pick;
            do pick = Random.Range(0, 3); while (pick == lastAttack);
            lastAttack = pick;

            switch (pick)
            {
                case 0: yield return Charge(phase); break;
                case 1: yield return CrystalRain(phase); break;
                default: yield return ShardRing(phase); break;
            }
        }

        protected override IEnumerator Intro()
        {
            yield return Squash(0.25f, 0.6f);
            ShakeCamera(0.4f);
        }

        protected override IEnumerator OnPhaseTransition(int phase)
        {
            ShakeCamera(0.7f);
            yield return Squash(0.3f, 0.5f);
            BossFx.RadialBurst(shardPrefab, ShotOrigin(), ringCountPhase2, Random.Range(0f, 360f), shardSpeed);
        }

        /// <summary>Between attacks: walk toward the player, facing them.</summary>
        protected override IEnumerator Recover(float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Vector3 dir = DirectionToPlayer();
                Face(dir);
                if (FlatDistanceToPlayer() > keepDistance) MovePlanar(dir * walkSpeed * SpeedMul());
                yield return null;
            }
        }

        // ---------------- Attacks ----------------

        private IEnumerator Charge(int phase)
        {
            Vector3 dir = DirectionToPlayer();
            Face(dir, instant: true);
            float windup = phase > 0 ? chargeWindup * 0.75f : chargeWindup;

            BossFx.LineTelegraph(this, transform.position, dir, chargeDistance, 2.4f, windup, telegraphMaterial, telegraphColor);
            yield return Squash(windup, 0.85f); // crouch while winding up

            float speed = chargeSpeed * SpeedMul();
            for (float travelled = 0f; travelled < chargeDistance; travelled += speed * Time.deltaTime)
            {
                if ((MovePlanar(dir * speed) & CollisionFlags.Sides) != 0)
                {
                    ShakeCamera(0.45f);
                    break;
                }
                yield return null;
            }

            if (phase > 0) BossFx.RadialBurst(shardPrefab, ShotOrigin(), 8, Random.Range(0f, 45f), shardSpeed);
            yield return new WaitForSeconds(phase > 0 ? chargeStun * 0.6f : chargeStun);
        }

        private IEnumerator CrystalRain(int phase)
        {
            int count = phase > 0 ? rainCountPhase2 : rainCount;
            float warning = phase > 0 ? rainWarning * 0.8f : rainWarning;
            Vector3 center = Player != null ? Player.position : transform.position;

            yield return Squash(0.35f, 1.15f); // raise arms
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = i == 0 ? Vector2.zero : Random.insideUnitCircle * rainSpread;
                StartCoroutine(RainDrop(center + new Vector3(offset.x, 0f, offset.y), warning));
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(warning + 0.3f);
        }

        private IEnumerator RainDrop(Vector3 position, float warning)
        {
            position.y = 0f;
            BossFx.CircleTelegraph(this, position, rainRadius, warning, telegraphMaterial, telegraphColor);
            const float fall = 0.25f;
            yield return new WaitForSeconds(Mathf.Max(0f, warning - fall));
            StartCoroutine(BossFx.DropObject(fallingCrystalPrefab, position, fall, 0.8f));
            yield return new WaitForSeconds(fall);
            BossFx.AreaDamage(position, rainRadius, rainDamage, playerLayers);
            ShakeCamera(0.12f);
        }

        private IEnumerator ShardRing(int phase)
        {
            yield return Squash(ringWindup, 0.75f);
            int count = phase > 0 ? ringCountPhase2 : ringCount;
            float offset = Random.Range(0f, 360f);
            BossFx.RadialBurst(shardPrefab, ShotOrigin(), count, offset, shardSpeed);

            if (phase > 0)
            {
                yield return new WaitForSeconds(0.35f);
                BossFx.RadialBurst(shardPrefab, ShotOrigin(), count, offset + 180f / count, shardSpeed);
            }
            yield return new WaitForSeconds(0.4f);
        }

        // ---------------- Helpers ----------------

        private float SpeedMul() => Phase > 0 ? phase2SpeedMultiplier : 1f;

        private Vector3 ShotOrigin() => transform.position + Vector3.up * 1f;

        private void Face(Vector3 dir, bool instant = false)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = instant ? target : Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        /// <summary>Squash/stretch the visual to <paramref name="yScale"/> and back over <paramref name="duration"/>.</summary>
        private IEnumerator Squash(float duration, float yScale)
        {
            if (visual == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI); // 0 -> 1 -> 0
                float y = Mathf.Lerp(1f, yScale, k);
                float xz = Mathf.Lerp(1f, 1f + (1f - yScale) * 0.5f, k);
                visual.localScale = new Vector3(visualBaseScale.x * xz, visualBaseScale.y * y, visualBaseScale.z * xz);
                yield return null;
            }
            visual.localScale = visualBaseScale;
        }
    }
}
