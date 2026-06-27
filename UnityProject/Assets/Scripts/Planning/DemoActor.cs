using UnityEngine;

namespace AIInGames.Planning.Demo
{
    /// <summary>
    /// Visual state helpers for an actor prefab. ShowSplash swaps the body for the actor's "Splash"
    /// child (a death decal) by enabling it and hiding the other sprite renderers; ResetVisual
    /// restores the body and hides the splash again.
    /// </summary>
    public static class DemoActor
    {
        public static void ShowSplash(Transform actor)
        {
            if (actor == null)
                return;

            Transform splash = Find(actor, "Splash");
            if (splash == null)
                return;

            splash.gameObject.SetActive(true);
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (!renderers[i].transform.IsChildOf(splash))
                    renderers[i].enabled = false;
        }

        public static void ResetVisual(Transform actor)
        {
            if (actor == null)
                return;

            Transform splash = Find(actor, "Splash");
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                bool inSplash = splash != null && renderers[i].transform.IsChildOf(splash);
                if (!inSplash)
                    renderers[i].enabled = true;
            }

            if (splash != null)
                splash.gameObject.SetActive(false);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                    return child;
            return null;
        }
    }
}
