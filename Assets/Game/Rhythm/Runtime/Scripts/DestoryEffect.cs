using Game.Rhythm.Runtime;
using UnityEngine;

public class DestoryEffect : MonoBehaviour
{
    public RhythmSessionController gameController;
    public bool isHitted;
    public float animationTime;

    private void OnEnable()
    {
        Invoke(nameof(ReturnToPool), animationTime);
    }

    private void ReturnToPool()
    {
        if (gameController == null)
        {
            return;
        }

        if (isHitted)
        {
            gameController.ReturnEffectObjectToPool(gameObject, gameController.hitEffectObjectPool);
        }
        else
        {
            gameController.ReturnEffectObjectToPool(gameObject, gameController.downEffectObjectPool);
        }

        gameObject.SetActive(false);
    }
}
