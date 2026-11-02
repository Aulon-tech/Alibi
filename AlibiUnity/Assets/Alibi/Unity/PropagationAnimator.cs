using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alibi.Unity
{
    public sealed class PropagationAnimator : MonoBehaviour
    {
        Coroutine running;

        public void Play(List<Action> steps, float stagger)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Run(steps, stagger));
        }

        IEnumerator Run(List<Action> steps, float stagger)
        {
            foreach (var step in steps)
            {
                step();
                if (stagger > 0) yield return new WaitForSeconds(stagger);
            }
            running = null;
        }

        public void Flash(Graphic g, Color flash, float duration)
        {
            if (g == null) return;
            StartCoroutine(FlashRoutine(g, flash, duration));
        }

        IEnumerator FlashRoutine(Graphic g, Color flash, float duration)
        {
            if (g == null) yield break;
            var original = g.color;
            float t = 0;
            while (t < duration && g != null)
            {
                t += Time.deltaTime;
                float k = Mathf.PingPong(t * 4f, 1f);
                g.color = Color.Lerp(original, flash, k);
                yield return null;
            }
            if (g != null) g.color = original;
        }

        public void Pulse(RectTransform rt, float duration)
        {
            if (rt == null) return;
            StartCoroutine(PulseRoutine(rt, duration));
        }

        IEnumerator PulseRoutine(RectTransform rt, float duration)
        {
            float t = 0;
            while (t < duration && rt != null)
            {
                t += Time.deltaTime;
                float s = 1f + 0.06f * Mathf.Sin(t / duration * Mathf.PI);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }
    }
}
