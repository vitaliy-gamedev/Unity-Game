using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildSlot Slot { get; private set; }

    public void Init(BuildSlot slot) => Slot = slot;
    
    public void Demolish()
    {
        if (Slot != null) Slot.Demolish();
        else Destroy(gameObject);
    }
}
