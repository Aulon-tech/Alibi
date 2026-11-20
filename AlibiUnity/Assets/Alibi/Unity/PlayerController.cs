using System;
using UnityEngine;

namespace Alibi.Unity
{
    public sealed class PlayerController : MonoBehaviour
    {
        public float Speed = 2.2f;
        public float RunSpeed = 3.6f;
        public float Gravity = 18f;
        public bool InputBlocked;
        public Vector3 Spawn;
        public Func<float> CameraYaw;

        CharacterController cc;
        Transform visual;
        float vy;
        float walkPhase;

        public static PlayerController Create(Transform parent, Vector3 spawn)
        {
            var go = new GameObject("Detektivi");
            go.transform.SetParent(parent, false);
            go.transform.position = spawn + Vector3.up * 0.05f;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.0f;
            cc.radius = 0.16f;
            cc.center = new Vector3(0, 0.5f, 0);
            cc.stepOffset = 0.2f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.02f;
            var p = go.AddComponent<PlayerController>();
            p.cc = cc;
            p.Spawn = spawn;
            var vis = new GameObject("Pamja").transform;
            vis.SetParent(go.transform, false);
            p.visual = vis;
            var coat = Theme.Lit(Theme.Hex("#3A4B5E"), 0.3f);
            var skin = Theme.Lit(Theme.Hex("#E3B48F"), 0.2f);
            var hat = Theme.Lit(Theme.Hex("#6B4F2E"), 0.25f);
            var scarf = Theme.Lit(Theme.Gold, 0.4f);
            Part(vis, PrimitiveType.Capsule, "Trupi", new Vector3(0, 0.4f, 0), new Vector3(0.32f, 0.4f, 0.32f), coat);
            Part(vis, PrimitiveType.Cylinder, "Shalli", new Vector3(0, 0.74f, 0), new Vector3(0.27f, 0.03f, 0.27f), scarf);
            Part(vis, PrimitiveType.Sphere, "Koka", new Vector3(0, 0.93f, 0), new Vector3(0.26f, 0.26f, 0.26f), skin);
            Part(vis, PrimitiveType.Cylinder, "Strehe", new Vector3(0, 1.05f, 0), new Vector3(0.38f, 0.012f, 0.38f), hat);
            Part(vis, PrimitiveType.Cylinder, "Kapela", new Vector3(0, 1.11f, 0), new Vector3(0.22f, 0.06f, 0.22f), hat);
            Part(vis, PrimitiveType.Cube, "Hunda", new Vector3(0, 0.93f, 0.13f), new Vector3(0.05f, 0.05f, 0.05f), skin);
            return p;
        }

        static void Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        }

        public void Teleport(Vector3 position, float yawDegrees)
        {
            cc.enabled = false;
            transform.position = position + Vector3.up * 0.05f;
            transform.rotation = Quaternion.Euler(0, yawDegrees, 0);
            cc.enabled = true;
            vy = 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float x = 0, z = 0;
            bool run = false;
            if (!InputBlocked)
            {
                if (ExploreInput.Held(ExploreKey.Up)) z += 1;
                if (ExploreInput.Held(ExploreKey.Down)) z -= 1;
                if (ExploreInput.Held(ExploreKey.Right)) x += 1;
                if (ExploreInput.Held(ExploreKey.Left)) x -= 1;
                run = ExploreInput.Held(ExploreKey.Run);
            }
            float yaw = CameraYaw != null ? CameraYaw() : 0f;
            var move = Quaternion.Euler(0, yaw, 0) * new Vector3(x, 0, z);
            if (move.sqrMagnitude > 1f) move.Normalize();
            if (cc.isGrounded && vy < 0) vy = -1f;
            vy -= Gravity * dt;
            float speed = run ? RunSpeed : Speed;
            cc.Move((move * speed + Vector3.up * vy) * dt);
            bool walking = move.sqrMagnitude > 0.01f;
            if (walking)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), Mathf.Clamp01(dt * 12f));
                walkPhase += dt * (run ? 16f : 11f);
            }
            float bob = walking ? Mathf.Abs(Mathf.Sin(walkPhase)) * 0.04f : 0f;
            visual.localPosition = new Vector3(0, bob, 0);
            if (transform.position.y < -4f) Teleport(Spawn, 0f);
        }
    }
}
