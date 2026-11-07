using System.Collections;
using ScientistCat.Combat;
using UnityEngine;

namespace ScientistCat.Bosses
{
    /// <summary>
    /// Placeholder attack visuals and area damage shared by all bosses:
    /// ground telegraphs (circle / line), falling objects, radial projectile bursts.
    /// </summary>
    public static class BossFx
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Flat disc on the ground that pulses until <paramref name="duration"/> ends, then destroys itself.</summary>
        public static GameObject CircleTelegraph(MonoBehaviour host, Vector3 center, float radius, float duration, Material material, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Telegraph_Circle";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(center.x, 0.03f, center.z);
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            Setup(go, material);
            host.StartCoroutine(PulseAndDestroy(go, duration, color, growFrom: 0.2f));
            Object.Destroy(go, duration + 0.05f); // safety net if the host's coroutines are stopped (boss died)
            return go;
        }

        /// <summary>Flat strip on the ground from <paramref name="from"/> along <paramref name="direction"/>.</summary>
        public static GameObject LineTelegraph(MonoBehaviour host, Vector3 from, Vector3 direction, float length, float width, float duration, Material material, Color color)
        {
            direction.y = 0f;
            direction.Normalize();
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Telegraph_Line";
            Object.Destroy(go.GetComponent<Collider>());
            Vector3 mid = from + direction * (length * 0.5f);
            go.transform.SetPositionAndRotation(new Vector3(mid.x, 0.03f, mid.z), Quaternion.LookRotation(direction, Vector3.up));
            go.transform.localScale = new Vector3(width, 0.01f, length);
            Setup(go, material);
            host.StartCoroutine(PulseAndDestroy(go, duration, color, growFrom: 1f));
            Object.Destroy(go, duration + 0.05f);
            return go;
        }

        /// <summary>Damages Health components on <paramref name="targetLayers"/> inside a vertical cylinder-ish area.</summary>
        public static void AreaDamage(Vector3 center, float radius, int damage, LayerMask targetLayers)
        {
            Vector3 bottom = new Vector3(center.x, 0f, center.z);
            var hits = Physics.OverlapCapsule(bottom, bottom + Vector3.up * 2.5f, radius, targetLayers, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                var health = h.GetComponentInParent<Health>();
                if (health != null) health.TakeDamage(damage, center);
            }
        }

        /// <summary>Fires <paramref name="count"/> projectiles evenly around a circle.</summary>
        public static void RadialBurst(Projectile prefab, Vector3 origin, int count, float angleOffset, float speed = -1f)
        {
            if (prefab == null || count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                float a = angleOffset + i * 360f / count;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                var p = Object.Instantiate(prefab, origin + dir * 1.2f, Quaternion.identity);
                p.Launch(dir, speed);
            }
        }

        /// <summary>Drops a visual object from above onto <paramref name="target"/>, then removes it.</summary>
        public static IEnumerator DropObject(GameObject prefab, Vector3 target, float fallTime, float lingerTime)
        {
            if (prefab == null) yield break;
            Vector3 start = target + Vector3.up * 10f;
            var go = Object.Instantiate(prefab, start, prefab.transform.rotation);
            for (float t = 0f; t < fallTime; t += Time.deltaTime)
            {
                if (go == null) yield break;
                go.transform.position = Vector3.Lerp(start, target, t / fallTime);
                yield return null;
            }
            if (go == null) yield break;
            go.transform.position = target;
            Object.Destroy(go, lingerTime);
        }

        private static void Setup(GameObject go, Material material)
        {
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static IEnumerator PulseAndDestroy(GameObject go, float duration, Color color, float growFrom)
        {
            var r = go.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();
            Vector3 fullScale = go.transform.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (go == null) yield break;
                float k = t / duration;
                float grow = Mathf.Lerp(growFrom, 1f, Mathf.Clamp01(k * 1.5f));
                go.transform.localScale = new Vector3(fullScale.x * grow, fullScale.y, fullScale.z * (growFrom < 1f ? grow : 1f));
                float pulse = 0.55f + 0.45f * Mathf.Sin(t * Mathf.Lerp(8f, 30f, k));
                block.SetColor(BaseColorId, Color.Lerp(color * 0.6f, color, pulse));
                r.SetPropertyBlock(block);
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }
    }
}
