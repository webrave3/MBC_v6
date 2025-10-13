using UnityEngine;

public class FactoryManager : MonoBehaviour
{
    public static FactoryManager Instance { get; private set; }

    public MobileFactory MobileFactory { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterFactory(MobileFactory factory)
    {
        MobileFactory = factory;
    }
}