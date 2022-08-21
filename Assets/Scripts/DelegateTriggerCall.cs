using UnityEngine;
using UnityEngine.Events;

/**
 * Skripta koja delegira OnTriggerEnter event na proslije?enu enter funkciju.
 * Omogu?ava delegiranje i razlikovanje vi�e collidera na jednom objektu.
 */
public class DelegateTriggerCall : MonoBehaviour
{
    private Collider caller;
    private void Awake()
    {
        caller = GetComponent<Collider>();
    }

    public UnityEvent<OnTriggerDelegation> Enter;

    void OnTriggerEnter(Collider other) => Enter.Invoke(new OnTriggerDelegation(caller, other));
}

/**
 * Struktura delegata, sadr�i pozivatelja (collider koji je pozvao event) i primatelja (objekt koji je u�ao u collider).
 */
public struct OnTriggerDelegation
{
    public OnTriggerDelegation(Collider caller, Collider other)
    {
        Caller = caller;
        Other = other;
    }

    public Collider Caller { get; private set; }
    public Collider Other { get; private set; }
}