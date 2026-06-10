using UnityEngine;

/// <summary>
/// Attach to any 3D card prop so the MobileScanner can read the
/// CardInstance it represents.
/// </summary>
public class CardVisualizer : MonoBehaviour
{
    public CardInstance CurrentCard { get; private set; }

    public void SetCard(CardInstance card)
    {
        CurrentCard = card;
    }
}
