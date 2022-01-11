using IniHelper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SelelctProgram : MonoBehaviour
{
    public GameObject BtnTemplate;
    public const string iniSection = "Program";
    public GameObject BtnGrid;
    public Startup startup;
    public GameObject progress;


    public void Start()
    {
        string iniPath = Application.streamingAssetsPath + "/ProgramConfig.ini";
        var allProgram = IniFiles.INIGetAllItemKeys(iniPath, iniSection);
        for (int i = 0; i < allProgram.Length; i++)
        {
            var val = IniFiles.INIGetStringValue(iniPath, iniSection, allProgram[i], null);
            char[] str = {','};
            var btnStyle = val.Split(str);
            if(btnStyle.Length>1)
            {
                var btn = GameObject.Instantiate(BtnTemplate, BtnGrid.transform);
                btn.GetComponentInChildren<Text>().text = btnStyle[0];

                string spriteName = null;
                string configName = null;
                if (btnStyle.Length == 3)
                {
                    spriteName = btnStyle[1];
                    configName = btnStyle[2];
                }
                else if (btnStyle.Length == 2)
                {
                    configName = btnStyle[1];
                }

                if(!string.IsNullOrEmpty(spriteName))
                {
                    StartCoroutine(LoadImage(Path.Combine(Application.streamingAssetsPath, spriteName), (sprite) =>
                    {
                        btn.GetComponentInChildren<Image>().sprite = sprite;
                    }));
                }


                btn.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(configName) && configName.EndsWith(".ini"))
                    {
                        StartCoroutine(UpdateAlphaClose(configName));
                    }
                    else if(!string.IsNullOrEmpty(configName))
                    {
                        if (System.IO.File.Exists(Path.Combine(Application.streamingAssetsPath, configName+".ini")))
                        {
                            StartCoroutine(UpdateAlphaClose(configName+".ini"));
                        }
                    }
                });

                btn.SetActive(true);

                StartCoroutine(UpdateAlpha());
            }
        }
    }

    public IEnumerator UpdateAlphaClose(string config)
    {
        var canvasGroup = BtnGrid.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }

        var progressCG = progress.GetComponent<CanvasGroup>();
        while (progressCG.alpha<1)
        {
            progressCG.alpha += Time.deltaTime;
            yield return new WaitForSeconds(0.01f);
        }
        startup.Init(config);
    }

    public IEnumerator UpdateAlpha()
    {
        var canvasGroup = BtnGrid.GetComponent<CanvasGroup>();
        while (canvasGroup.alpha<1)
        {
            canvasGroup.alpha += Time.deltaTime;
            yield return new WaitForSeconds(0.01f);
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }


    /// <summary>
    /// 加载按钮图片
    /// </summary>
    IEnumerator LoadImage(string path, System.Action<Sprite> action)
    {
        WWW www = new WWW(path);
        yield return www;
        if (www != null && string.IsNullOrEmpty(www.error))
        {
            Texture2D texture = www.texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            action?.Invoke(sprite);
        }
    }
}
