using System.Collections.Generic;
using UnityEngine;

namespace CubeBlast.Game
{
    /// <summary>
    /// Lightweight sprite-particle effects. All movement is coded (no Physics,
    /// no Unity animation clips), in the same spirit as the fall mechanics.
    /// </summary>
    public static class Fx
    {
        public const int ParticleSortingOrder = 220;

        private class SpriteParticle : MonoBehaviour
        {
            public Vector3 Velocity;
            public float Gravity;
            public float AngularVelocity;
            public float Lifetime;
            public bool Shrink = true;
            public bool FadeOut = true;

            private float _age;
            private SpriteRenderer _renderer;
            private Vector3 _startScale;

            private void Start()
            {
                _renderer = GetComponent<SpriteRenderer>();
                _startScale = transform.localScale;
            }

            private void Update()
            {
                _age += Time.deltaTime;
                if (_age >= Lifetime)
                {
                    Destroy(gameObject);
                    return;
                }
                Velocity += Vector3.down * Gravity * Time.deltaTime;
                transform.position += Velocity * Time.deltaTime;
                transform.Rotate(0f, 0f, AngularVelocity * Time.deltaTime);
                float t = _age / Lifetime;
                if (Shrink)
                {
                    transform.localScale = _startScale * Mathf.Lerp(1f, 0.1f, t * t);
                }
                if (FadeOut && _renderer != null)
                {
                    var c = _renderer.color;
                    c.a = Mathf.Lerp(1f, 0f, t * t);
                    _renderer.color = c;
                }
            }
        }

        /// <summary>Spawns a burst of sprite shards flying out of a world position.</summary>
        public static void Burst(Vector3 position, IList<Sprite> sprites, int count, float size,
            float speed = 4f, float gravity = 9f, float lifetime = 0.7f, Color? color = null)
        {
            if (sprites == null || sprites.Count == 0)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                var sprite = sprites[Random.Range(0, sprites.Count)];
                if (sprite == null)
                {
                    continue;
                }
                var go = new GameObject("~fx");
                go.transform.position = position + (Vector3)(Random.insideUnitCircle * 0.15f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                if (color.HasValue)
                {
                    renderer.color = color.Value;
                }
                renderer.sortingOrder = ParticleSortingOrder;
                float pixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
                float scale = size * sprite.pixelsPerUnit / pixels;
                go.transform.localScale = new Vector3(scale, scale, 1f);
                go.transform.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

                var particle = go.AddComponent<SpriteParticle>();
                Vector2 direction = Random.insideUnitCircle.normalized;
                direction.y = Mathf.Abs(direction.y) * 0.6f + direction.y * 0.4f; // bias upwards
                particle.Velocity = direction * speed * Random.Range(0.6f, 1.3f);
                particle.Gravity = gravity;
                particle.AngularVelocity = Random.Range(-360f, 360f);
                particle.Lifetime = lifetime * Random.Range(0.75f, 1.25f);
            }
        }

        /// <summary>Short camera shake for TNT-style explosions.</summary>
        public static void ShakeCamera(float strength = 0.15f, float duration = 0.25f)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }
            Vector3 basePosition = camera.transform.position;
            Tween.To(t =>
            {
                if (camera == null) { return; }
                float damper = 1f - t;
                camera.transform.position = basePosition +
                    (Vector3)(Random.insideUnitCircle * strength * damper);
            }, duration, Ease.Linear, 0f, () =>
            {
                if (camera != null) { camera.transform.position = basePosition; }
            });
        }

        public static List<Sprite> CubeShards(Core.CubeColor color)
        {
            return new List<Sprite> { GameAssets.CubeParticle(color) };
        }

        public static List<Sprite> VaseShards()
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 3; i++) { list.Add(GameAssets.VaseParticle(i)); }
            return list;
        }

        public static List<Sprite> StoneShards()
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 3; i++) { list.Add(GameAssets.StoneParticle(i)); }
            return list;
        }

        public static List<Sprite> TntShards()
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 2; i++) { list.Add(GameAssets.TntParticle(i)); }
            return list;
        }

        public static List<Sprite> ChaliceBoxShards()
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 5; i++) { list.Add(GameAssets.ChaliceBoxParticle(i)); }
            return list;
        }

        public static List<Sprite> ChaliceBoxDoorShards()
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 10; i++) { list.Add(GameAssets.ChaliceBoxDoorParticle(i)); }
            return list;
        }
    }
}
