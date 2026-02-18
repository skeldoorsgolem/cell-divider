using UnityEngine;

/// <summary>
/// Subscribes to EventBus events and fires the appropriate AudioSynth sounds.
/// Attach to a persistent GO alongside AudioSynth.
/// </summary>
public class AudioHooks : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.OnTechTreeChanged += OnUnlock;
        EventBus.OnShopChanged     += OnPurchase;
    }

    private void OnDisable()
    {
        EventBus.OnTechTreeChanged -= OnUnlock;
        EventBus.OnShopChanged     -= OnPurchase;
    }

    private void OnUnlock()   => AudioSynth.Instance?.PlayUnlock();
    private void OnPurchase() => AudioSynth.Instance?.PlayPurchase();
}
