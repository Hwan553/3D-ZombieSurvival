using UnityEngine;

public class HelpController : MonoBehaviour
{
    [SerializeField] private GameObject helpPanel;
    
    public void OnHelpButtonClick()
    {
        helpPanel.SetActive(true);
    }

    public void OnCloseButtonClick()
    {
        helpPanel.SetActive(false);
    }

    
}
