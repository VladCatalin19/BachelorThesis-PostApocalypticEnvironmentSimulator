using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    private int houseNum;
    private bool growTrees;
    private SetParameters.IvyGrowingSpeed ivyGrowingSpeed;
    private SetParameters.DestructionTime destructionStart;
    private int sitesPerTriangle;

    public Slider sitesSlider;
    public TextMeshProUGUI sitesText;
    public Slider housesSlider;
    public TextMeshProUGUI housesText;
    public Toggle growingTreesToggle;
    public TMP_Dropdown ivyGrowingSpeedDropdown;
    public TMP_Dropdown destructionStartDropdown;

    public RawImage loadingImage;

    private void Awake()
    {
        if (!PlayerPrefs.HasKey("HouseNumber"))
        {
            PlayerPrefs.SetInt("HouseNumber", 9);
            houseNum = 9;
        }
        else
        {
            houseNum = PlayerPrefs.GetInt("HouseNumber");
        }

        if (!PlayerPrefs.HasKey("GrowTrees"))
        {
            PlayerPrefs.SetInt("GrowTrees", 1);
            growTrees = true;
        }
        else
        {
            growTrees = System.Convert.ToBoolean(PlayerPrefs.GetInt("GrowTrees"));
        }

        if (!PlayerPrefs.HasKey("IvyGrowingSpeed"))
        {
            PlayerPrefs.SetInt("IvyGrowingSpeed", (int)SetParameters.IvyGrowingSpeed.medium);
            ivyGrowingSpeed = SetParameters.IvyGrowingSpeed.medium;
        }
        else
        {
            ivyGrowingSpeed = (SetParameters.IvyGrowingSpeed)PlayerPrefs.GetInt("IvyGrowingSpeed");
        }

        if (!PlayerPrefs.HasKey("SitesPerTriangle"))
        {
            PlayerPrefs.SetInt("SitesPerTriangle", 3);
            sitesPerTriangle = 3;
        }
        else
        {
            sitesPerTriangle = PlayerPrefs.GetInt("SitesPerTriangle");
        }

        if (!PlayerPrefs.HasKey("DestructionStart"))
        {
            PlayerPrefs.SetInt("DestructionStart", 0);
            destructionStart = SetParameters.DestructionTime.soon;
        }
        else
        {
            destructionStart = (SetParameters.DestructionTime)PlayerPrefs.GetInt("DestructionStart");
        }

        sitesSlider.value = sitesPerTriangle;
        sitesText.text = sitesPerTriangle.ToString();
        housesSlider.value = houseNum;
        housesText.text = houseNum.ToString();
        growingTreesToggle.isOn = growTrees;
        ivyGrowingSpeedDropdown.value = (int)ivyGrowingSpeed;
        destructionStartDropdown.value = (int)destructionStart;
    }

    private void Update()
    {
        sitesPerTriangle = (int)sitesSlider.value;
        sitesText.text = sitesPerTriangle.ToString();
        houseNum = (int)housesSlider.value;
        housesText.text = houseNum.ToString();
        growTrees = growingTreesToggle.isOn;
        ivyGrowingSpeed = (SetParameters.IvyGrowingSpeed)ivyGrowingSpeedDropdown.value;
        destructionStart = (SetParameters.DestructionTime)destructionStartDropdown.value;
    }

    public void ChangeHouseNum(int value)
    {
        houseNum = value;
    }

    public void ChangeIsGrowing(bool value)
    {
        growTrees = value;
    }

    public void ChangeIvySpeed(int value)
    {
        ivyGrowingSpeed = (SetParameters.IvyGrowingSpeed)value;
    }

    public void ChangeSites(int value)
    {
        sitesPerTriangle = value;
    }

    public void ChangeDestructionTime(int value)
    {
        destructionStart = (SetParameters.DestructionTime)value;
    }

    public void Play()
    {
        // Save prefs
        PlayerPrefs.SetInt("HouseNumber", houseNum);
        PlayerPrefs.SetInt("GrowTrees", System.Convert.ToInt32(growTrees));
        PlayerPrefs.SetInt("IvyGrowingSpeed", (int)ivyGrowingSpeed);
        PlayerPrefs.SetInt("DestructionStart", (int)destructionStart);
        PlayerPrefs.SetInt("SitesPerTriangle", sitesPerTriangle);

        // Change scene
        FadingScript fs = gameObject.AddComponent<FadingScript>();
        fs.img = loadingImage;
        loadingImage.enabled = true;
        loadingImage.raycastTarget = true;
        fs.nextSceneName = "GameScene";
        fs.StartAnimation(false);
        //SceneManager.LoadScene("GameScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
