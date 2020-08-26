using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadingScript : MonoBehaviour
{
    // the image you want to fade, assign in inspector
    public RawImage img;
    public string nextSceneName;

    public void StartAnimation(bool fadeAway)
    {
        // fades the image out when you click
        StartCoroutine(FadeImage(fadeAway));
    }

    IEnumerator FadeImage(bool fadeAway)
    {
        // fade from opaque to transparent
        if (fadeAway)
        {
            // loop over 1 second backwards
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1);
            for (float i = 1; i >= 0; i -= Time.deltaTime)
            {
                // set color with i as alpha
                img.color = new Color(img.color.r, img.color.g, img.color.b, i);
                yield return null;
            }
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0);
        }
        // fade from transparent to opaque
        else
        {
            // loop over 1 second
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0);
            for (float i = 0; i <= 1; i += Time.deltaTime)
            {
                // set color with i as alpha
                img.color = new Color(img.color.r, img.color.g, img.color.b, i);
                yield return null;
            }
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1);
        }

        if (nextSceneName != null)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
