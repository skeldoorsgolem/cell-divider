using UnityEngine;

/// <summary>
/// Tab switching between panels (Shop / Tech Tree).
/// Attach to each tab button. Assign all panels to the panels array
/// and set myPanel to the one this button should reveal.
/// </summary>
public class PanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject[] allPanels;
    [SerializeField] private GameObject   myPanel;

    public void ShowMyPanel()
    {
        foreach (var panel in allPanels)
            panel.SetActive(false);

        myPanel?.SetActive(true);
    }
}
