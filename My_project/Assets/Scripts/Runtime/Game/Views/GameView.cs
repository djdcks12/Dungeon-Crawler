using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Main View of the <see cref="GameApplication"></see>
    /// </summary>
    public class GameView : View<GameApplication>
    {

        void Awake()
        {
            if (App.IsDedicatedServer)
            {
                OnDedicatedServerDestroyViews();
            }
        }

        void OnDedicatedServerDestroyViews()
        {
            Destroy(gameObject);
        }
    }
}
