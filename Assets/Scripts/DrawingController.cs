using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/**
 * Glavni kontroler za AirPaint aplikaciju
 */
public class DrawingController : MonoBehaviour
{
    [Header("Drawing data")]
    public GameObject drawingBoardObject;
    public GameObject drawingData;
    public GameObject drawing3dData;
    public GameObject lineObject;
    public GameObject boardObject;
    public float lineDelay; //delay popravlja pre nagle linije kod polaganog crtanja
    [Header("Additional menus")]
    public GameObject colorMenu;
    public GameObject brushMenu;
    [Header("Color picker")]
    public GameObject colorPreviewObject;
    public TextMeshPro redText;
    public TextMeshPro greenText;
    public TextMeshPro blueText;
    public TextMeshPro rainbowModeStatusText;
    public TextMeshPro widthText;
    private Renderer targetRenderer;

    private int lineCount = 1;
    float timer;
    private Dictionary<long, Line> lineDict = new Dictionary<long, Line>(); //dictionary mapiranje za podršku crtanja s dvije ruke

    // razli?ita stanja u kojima može biti aplikacija
    private bool drawingMode3D = false;
    private bool resizeToggled = false;
    private bool tapToPlaceToggled = false;
    private bool drawing = false;
    private bool rainbowMode = false;

    private void Start()
    {
        timer = lineDelay; // inicijaliziraj timer

        // pobrini se da objekti koji trebaju biti isklju?eni u startu stvarno jesu
        colorMenu.SetActive(false);
        brushMenu.SetActive(false);

        targetRenderer = colorPreviewObject.GetComponent<Renderer>();

        // reorijentiraj scenu prema kameri
        this.transform.root.gameObject.GetComponent<MixedRealitySceneContent>().ReorientContent();
    }

    #region Public methods

    /**
     * Resetira 3D i 2D crtanje
     */
    public void ResetDrawing()
    {
        foreach (Transform child in drawing3dData.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in drawingData.transform)
        {
            Destroy(child.gameObject);
        }
    }

    /**
     * Mijenja na?in crtanja na 2D ili 3D
     */
    public void SwitchDrawingMode()
    {
        drawingMode3D = !drawingMode3D;
        boardObject.GetComponent<CustomPointerHandler>().SetFocusRequired(!drawingMode3D);
        lineObject.GetComponent<LineRenderer>().useWorldSpace = drawingMode3D;
    }

    /**
     * Pali/gasi mogu?nost manipuliranja platna za crtanje
     */
    public void ToggleResize()
    {
        resizeToggled = !resizeToggled;
        drawingBoardObject.GetComponent<BoxCollider>().enabled = resizeToggled;
        drawingBoardObject.GetComponent<ObjectManipulator>().enabled = resizeToggled;
        drawingBoardObject.GetComponent<NearInteractionGrabbable>().enabled = resizeToggled;
        boardObject.GetComponent<BoundsControl>().enabled = resizeToggled;
    }

    /**
     * Pali/gasi izbornik boja
     */
    public void ToggleColorMenu()
    {
        colorMenu.SetActive(!colorMenu.activeSelf);
        if (brushMenu.activeSelf)
        {
            brushMenu.SetActive(false);
        }
    }

    /**
     * Pali/gasi izbornik za promjenu debljine kista
     */
    public void ToggleBrushMenu()
    {
        brushMenu.SetActive(!brushMenu.activeSelf);
        if (colorMenu.activeSelf)
        {
            colorMenu.SetActive(false);
        }
    }

    /**
     * Pali gasi Rainbow mode
     */
    public void ToggleRainbowMode()
    {
        rainbowMode = !rainbowMode;
        if (!rainbowMode) // osvježi prijašnje selektiranu boju
        {
            lineObject.GetComponent<LineRenderer>().startColor = targetRenderer.material.color;
            lineObject.GetComponent<LineRenderer>().endColor = targetRenderer.material.color;

            rainbowModeStatusText.color = Color.red;
            rainbowModeStatusText.text = "Rainbow mode if off";
        }
        else
        {
            rainbowModeStatusText.color = Color.green;
            rainbowModeStatusText.text = "Rainbow mode if on";
        }
    }

    /**
     * Pali/gasi Tap to place
     */
    public void EnableTapToPlace()
    {
        tapToPlaceToggled = true;
        // box collider se mora ugasiti kako bi se glavni parent object mogao micati
        boardObject.GetComponent<BoxCollider>().enabled = false;
        drawingBoardObject.GetComponent<TapToPlace>().enabled = true;
        drawingBoardObject.GetComponent<TapToPlace>().StartPlacement();
    }

    #endregion

    #region Listeners

    /**
     * Okida se kada završi Tap to place i vra?a postavke na defaultne vrijednosti
     */
    public void OnTapToPlaceEnded()
    {
        tapToPlaceToggled = false;
        boardObject.GetComponent<BoxCollider>().enabled = true;
        drawingBoardObject.GetComponent<TapToPlace>().enabled = false;
    }

    /**
     * Inicijalizira novu liniju kada korisnik krene selektirati.
     * Metoda se okida kada korisnik stisne trigger i krene crtati.
     */
    public void PointerDown(MixedRealityPointerEventData eventData)
    {
        if (!resizeToggled && !tapToPlaceToggled)
        {
            var result = eventData.Pointer.Result;
            eventData.Pointer.IsTargetPositionLockedOnFocusLock = false; // sprije?ava ljepljenje pokaziva?a za platno (mora se slobodno kretati)
            eventData.Pointer.IsFocusLocked = false; // sprije?ava crtanje izvan platna

            lineDict.Remove(eventData.Pointer.PointerId); // makni ako je ostala linija od prije sa istim identifikatorom
            drawing = true;
            Line lineClone = CreateLine();
            lineDict.Add(eventData.Pointer.PointerId, lineClone);
            if (rainbowMode)
            {
                lineClone.currentLine.GetComponent<LineRenderer>().endColor = GetRandomColor();
                lineClone.currentLine.GetComponent<LineRenderer>().startColor = GetRandomColor();
            }
        }
    }

    /**
     * Kreira novu to?ku na linij kada korisnik vu?e pokaziva?.
     * Ovdje se ograni?ava broj to?aka po liniji sa jednostavnim timerom kako se ne bi preopteretila aplikacija
     */
    public void PointerDragged(MixedRealityPointerEventData eventData)
    {
        if (!resizeToggled && !tapToPlaceToggled)
        {
            if (!drawing && !drawingMode3D) // korisnik je izašao sa platna ali nije pustio trigger, izbriši liniju ako postoji i kreni s novom
            {
                lineDict.Remove(eventData.Pointer.PointerId);
                drawing = true;
                PointerDown(eventData);
            }
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                // 2D crtanje treba koristiti local space pointere
                var position = drawingMode3D ? eventData.Pointer.Position : eventData.Pointer.Result.Details.PointLocalSpace;

                Line lineClone;
                if (lineDict.TryGetValue(eventData.Pointer.PointerId, out lineClone))
                {
                    lineClone.pointList.Add(position);
                    lineClone.currentLine.GetComponent<LineRenderer>().positionCount = lineClone.pointList.Count;
                    lineClone.currentLine.GetComponent<LineRenderer>().SetPositions(lineClone.pointList.ToArray());

                    timer = lineDelay;
                }
            }
        }
    }

    /**
     * Briše liniju vezanu za pointer.
     * Metoda se okida kada korisnik pusti trigger i završi sa crtanjem linije.
     */
    public void PointerUp(MixedRealityPointerEventData eventData)
    {
        if (!resizeToggled)
        {
            lineDict.Remove(eventData.Pointer.PointerId);
            if (lineDict.Count == 0)
            {
                drawing = false;
            }
        }
    }

    /**
     * Postavlja drawing status na false kada korisnik iza?e sa platna.
     * Služi kao signalizacija da se kreira nova linija ako korisnik ponovo do?e do platna a da nije pustio trigger.
     */
    public void FocusExit()
    {
        drawing = false;
    }

    /**
     * Ažurira udio crvene boje u liniji i prikazu boje.
     */
    public void OnSliderUpdatedRed(SliderEventData eventData)
    {
        if ((targetRenderer != null) && (targetRenderer.material != null))
        {
            targetRenderer.material.color = new Color(eventData.NewValue, targetRenderer.sharedMaterial.color.g, targetRenderer.sharedMaterial.color.b);
            if (!rainbowMode)
            {
                lineObject.GetComponent<LineRenderer>().startColor = targetRenderer.material.color;
                lineObject.GetComponent<LineRenderer>().endColor = targetRenderer.material.color;
            }
        }
        redText.text = $"{eventData.NewValue:F2}";
    }

    /**
     * Ažurira udio zelene boje u liniji i prikazu boje.
     */
    public void OnSliderUpdatedGreen(SliderEventData eventData)
    {
        if ((targetRenderer != null) && (targetRenderer.material != null))
        {
            targetRenderer.material.color = new Color(targetRenderer.sharedMaterial.color.r, eventData.NewValue, targetRenderer.sharedMaterial.color.b);
            if (!rainbowMode)
            {
                lineObject.GetComponent<LineRenderer>().startColor = targetRenderer.material.color;
                lineObject.GetComponent<LineRenderer>().endColor = targetRenderer.material.color;
            }
        }
        greenText.text = $"{eventData.NewValue:F2}";
    }

    /**
     * Ažurira udio plave boje u liniji i prikazu boje.
     */
    public void OnSliderUpdateBlue(SliderEventData eventData)
    {
        if ((targetRenderer != null) && (targetRenderer.material != null))
        {
            targetRenderer.material.color = new Color(targetRenderer.sharedMaterial.color.r, targetRenderer.sharedMaterial.color.g, eventData.NewValue);
            if (!rainbowMode)
            {
                lineObject.GetComponent<LineRenderer>().startColor = targetRenderer.material.color;
                lineObject.GetComponent<LineRenderer>().endColor = targetRenderer.material.color;
            }
        }
        blueText.text = $"{eventData.NewValue:F2}";
    }

    /**
     * Ažurira debljinu kista
     */
    public void OnSliderBrushWidth(SliderEventData eventData)
    {
        float widthMultiplier = eventData.NewValue / 10; // dijeljenje sa 10 da se smanji utjecaj slidera na debljinu.
        lineObject.GetComponent<LineRenderer>().widthMultiplier = widthMultiplier;
        widthText.text = $"{eventData.NewValue:F2}";
    }

    #endregion

    #region Private methods

    /**
     * Kreira kopiju originalne linije
     */
    private Line CreateLine()
    {
        Line lineClone = new Line();
        lineClone.currentLine = Instantiate(lineObject, drawingMode3D ? drawing3dData.transform : drawingData.transform);
        lineClone.currentLine.name = "Data (" + lineCount + ")";
        lineCount++;
        return lineClone;
    }

    /**
     * Dohva?a nasumi?nu boju.
     */
    private Color GetRandomColor()
    {
        return Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
    }

    #endregion

    #region Objects

    private class Line
    {
        public GameObject currentLine;
        public List<Vector3> pointList = new List<Vector3>();
    }

    #endregion
}
