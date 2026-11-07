using UnityEngine;

namespace ScientistCat.Combat
{
    /// <summary>Makes a hazard read as dangerous: pulses its color, and flashes bright when it deals damage.</summary>
    [RequireComponent(typeof(Renderer))]
    public class HazardPulse : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color colorA = new Color(1f, 0.45f, 0.05f);
        [SerializeField] private Color colorB = new Color(1f, 0.15f, 0.05f);
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private Color hitColor = new Color(1f, 1f, 0.7f);
        [SerializeField] private float hitFlashDuration = 0.15f;
        [Tooltip("Optional: flashes when this ContactDamage hits. Defaults to the one on this object.")]
        [SerializeField] private ContactDamage source;

        private Renderer rend;
        private MaterialPropertyBlock block;
        private float hitUntil;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            if (source == null) source = GetComponent<ContactDamage>();
        }

        private void OnEnable()
        {
            if (source != null) source.DamageDealt += OnDamageDealt;
        }

        private void OnDisable()
        {
            if (source != null) source.DamageDealt -= OnDamageDealt;
        }

        private void OnDamageDealt(Health target) => hitUntil = Time.time + hitFlashDuration;

        private void Update()
        {
            Color c = Time.time < hitUntil
                ? hitColor
                : Color.Lerp(colorA, colorB, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            rend.SetPropertyBlock(block);
        }
    }
}
