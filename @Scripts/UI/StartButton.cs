using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    [SerializeField] private AudioClip startSFX;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnStartButtonPressed()
    {
        
        if (startSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(startSFX);
            
            StartCoroutine(LoadGameSceneAfterDelay(startSFX.length));
        }
        else
        {
            
            SceneManager.LoadScene(1);
        }
    }

    private IEnumerator LoadGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(1);
    }
}

