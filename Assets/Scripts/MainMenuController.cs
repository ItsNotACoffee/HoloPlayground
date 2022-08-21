using Microsoft.MixedReality.Toolkit;
using UnityEngine;

/**
 * Kontroler za main menu aplikacije
 */
public class MainMenuController : MonoBehaviour
{
    void Start()
    {
        // reorijentiraj scenu prema kameri
        this.transform.root.gameObject.GetComponent<MixedRealitySceneContent>().ReorientContent();
    }
}
