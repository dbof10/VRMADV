﻿using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UniRx.Async;
using VRM;
public class VRM_reading : MonoBehaviour
{  
    public GameObject Canvas;
    public GameObject Content;
    public GameObject ScrollView;
    public GameObject ButtonPrefab;
    private GameObject ButtonNow;
    private int count = 0;
    private int margin = 90;
    private int AvaterNum = 3;

    async UniTask Start(){
        DirectoryCheck();
        string[] AvaterPath = DefaultAvaterPath();
        foreach (string check in AvaterPath)
        {
            Debug.Log(check);
            if (System.IO.File.Exists (Application.persistentDataPath + "/" + "ModelData" + "/" + check) == false)
            {
                await CopyAvater(Application.streamingAssetsPath + "/" + check, check);
            }
        }
        
        DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath + "/" + "ModelData");
        FileInfo[] info = dir.GetFiles("*.vrm");

        ButtonLayout();
        var converter =  new ByteToVRMConverter();       

        foreach(FileInfo f in info)
        {
#if UNITY_EDITOR
            Sprite sprite = await converter.GetMetaData(Application.persistentDataPath + "/ModelData/" + f.Name);
#elif UNITY_ANDROID
            Sprite sprite = await converter.GetMetaData("file://" + Application.persistentDataPath + "/ModelData/" + f.Name);
#endif
            
            ButtonCreate(count);
            GameObject.Find("Button" + count).GetComponent<Image>().sprite = sprite;
            count++;
        }

    }

    public void ButtonLayout()
    {
        Vector2 CanvasSize = Canvas.GetComponent<RectTransform>().sizeDelta;
        var ButtonSize =  (CanvasSize.x - margin * 3) / 2;
        ScrollView.GetComponent<RectTransform>().sizeDelta = CanvasSize;
        Content.GetComponent<GridLayoutGroup>().padding.left = margin;
        Content.GetComponent<GridLayoutGroup>().padding.top = margin;
        Content.GetComponent<GridLayoutGroup>().padding.bottom = margin;
        Content.GetComponent<GridLayoutGroup>().cellSize = new Vector2(ButtonSize, ButtonSize);
        Content.GetComponent<GridLayoutGroup>().spacing = new Vector2(margin, margin); 
    }

    public void ButtonCreate(int count)
    {
        ButtonNow = Instantiate(ButtonPrefab) as GameObject;
        ButtonNow.transform.SetParent(Content.transform);
        ButtonNow.name = "Button" + count;
        ButtonNow.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
    }

    public void DirectoryCheck()
    {
        if (!(Directory.Exists(Application.persistentDataPath + "/" + "ModelData")))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/" + "ModelData");
        }
    }


    public async UniTask CopyAvater(string path,string model)
    {
        var uwr = UnityWebRequest.Get(path);
        await uwr.SendWebRequest();
        Debug.Log("Bytes:" + uwr.downloadedBytes);

        if(uwr.isNetworkError || uwr.isHttpError)
        {
            throw new Exception("Cannnot local file:" + path);
        }

        byte[] bytes = uwr.downloadHandler.data;
        string topath =  Application.persistentDataPath + "/" + "ModelData" + "/" + model;
        File.WriteAllBytes(topath, bytes);
    }

    public string[] DefaultAvaterPath()
    {
        string[] AvaterPath = new string[AvaterNum];
        for(int num = 0; num < AvaterNum; num++)
        {
            AvaterPath[num] = "model" + num +".vrm";
        }
        return AvaterPath;

    }
}
public class ByteToVRMConverter :IDisposable
{
    public async UniTask<Sprite> GetMetaData(string path)
    {
        var data = await LoadAvater(path);
        Debug.Log("Loaded");
        var tex = data.Thumbnail;
        Sprite sprite = Sprite.Create(
        texture : tex,
            rect : new Rect(0, 0, tex.width, tex.height),
            pivot : new Vector2(0.5f, 0.5f) 
        );
        return sprite; 
    }

    public async UniTask<VRMMetaObject> LoadAvater(string path)
    {
        Debug.Log(path);
        var uwr = UnityWebRequest.Get(path);
        await uwr.SendWebRequest();
        Debug.Log("Bytes:" + uwr.downloadedBytes);

        if(uwr.isNetworkError || uwr.isHttpError)
        {
            throw new Exception("Cannnot local file:" + path);
        }

        byte[] bytes = uwr.downloadHandler.data;
        var context = new VRMImporterContext();
        context.ParseGlb(bytes);
        return context.ReadMeta(true); 
    }

    public void Dispose()
    {

    }
}